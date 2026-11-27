namespace SmartFactory.BuildingBlocks.Contracts;

/// <summary>The minimum another module may know about an asset.</summary>
public sealed record AssetReference(Guid Id, string Code, string Name);

/// <summary>
/// Query contract implemented by the Assets module and used by other modules (ADR-002: modules talk through public
/// contracts, never through each other's tables). Results are scoped to the current tenant: an asset of another
/// tenant is reported as not found.
/// </summary>
public interface IAssetDirectory
{
    Task<AssetReference?> FindAsync(Guid assetId, CancellationToken cancellationToken);
}
