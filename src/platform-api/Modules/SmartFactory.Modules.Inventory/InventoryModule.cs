using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SmartFactory.Modules.Inventory;

/// <summary>
/// Public surface of the Inventory module. This is the only public type in the assembly; everything else is
/// internal so other modules cannot reach in (ADR-002). The host calls the two methods below and
/// nothing else.
/// </summary>
public static class InventoryModule
{
    /// <summary>Database schema owned by this module (ADR-007).</summary>
    public const string Schema = "inventory";

    /// <summary>Route prefix for the module's endpoints.</summary>
    public const string RoutePrefix = "/api/inventory";

    public static IServiceCollection AddInventoryModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        // Week 2+: DbContext for the "inventory" schema, repositories, handlers.
        return services;
    }

    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        var group = endpoints.MapGroup(RoutePrefix).WithTags("Inventory");
        group.MapGet("/ping", () => Results.Ok(new { module = Schema, status = "scaffold" }))
            .WithName("Inventory.Ping")
            .WithSummary("Module liveness marker; replaced by real endpoints from Week 2.")
            .RequireAuthorization();
        return endpoints;
    }
}
