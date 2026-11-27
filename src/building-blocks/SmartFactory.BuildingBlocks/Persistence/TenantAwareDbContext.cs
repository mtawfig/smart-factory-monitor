using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SmartFactory.BuildingBlocks.Tenancy;

namespace SmartFactory.BuildingBlocks.Persistence;

/// <summary>
/// Base class for module DbContexts. Adds a global query filter <c>TenantId == current tenant</c> to every
/// <see cref="ITenantScoped"/> entity, so a forgotten <c>Where</c> cannot leak another tenant's rows. The filter
/// reads <see cref="CurrentTenantId"/> on this context instance, so it is re-evaluated per request.
/// </summary>
public abstract class TenantAwareDbContext(DbContextOptions options, ICurrentTenant tenant) : DbContext(options)
{
    /// <summary>The schema this module owns (ADR-007). Every entity of the context is mapped into it.</summary>
    protected abstract string Schema { get; }

    /// <summary>Read by the global query filters. Empty string (never null) so the filter matches nothing.</summary>
    protected string CurrentTenantId => tenant.TenantId ?? string.Empty;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        ConfigureModel(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType) || entityType.BaseType is not null)
            {
                continue;
            }

            var entity = Expression.Parameter(entityType.ClrType, "e");
            var filter = Expression.Lambda(
                Expression.Equal(
                    Expression.Property(entity, nameof(ITenantScoped.TenantId)),
                    Expression.Property(Expression.Constant(this), nameof(CurrentTenantId))),
                entity);
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
        }
    }

    /// <summary>Module-specific mapping. Runs before the tenant filters are added.</summary>
    protected abstract void ConfigureModel(ModelBuilder modelBuilder);
}
