using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SmartFactory.BuildingBlocks.Auditing;

namespace SmartFactory.Modules.Maintenance.Persistence;

/// <summary>Runs a change and its audit records in one database transaction: both are committed or neither is.</summary>
internal sealed class MaintenanceUnitOfWork(MaintenanceDbContext db, IAuditTrail audit)
{
    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<(T Result, IReadOnlyList<AuditRecord> Audit)>> work, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(work);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var (result, records) = await work(cancellationToken).ConfigureAwait(false);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        if (records.Count > 0)
        {
            await audit.RecordAsync(records, db.Database.GetDbConnection(), transaction.GetDbTransaction(), cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }
}
