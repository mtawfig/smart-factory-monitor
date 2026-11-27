using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SmartFactory.BuildingBlocks.Tenancy;

namespace SmartFactory.BuildingBlocks.Persistence;

/// <summary>
/// Before every <c>SaveChanges</c>: stamps the current tenant on new <see cref="ITenantScoped"/> entities and refuses
/// to save a modified or deleted entity that belongs to another tenant. A save without a tenant fails loudly.
/// </summary>
public sealed class TenantStampingInterceptor(ICurrentTenant tenant) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void Apply(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries<ITenantScoped>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            var current = tenant.TenantId;
            if (string.IsNullOrWhiteSpace(current))
            {
                throw new InvalidOperationException(
                    $"Cannot save {entry.Metadata.ClrType.Name}: no tenant in the current scope.");
            }

            var property = entry.Property(nameof(ITenantScoped.TenantId));
            if (entry.State == EntityState.Added && string.IsNullOrEmpty((string?)property.CurrentValue))
            {
                property.CurrentValue = current;
            }
            else if (!string.Equals((string?)property.CurrentValue, current, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Cross-tenant write blocked on {entry.Metadata.ClrType.Name}.");
            }
        }
    }
}
