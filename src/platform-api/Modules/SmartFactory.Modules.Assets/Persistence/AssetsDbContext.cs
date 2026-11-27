using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SmartFactory.BuildingBlocks.Persistence;
using SmartFactory.BuildingBlocks.Tenancy;
using SmartFactory.Modules.Assets.Domain;

namespace SmartFactory.Modules.Assets.Persistence;

internal sealed class AssetsDbContext(DbContextOptions<AssetsDbContext> options, ICurrentTenant tenant)
    : TenantAwareDbContext(options, tenant)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Plant> Plants => Set<Plant>();
    public DbSet<Line> Lines => Set<Line>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<SensorMapping> SensorMappings => Set<SensorMapping>();

    protected override string Schema => AssetsModule.Schema;

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Id).HasMaxLength(64);
            e.Property(t => t.Name).HasMaxLength(200);
            // Why: the tenants table is not ITenantScoped (it *is* the tenant), so it gets its own filter: a tenant
            // can read only its own row. RLS mirrors this on the id column.
            e.HasQueryFilter(t => t.Id == CurrentTenantId);
        });

        modelBuilder.Entity<Plant>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).ValueGeneratedNever();
            e.Property(p => p.TenantId).HasMaxLength(64);
            e.Property(p => p.Code).HasMaxLength(64);
            e.Property(p => p.Name).HasMaxLength(200);
            e.Property(p => p.TimeZone).HasMaxLength(64);
            e.HasIndex(p => new { p.TenantId, p.Code }).IsUnique();
            e.HasOne<Tenant>().WithMany().HasForeignKey(p => p.TenantId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(p => p.Lines).WithOne(l => l.Plant).HasForeignKey(l => l.PlantId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Line>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.Id).ValueGeneratedNever();
            e.Property(l => l.TenantId).HasMaxLength(64);
            e.Property(l => l.Code).HasMaxLength(64);
            e.Property(l => l.Name).HasMaxLength(200);
            e.HasIndex(l => new { l.TenantId, l.PlantId, l.Code }).IsUnique();
            e.HasMany(l => l.Assets).WithOne(a => a.Line).HasForeignKey(a => a.LineId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Asset>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).ValueGeneratedNever();
            e.Property(a => a.TenantId).HasMaxLength(64);
            e.Property(a => a.Code).HasMaxLength(16);
            e.Property(a => a.Name).HasMaxLength(120);
            e.Property(a => a.AssetClass).HasMaxLength(32);
            e.Property(a => a.Criticality).HasConversion<string>().HasMaxLength(16);
            e.Property(a => a.Status).HasConversion<string>().HasMaxLength(24);
            e.Property(a => a.Description).HasMaxLength(1000);
            e.Property(a => a.RatedPowerKw).HasPrecision(10, 2);
            e.Property(a => a.Version).IsRowVersion();
            e.HasIndex(a => new { a.TenantId, a.Code }).IsUnique();
            e.HasMany(a => a.Sensors).WithOne().HasForeignKey(s => s.AssetId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SensorMapping>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).ValueGeneratedNever();
            e.Property(s => s.TenantId).HasMaxLength(64);
            e.Property(s => s.Metric).HasMaxLength(48);
            e.Property(s => s.Unit).HasMaxLength(16);
            e.Property(s => s.IntervalSeconds).HasPrecision(8, 2);
            e.Property(s => s.NominalMin).HasPrecision(12, 3);
            e.Property(s => s.NominalMax).HasPrecision(12, 3);
            e.Property(s => s.Kind).HasMaxLength(48);
            e.Property(s => s.SourceTopic).HasMaxLength(256);
            e.HasIndex(s => new { s.TenantId, s.AssetId, s.Metric }).IsUnique();
            e.HasIndex(s => s.SourceTopic);
        });
    }
}

/// <summary>
/// Lets <c>dotnet ef migrations add</c> build the model without the host. The connection string is never opened.
/// </summary>
internal sealed class AssetsDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AssetsDbContext>
{
    public AssetsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AssetsDbContext>()
            .UseNpgsql("Host=localhost;Database=design_time_only", o => o.MigrationsHistoryTable("__ef_migrations_history", AssetsModule.Schema))
            .UseSnakeCaseNamingConvention()
            .Options;
        return new AssetsDbContext(options, new FixedTenant());
    }

    private sealed class FixedTenant : ICurrentTenant
    {
        public string? TenantId => null;
    }
}
