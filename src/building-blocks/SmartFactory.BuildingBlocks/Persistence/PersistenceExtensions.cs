using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SmartFactory.BuildingBlocks.Persistence;

public static class PersistenceExtensions
{
    /// <summary>Shared persistence services: options and the two tenancy interceptors.</summary>
    public static IServiceCollection AddPlatformPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DatabaseOptions>().Bind(configuration.GetSection(DatabaseOptions.SectionName));
        services.TryAddScoped<TenantSessionInterceptor>();
        services.TryAddScoped<TenantStampingInterceptor>();
        return services;
    }

    /// <summary>
    /// Registers a module DbContext against the shared database: its own schema, its own migrations history table
    /// inside that schema, snake_case names, and the tenancy interceptors.
    /// </summary>
    public static IServiceCollection AddModuleDbContext<TContext>(this IServiceCollection services, IConfiguration configuration, string schema)
        where TContext : DbContext
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);
        var connectionString = configuration.GetConnectionString(DatabaseOptions.ConnectionStringName) ?? string.Empty;

        services.AddDbContext<TContext>((sp, options) => options
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", schema))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(sp.GetRequiredService<TenantSessionInterceptor>(), sp.GetRequiredService<TenantStampingInterceptor>()));
        return services;
    }

    /// <summary>Applies pending migrations for a module, with the login's own privileges.</summary>
    public static async Task MigrateModuleAsync<TContext>(this IServiceProvider services, CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        using var scope = services.CreateScope();
        using var elevated = ElevatedDatabaseScope.Begin();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }
}
