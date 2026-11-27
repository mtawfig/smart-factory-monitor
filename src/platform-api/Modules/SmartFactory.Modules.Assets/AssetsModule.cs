using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartFactory.BuildingBlocks.Auth;
using SmartFactory.BuildingBlocks.Contracts;
using SmartFactory.BuildingBlocks.Persistence;
using SmartFactory.BuildingBlocks.Tenancy;
using SmartFactory.Modules.Assets.Endpoints;
using SmartFactory.Modules.Assets.Persistence;
using SmartFactory.Modules.Assets.Seeding;

namespace SmartFactory.Modules.Assets;

/// <summary>
/// Public surface of the Assets module: the only public type in the assembly (ADR-002). The host calls these
/// methods; everything else is internal so other modules cannot reach in.
/// </summary>
public static class AssetsModule
{
    /// <summary>Database schema owned by this module (ADR-007).</summary>
    public const string Schema = "assets";

    /// <summary>Route prefix for the module's endpoints.</summary>
    public const string RoutePrefix = "/api/assets";

    public static IServiceCollection AddAssetsModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        services.AddModuleDbContext<AssetsDbContext>(configuration, Schema);
        services.AddScoped<AssetsUnitOfWork>();
        services.AddScoped<PlantSeeder>();
        services.AddScoped<IAssetDirectory, AssetDirectory>();
        return services;
    }

    public static IEndpointRouteBuilder MapAssetsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        AssetEndpoints.Map(endpoints);
        return endpoints;
    }

    /// <summary>Applies this module's migrations (schema, tables, role grants, row-level security).</summary>
    public static Task MigrateAssetsAsync(this IServiceProvider services, CancellationToken cancellationToken = default) =>
        services.MigrateModuleAsync<AssetsDbContext>(cancellationToken);

    /// <summary>
    /// Seeds tenant, plant, lines, assets and sensors from a <c>plant.yaml</c> file. Idempotent. Returns the number of
    /// records created. <paramref name="tenantIdOverride"/> seeds the same plant into another tenant (tests).
    /// </summary>
    public static async Task<int> SeedAssetsFromPlantSpecAsync(
        this IServiceProvider services, string plantSpecPath, string? tenantIdOverride = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plantSpecPath);
        var spec = PlantSpecification.Load(plantSpecPath);
        var tenantId = tenantIdOverride ?? spec.Tenant.Id;

        using var tenantScope = TenantScope.Begin(tenantId);
        using var actorScope = ActorScope.Begin("seed");
        using var scope = services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<PlantSeeder>();
        return await seeder.SeedAsync(spec, tenantId, cancellationToken).ConfigureAwait(false);
    }
}
