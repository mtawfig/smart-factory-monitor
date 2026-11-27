namespace SmartFactory.BuildingBlocks.Tenancy;

/// <summary>
/// Marks an entity that belongs to exactly one tenant. Such entities get, automatically:
/// an EF Core global query filter on <see cref="TenantId"/>, the tenant stamped on insert, a guard against
/// cross-tenant updates, and (in the module's migration) a PostgreSQL row-level security policy.
/// </summary>
public interface ITenantScoped
{
    string TenantId { get; }
}
