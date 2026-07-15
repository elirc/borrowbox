using System.Collections.Concurrent;
using BorrowBox.Core.Equipment;

namespace BorrowBox.Infrastructure.Equipment;

public sealed class InMemoryEquipmentStore : IEquipmentStore
{
    private readonly ConcurrentDictionary<Guid, EquipmentItem> _items = new();

    public Task AddAsync(EquipmentItem item, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _items[item.Id] = item;
        return Task.CompletedTask;
    }

    public Task<EquipmentItem?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _items.TryGetValue(id, out var item);
        return Task.FromResult(item);
    }

    public Task<IReadOnlyCollection<EquipmentItem>> ListAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyCollection<EquipmentItem>>(_items.Values.ToArray());
    }
}
