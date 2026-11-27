using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SmartFactory.BuildingBlocks.Persistence;
using SmartFactory.BuildingBlocks.Tenancy;
using SmartFactory.Modules.Maintenance.Domain;

namespace SmartFactory.Modules.Maintenance.Persistence;

internal sealed class MaintenanceDbContext(DbContextOptions<MaintenanceDbContext> options, ICurrentTenant tenant)
    : TenantAwareDbContext(options, tenant)
{
    public DbSet<MaintenanceRequest> Requests => Set<MaintenanceRequest>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<ChecklistItem> ChecklistItems => Set<ChecklistItem>();
    public DbSet<WorkOrderHistoryEntry> History => Set<WorkOrderHistoryEntry>();
    public DbSet<Finding> Findings => Set<Finding>();
    public DbSet<Attachment> Attachments => Set<Attachment>();

    protected override string Schema => MaintenanceModule.Schema;

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MaintenanceRequest>(e =>
        {
            e.ToTable("requests");
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).ValueGeneratedNever();
            e.Property(r => r.TenantId).HasMaxLength(64);
            e.Property(r => r.Title).HasMaxLength(200);
            e.Property(r => r.Reason).HasMaxLength(2000);
            e.Property(r => r.Priority).HasConversion<string>().HasMaxLength(16);
            e.Property(r => r.RequestedBy).HasMaxLength(120);
            e.Property(r => r.Decision).HasConversion<string>().HasMaxLength(16);
            e.HasIndex(r => new { r.TenantId, r.AssetId });
        });

        modelBuilder.Entity<WorkOrder>(e =>
        {
            e.HasKey(w => w.Id);
            e.Property(w => w.Id).ValueGeneratedNever();
            e.Property(w => w.TenantId).HasMaxLength(64);
            e.Property(w => w.AssetCode).HasMaxLength(16);
            e.Property(w => w.Title).HasMaxLength(200);
            e.Property(w => w.Description).HasMaxLength(2000);
            e.Property(w => w.Priority).HasConversion<string>().HasMaxLength(16);
            e.Property(w => w.Status).HasConversion<string>().HasMaxLength(16);
            e.Property(w => w.HeldFrom).HasConversion<string>().HasMaxLength(16);
            e.Property(w => w.AssignedTo).HasMaxLength(120);
            e.Property(w => w.ApprovedBy).HasMaxLength(120);
            e.Property(w => w.Disposition).HasConversion<string>().HasMaxLength(16);
            e.Property(w => w.StatusReason).HasMaxLength(1000);
            e.Property(w => w.Version).IsRowVersion();
            e.Ignore(w => w.PendingHistory);
            e.Ignore(w => w.PendingChecklist);
            e.Ignore(w => w.RemovedChecklist);
            e.Ignore(w => w.ChecklistResolved);
            e.HasIndex(w => new { w.TenantId, w.Status });
            e.HasIndex(w => new { w.TenantId, w.AssignedTo });
            e.HasIndex(w => new { w.TenantId, w.AssetId });
            e.HasOne<MaintenanceRequest>().WithOne().HasForeignKey<WorkOrder>(w => w.RequestId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(w => w.Checklist).WithOne().HasForeignKey(c => c.WorkOrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(w => w.History).WithOne().HasForeignKey(h => h.WorkOrderId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ChecklistItem>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).ValueGeneratedNever();
            e.Property(c => c.TenantId).HasMaxLength(64);
            e.Property(c => c.Text).HasMaxLength(300);
            e.Property(c => c.Status).HasConversion<string>().HasMaxLength(16);
            e.Property(c => c.Note).HasMaxLength(1000);
        });

        modelBuilder.Entity<WorkOrderHistoryEntry>(e =>
        {
            e.ToTable("work_order_history");
            e.HasKey(h => h.Id);
            e.Property(h => h.Id).ValueGeneratedNever();
            e.Property(h => h.TenantId).HasMaxLength(64);
            e.Property(h => h.Action).HasMaxLength(32);
            e.Property(h => h.FromStatus).HasConversion<string>().HasMaxLength(16);
            e.Property(h => h.ToStatus).HasConversion<string>().HasMaxLength(16);
            e.Property(h => h.Actor).HasMaxLength(120);
            e.Property(h => h.Note).HasMaxLength(1000);
            e.HasIndex(h => new { h.WorkOrderId, h.Revision }).IsUnique();
        });

        modelBuilder.Entity<Finding>(e =>
        {
            e.HasKey(f => f.Id);
            e.Property(f => f.Id).ValueGeneratedNever();
            e.Property(f => f.TenantId).HasMaxLength(64);
            e.Property(f => f.Technician).HasMaxLength(120);
            e.Property(f => f.Condition).HasConversion<string>().HasMaxLength(16);
            e.Property(f => f.Notes).HasMaxLength(Finding.MaxNotesLength);
            e.HasIndex(f => f.WorkOrderId);
            e.HasOne<WorkOrder>().WithMany().HasForeignKey(f => f.WorkOrderId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(f => f.Attachments).WithOne().HasForeignKey(a => a.FindingId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Attachment>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).ValueGeneratedNever();
            e.Property(a => a.TenantId).HasMaxLength(64);
            e.Property(a => a.FileName).HasMaxLength(120);
            e.Property(a => a.ContentType).HasMaxLength(64);
            e.Property(a => a.Sha256).HasMaxLength(64).IsFixedLength();
            e.Property(a => a.StorageKey).HasMaxLength(200);
        });
    }
}

/// <summary>Lets <c>dotnet ef migrations add</c> build the model without the host. The connection string is never opened.</summary>
internal sealed class MaintenanceDbContextDesignTimeFactory : IDesignTimeDbContextFactory<MaintenanceDbContext>
{
    public MaintenanceDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MaintenanceDbContext>()
            .UseNpgsql("Host=localhost;Database=design_time_only", o => o.MigrationsHistoryTable("__ef_migrations_history", MaintenanceModule.Schema))
            .UseSnakeCaseNamingConvention()
            .Options;
        return new MaintenanceDbContext(options, new FixedTenant());
    }

    private sealed class FixedTenant : ICurrentTenant
    {
        public string? TenantId => null;
    }
}
