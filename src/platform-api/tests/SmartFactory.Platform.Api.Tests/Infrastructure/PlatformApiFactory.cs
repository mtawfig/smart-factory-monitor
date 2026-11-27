using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SmartFactory.Modules.Assets;
using Testcontainers.PostgreSql;

namespace SmartFactory.Platform.Api.Tests.Infrastructure;

/// <summary>
/// The real Platform API on a real PostgreSQL 16, migrated, with the plant seeded into two tenants that have
/// identical asset codes. Database source, in order: <c>SF_TEST_POSTGRES</c> (an admin connection string to an
/// existing server; a throw-away database is created on it), else a Testcontainers PostgreSQL. Without either the
/// database tests are skipped locally and fail in CI.
/// </summary>
public sealed class PlatformApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private string? _adminConnectionString;
    private string? _databaseName;

    public string? ConnectionString { get; private set; }

    public string? UnavailableReason { get; private set; }

    /// <summary>Attachment folder for this test run, deleted at the end.</summary>
    public string AttachmentRoot { get; } = Path.Combine(Path.GetTempPath(), $"sf-test-attachments-{Guid.NewGuid():N}");

    public async ValueTask InitializeAsync()
    {
        try
        {
            var external = Environment.GetEnvironmentVariable("SF_TEST_POSTGRES");
            if (!string.IsNullOrWhiteSpace(external))
            {
                _adminConnectionString = external;
                _databaseName = $"sf_test_{Guid.NewGuid():N}";
                await using var admin = new NpgsqlConnection(external);
                await admin.OpenAsync();
                await using var create = new NpgsqlCommand($"CREATE DATABASE {_databaseName}", admin);
                await create.ExecuteNonQueryAsync();
                ConnectionString = new NpgsqlConnectionStringBuilder(external) { Database = _databaseName }.ConnectionString;
            }
            else
            {
                _container = new PostgreSqlBuilder("postgres:16-alpine").Build();
                await _container.StartAsync();
                ConnectionString = _container.GetConnectionString();
            }
        }
        catch (Exception ex) when (Environment.GetEnvironmentVariable("CI") is null)
        {
            UnavailableReason = $"No PostgreSQL for integration tests (set SF_TEST_POSTGRES or start Docker): {ex.Message}";
            return;
        }

        // First access builds the host, which migrates (Database:MigrateOnStartup). Then seed both tenants.
        await Services.SeedAssetsFromPlantSpecAsync(RepositoryPaths.PlantSpec, TestUsers.TenantA);
        await Services.SeedAssetsFromPlantSpecAsync(RepositoryPaths.PlantSpec, TestUsers.TenantB);
    }

    /// <summary>Call at the start of every database test.</summary>
    public void RequireDatabase()
    {
        if (UnavailableReason is not null)
        {
            Assert.Skip(UnavailableReason);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Platform", ConnectionString ?? "Host=unavailable");
        builder.UseSetting("Database:MigrateOnStartup", "true");
        builder.UseSetting("Database:ApplicationRole", "sf_app");
        builder.UseSetting("Seed:Enabled", "false");
        builder.UseSetting("Attachments:LocalRoot", AttachmentRoot);
        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                o.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        if (Directory.Exists(AttachmentRoot))
        {
            Directory.Delete(AttachmentRoot, recursive: true);
        }

        if (_container is not null)
        {
            await _container.DisposeAsync();
        }

        if (_adminConnectionString is not null && _databaseName is not null)
        {
            NpgsqlConnection.ClearAllPools();
            await using var admin = new NpgsqlConnection(_adminConnectionString);
            await admin.OpenAsync();
            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS {_databaseName} WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }
}

[CollectionDefinition(Name)]
public sealed class DatabaseTestGroup : ICollectionFixture<PlatformApiFactory>
{
    public const string Name = "database";
}

/// <summary>The host without any database, for tests of the HTTP pipeline itself (health, headers, auth, OpenAPI).</summary>
public sealed class NoDatabaseFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Platform", "Host=unavailable");
        builder.UseSetting("Database:MigrateOnStartup", "false");
        builder.UseSetting("Seed:Enabled", "false");
    }
}
