using System.Security.Claims;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using SmartFactory.BuildingBlocks;
using SmartFactory.Platform.Api;
using SmartFactory.BuildingBlocks.Auth;
using SmartFactory.BuildingBlocks.Persistence;
using SmartFactory.BuildingBlocks.Telemetry;
using SmartFactory.BuildingBlocks.Tenancy;
using SmartFactory.Modules.Assets;
using SmartFactory.Modules.Audit;
using SmartFactory.Modules.Inventory;
using SmartFactory.Modules.Maintenance;

// Why: the container HEALTHCHECK runs this same binary with --health-probe. The aspnet image has no curl, so the
// probe asks the running instance for /health/live and exits 0/1 without building a second host.
if (args.Contains("--health-probe"))
{
    return await HealthProbe.RunAsync();
}

var builder = WebApplication.CreateBuilder(args);

// ── Cross-cutting (BuildingBlocks) ───────────────────────────────────────────
builder.Services.AddPlatformAuthentication(builder.Configuration);
builder.Services.AddPlatformPersistence(builder.Configuration);
builder.Services.AddPlatformTelemetry(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi(o => o.AddDocumentTransformer((doc, _, _) =>
{
    doc.Info.Title = "SmartFactory AI — Platform API";
    doc.Info.Description = "Advisory-only predictive-maintenance platform. All data is synthetic. No PLC control path exists.";
    return Task.CompletedTask;
}));

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .WithExposedHeaders(SyntheticDataHeaderMiddleware.HeaderName)));

// Why: /health/live answers "process is up"; /health/ready adds dependency checks tagged "ready".
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Platform") ?? string.Empty, name: "postgres", tags: ["ready"])
    .AddRedis(builder.Configuration.GetConnectionString("Redis") ?? string.Empty, name: "redis", tags: ["ready"]);

// ── Modules (ADR-002) ────────────────────────────────────────────────────────
builder.Services
    .AddAssetsModule(builder.Configuration)
    .AddMaintenanceModule(builder.Configuration)
    .AddInventoryModule(builder.Configuration)
    .AddAuditModule(builder.Configuration);

var app = builder.Build();

await InitialiseDatabaseAsync(app);

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseSyntheticDataHeader();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapScalarApiReference(o => o.WithTitle("SmartFactory AI — Platform API (synthetic data)"));

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = r => r.Tags.Contains("ready") });

app.MapGet("/api/me", (ClaimsPrincipal user, ICurrentTenant tenant) =>
        Results.Ok(PlatformAuthenticationExtensions.DescribeCaller(user) with { TenantId = tenant.TenantId }))
    .WithName("Me")
    .WithSummary("Who am I: subject, username, tenant and roles from the validated token.")
    .RequireAuthorization();

app.MapAssetsEndpoints()
   .MapMaintenanceEndpoints()
   .MapInventoryEndpoints()
   .MapAuditEndpoints();

await app.RunAsync();
return 0;

// Migrations and seeding run only when configured (local Compose, tests). In Azure, migrations run from the deploy
// pipeline with an elevated login and the app starts with neither flag set.
static async Task InitialiseDatabaseAsync(WebApplication app)
{
    var database = app.Configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();
    if (database.MigrateOnStartup)
    {
        await app.Services.MigrateAssetsAsync();
        await app.Services.MigrateAuditAsync();
        await app.Services.MigrateMaintenanceAsync();
    }

    var seedPath = app.Configuration["Seed:PlantSpecPath"];
    if (app.Configuration.GetValue<bool>("Seed:Enabled") && !string.IsNullOrWhiteSpace(seedPath))
    {
        await app.Services.SeedAssetsFromPlantSpecAsync(Path.GetFullPath(seedPath, app.Environment.ContentRootPath));
    }
}

/// <summary>Marker so integration tests can reference the host with WebApplicationFactory.</summary>
public partial class Program;
