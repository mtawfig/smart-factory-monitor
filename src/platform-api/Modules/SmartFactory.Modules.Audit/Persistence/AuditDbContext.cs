using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SmartFactory.BuildingBlocks.Persistence;
using SmartFactory.BuildingBlocks.Tenancy;
using SmartFactory.Modules.Audit.Domain;

namespace SmartFactory.Modules.Audit.Persistence;

internal sealed class AuditDbContext(DbContextOptions<AuditDbContext> options, ICurrentTenant tenant)
    : TenantAwareDbContext(options, tenant)
{
    public DbSet<AuditEvent> Events => Set<AuditEvent>();

    protected override string Schema => AuditModule.Schema;

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditEvent>(e =>
        {
            e.ToTable("audit_events");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.TenantId).HasMaxLength(64);
            e.Property(x => x.ActorId).HasMaxLength(128);
            e.Property(x => x.ActorName).HasMaxLength(200);
            e.Property(x => x.AggregateType).HasMaxLength(64);
            e.Property(x => x.AggregateId).HasMaxLength(64);
            e.Property(x => x.Action).HasMaxLength(64);
            e.Property(x => x.BeforeHash).HasMaxLength(64).IsFixedLength();
            e.Property(x => x.AfterHash).HasMaxLength(64).IsFixedLength();
            e.Property(x => x.PreviousChainHash).HasMaxLength(64).IsFixedLength();
            e.Property(x => x.ChainHash).HasMaxLength(64).IsFixedLength();
            e.Property(x => x.CorrelationId).HasMaxLength(64);
            e.HasIndex(x => new { x.TenantId, x.Sequence }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.AggregateType, x.AggregateId });
        });
    }
}

internal sealed class AuditDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AuditDbContext>
{
    public AuditDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AuditDbContext>()
            .UseNpgsql("Host=localhost;Database=design_time_only", o => o.MigrationsHistoryTable("__ef_migrations_history", AuditModule.Schema))
            .UseSnakeCaseNamingConvention()
            .Options;
        return new AuditDbContext(options, new NoTenant());
    }

    private sealed class NoTenant : ICurrentTenant
    {
        public string? TenantId => null;
    }
}
