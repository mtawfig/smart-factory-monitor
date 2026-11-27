using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartFactory.BuildingBlocks.Auth;
using SmartFactory.BuildingBlocks.Persistence;
using SmartFactory.Modules.Maintenance.Endpoints;
using SmartFactory.Modules.Maintenance.Persistence;
using SmartFactory.Modules.Maintenance.Storage;

namespace SmartFactory.Modules.Maintenance;

/// <summary>
/// Public surface of the Maintenance module: the only public type in the assembly (ADR-002). The host calls these
/// methods; everything else is internal so other modules cannot reach in.
/// </summary>
public static class MaintenanceModule
{
    /// <summary>Database schema owned by this module (ADR-007).</summary>
    public const string Schema = "maintenance";

    /// <summary>Route prefix for the module's endpoints.</summary>
    public const string RoutePrefix = "/api/maintenance";

    public static IServiceCollection AddMaintenanceModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        services.AddModuleDbContext<MaintenanceDbContext>(configuration, Schema);
        services.AddScoped<MaintenanceUnitOfWork>();
        services.TryAddSingleton(TimeProvider.System);

        var storeOptions = configuration.GetSection(AttachmentStoreOptions.SectionName).Get<AttachmentStoreOptions>() ?? new AttachmentStoreOptions();
        services.AddSingleton(storeOptions);
        services.AddSingleton<IAttachmentStore, LocalDiskAttachmentStore>();

        // Module-specific policies (blueprint RBAC matrix): who takes part in maintenance at all, and who may approve.
        services.AddAuthorizationBuilder()
            .AddPolicy(MaintenanceEndpoints.ParticipantPolicy, p => p.RequireRole(
                Roles.ReliabilityManager, Roles.MaintenancePlanner, Roles.Technician, Roles.Administrator))
            .AddPolicy(MaintenanceEndpoints.ApproverPolicy, p => p.RequireRole(
                Roles.ReliabilityManager, Roles.MaintenancePlanner, Roles.Administrator));
        return services;
    }

    public static IEndpointRouteBuilder MapMaintenanceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        MaintenanceEndpoints.Map(endpoints);
        return endpoints;
    }

    /// <summary>Applies this module's migrations (schema, tables, role grants, row-level security).</summary>
    public static Task MigrateMaintenanceAsync(this IServiceProvider services, CancellationToken cancellationToken = default) =>
        services.MigrateModuleAsync<MaintenanceDbContext>(cancellationToken);
}
