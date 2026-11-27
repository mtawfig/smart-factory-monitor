using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartFactory.BuildingBlocks.Tenancy;

namespace SmartFactory.BuildingBlocks.Auth;

/// <summary>Binds the <c>Authentication</c> configuration section.</summary>
public sealed class AuthenticationOptions
{
    public const string SectionName = "Authentication";

    /// <summary>OIDC issuer, e.g. <c>http://localhost:8180/realms/smartfactory</c>.</summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>Expected <c>aud</c> claim; the Keycloak client id of the API.</summary>
    public string Audience { get; set; } = "smartfactory-api";

    /// <summary>False only for local Compose, where Keycloak runs over plain HTTP.</summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>
    /// Optional discovery URL when the API reaches the identity provider on a different address than the public
    /// issuer. In Compose the browser sees <c>http://localhost:8180</c> (the token issuer) while the API container
    /// reaches Keycloak at <c>http://keycloak:8080</c>. Empty means "derive from <see cref="Authority"/>".
    /// </summary>
    public string MetadataAddress { get; set; } = string.Empty;
}

public static class PlatformAuthenticationExtensions
{
    /// <summary>
    /// JWT bearer authentication against Keycloak plus the persona policies (ADR-008).
    /// Keycloak puts realm roles under <c>realm_access.roles</c>; a claims transformation lifts them to
    /// standard role claims so <c>[Authorize(Roles = ...)]</c> and policies work unchanged.
    /// </summary>
    public static IServiceCollection AddPlatformAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(AuthenticationOptions.SectionName).Get<AuthenticationOptions>()
                      ?? new AuthenticationOptions();

        services.AddHttpContextAccessor();
        services.AddSingleton<ICurrentTenant, CurrentTenant>();
        services.AddSingleton<ICurrentActor, CurrentActor>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwt =>
            {
                jwt.Authority = options.Authority;
                jwt.Audience = options.Audience;
                jwt.RequireHttpsMetadata = options.RequireHttpsMetadata;
                if (!string.IsNullOrWhiteSpace(options.MetadataAddress))
                {
                    jwt.MetadataAddress = options.MetadataAddress;
                    jwt.TokenValidationParameters.ValidIssuer = options.Authority;
                }
                jwt.MapInboundClaims = false; // Why: keep Keycloak claim names (preferred_username, tenant_id) as-is.
                jwt.TokenValidationParameters.NameClaimType = "preferred_username";
                jwt.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;
                jwt.TokenValidationParameters.ValidateIssuer = true;
                jwt.TokenValidationParameters.ValidateAudience = true;
                jwt.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds(30);

                jwt.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        KeycloakRoles.LiftRealmRoles(context.Principal);
                        return Task.CompletedTask;
                    },
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.ReliabilityManager, p => p.RequireRole(Roles.ReliabilityManager, Roles.Administrator))
            .AddPolicy(Policies.MaintenancePlanner, p => p.RequireRole(Roles.MaintenancePlanner, Roles.Administrator))
            .AddPolicy(Policies.Technician, p => p.RequireRole(Roles.Technician, Roles.MaintenancePlanner, Roles.Administrator))
            .AddPolicy(Policies.MlEngineer, p => p.RequireRole(Roles.MlEngineer, Roles.Administrator))
            .AddPolicy(Policies.Administrator, p => p.RequireRole(Roles.Administrator))
            .AddPolicy(Policies.TenantMember, p => p.RequireAuthenticatedUser().RequireClaim(TenantClaims.TenantId));

        return services;
    }

    /// <summary>Returns a small description of the caller: who they are, which tenant, which roles.</summary>
    public static CallerInfo DescribeCaller(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return new CallerInfo(
            Subject: user.FindFirst("sub")?.Value,
            Username: user.Identity?.Name,
            TenantId: user.FindFirst(TenantClaims.TenantId)?.Value,
            Roles: [.. user.FindAll(ClaimTypes.Role).Select(c => c.Value).Order()]);
    }
}

/// <summary>Who is calling. Returned by <c>GET /api/me</c>; used by the SPA after login.</summary>
public sealed record CallerInfo(string? Subject, string? Username, string? TenantId, IReadOnlyList<string> Roles);

/// <summary>Realm role names as defined in the Keycloak realm export.</summary>
public static class Roles
{
    public const string ReliabilityManager = "reliability-manager";
    public const string MaintenancePlanner = "maintenance-planner";
    public const string Technician = "technician";
    public const string MlEngineer = "ml-engineer";
    public const string Administrator = "administrator";
}

/// <summary>Authorization policy names. Endpoints reference these, never raw role strings.</summary>
public static class Policies
{
    public const string ReliabilityManager = "ReliabilityManager";
    public const string MaintenancePlanner = "MaintenancePlanner";
    public const string Technician = "Technician";
    public const string MlEngineer = "MlEngineer";
    public const string Administrator = "Administrator";
    public const string TenantMember = "TenantMember";
}
