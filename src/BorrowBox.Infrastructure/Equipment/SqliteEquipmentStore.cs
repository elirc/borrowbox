using BorrowBox.Core.Equipment;
using BorrowBox.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BorrowBox.Infrastructure.Equipment;

public sealed class SqliteEquipmentStore(BorrowBoxDbContext dbContext) : IEquipmentStore
{
    public async Task AddAsync(EquipmentItem item, CancellationToken cancellationToken)
    {
        dbContext.Equipment.Add(ToRow(item));
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<EquipmentItem?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var row = await dbContext.Equipment.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken).ConfigureAwait(false);
        return row is null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyCollection<EquipmentItem>> ListAsync(CancellationToken cancellationToken)
    {
        var rows = await dbContext.Equipment.AsNoTracking().ToArrayAsync(cancellationToken).ConfigureAwait(false);
        return rows.Select(ToDomain).ToArray();
    }

    public async Task SaveAsync(EquipmentItem item, CancellationToken cancellationToken)
    {
        var row = await dbContext.Equipment.SingleAsync(candidate => candidate.Id == item.Id, cancellationToken)
            .ConfigureAwait(false);
        row.Name = item.Name;
        row.Category = item.Category;
        row.Status = item.Status.ToString();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<EquipmentItem?> TryCheckOutAsync(Guid id, CancellationToken cancellationToken)
    {
        var changed = await dbContext.Equipment
            .Where(row => row.Id == id && row.Status == nameof(EquipmentStatus.Available))
            .ExecuteUpdateAsync(update => update.SetProperty(
                row => row.Status, nameof(EquipmentStatus.CheckedOut)), cancellationToken)
            .ConfigureAwait(false);
        return changed == 1 ? await GetAsync(id, cancellationToken).ConfigureAwait(false) : null;
    }

    private static EquipmentRow ToRow(EquipmentItem item) => new()
    {
        Id = item.Id,
        AssetTag = item.AssetTag,
        Name = item.Name,
        Category = item.Category,
        Status = item.Status.ToString()
    };

    private static EquipmentItem ToDomain(EquipmentRow row) => new(
        row.Id, row.AssetTag, row.Name, row.Category, Enum.Parse<EquipmentStatus>(row.Status));
}
