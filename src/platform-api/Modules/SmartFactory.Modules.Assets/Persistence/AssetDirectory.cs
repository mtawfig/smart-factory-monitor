using Microsoft.EntityFrameworkCore;
using SmartFactory.BuildingBlocks.Contracts;

namespace SmartFactory.Modules.Assets.Persistence;

/// <summary>Answers other modules' questions about assets; tenant filtering and RLS apply as for any query.</summary>
internal sealed class AssetDirectory(AssetsDbContext db) : IAssetDirectory
{
    public Task<AssetReference?> FindAsync(Guid assetId, CancellationToken cancellationToken) =>
        db.Assets.AsNoTracking()
            .Where(a => a.Id == assetId)
            .Select(a => new AssetReference(a.Id, a.Code, a.Name))
            .FirstOrDefaultAsync(cancellationToken);
}
