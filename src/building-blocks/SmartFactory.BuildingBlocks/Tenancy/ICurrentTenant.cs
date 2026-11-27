namespace SmartFactory.BuildingBlocks.Tenancy;

/// <summary>
/// The tenant of the current request or message. Every tenant-scoped query, event and audit record
/// must take its tenant from here, never from user input (ADR-008).
/// </summary>
public interface ICurrentTenant
{
    /// <summary>Tenant identifier from the <c>tenant_id</c> claim, e.g. <c>tnt-alsafa</c>.</summary>
    string? TenantId { get; }

    /// <summary>True when a tenant has been resolved for this scope.</summary>
    bool HasTenant => !string.IsNullOrWhiteSpace(TenantId);
}
