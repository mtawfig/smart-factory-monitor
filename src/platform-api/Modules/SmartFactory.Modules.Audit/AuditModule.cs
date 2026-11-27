using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartFactory.BuildingBlocks.Auditing;
using SmartFactory.BuildingBlocks.Persistence;
using SmartFactory.Modules.Audit.Endpoints;
using SmartFactory.Modules.Audit.Persistence;

namespace SmartFactory.Modules.Audit;

/// <summary>
/// Public surface of the Audit module (ADR-002). Other modules never reference this assembly; they write through
/// the <see cref="IAuditTrail"/> contract in BuildingBlocks, which this module implements.
/// </summary>
public static class AuditModule
{
    /// <summary>Database schema owned by this module (ADR-007).</summary>
    public const string Schema = "audit";

    /// <summary>Route prefix for the module's endpoints.</summary>
    public const string RoutePrefix = "/api/audit";

    public static IServiceCollection AddAuditModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        services.AddModuleDbContext<AuditDbContext>(configuration, Schema);
        services.AddScoped<IAuditTrail, AuditTrail>();
        return services;
    }

    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        AuditEndpoints.Map(endpoints);
        return endpoints;
    }

    /// <summary>Applies this module's migrations (table, append-only trigger, grants, row-level security).</summary>
    public static Task MigrateAuditAsync(this IServiceProvider services, CancellationToken cancellationToken = default) =>
        services.MigrateModuleAsync<AuditDbContext>(cancellationToken);
}
