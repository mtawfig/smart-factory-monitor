using Microsoft.AspNetCore.Http;

namespace SmartFactory.BuildingBlocks.Tenancy;

/// <summary>Claim names used by the identity provider (Keycloak realm <c>smartfactory</c>).</summary>
public static class TenantClaims
{
    public const string TenantId = "tenant_id";
}

/// <summary>
/// Resolves the tenant for the current unit of work: an explicit <see cref="TenantScope"/> wins (background jobs,
/// seeding, tests); otherwise the <c>tenant_id</c> claim of the authenticated HTTP request.
/// </summary>
internal sealed class CurrentTenant(IHttpContextAccessor accessor) : ICurrentTenant
{
    public string? TenantId => TenantScope.Current ?? accessor.HttpContext?.User.FindFirst(TenantClaims.TenantId)?.Value;
}

/// <summary>
/// Runs code on behalf of one tenant outside an HTTP request. Flows across <c>await</c> (AsyncLocal) and is restored
/// on dispose, so scopes nest safely.
/// </summary>
public static class TenantScope
{
    private static readonly AsyncLocal<string?> Override = new();

    internal static string? Current => Override.Value;

    public static IDisposable Begin(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        var previous = Override.Value;
        Override.Value = tenantId;
        return new Restore(() => Override.Value = previous);
    }

    private sealed class Restore(Action restore) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                restore();
            }
        }
    }
}
