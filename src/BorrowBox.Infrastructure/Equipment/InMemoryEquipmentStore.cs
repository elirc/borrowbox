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

    public Task SaveAsync(EquipmentItem item, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _items[item.Id] = item;
        return Task.CompletedTask;
    }

    public Task<EquipmentItem?> TryCheckOutAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_items.TryGetValue(id, out var item)) return Task.FromResult<EquipmentItem?>(null);
        lock (item)
        {
            if (item.Status != EquipmentStatus.Available) return Task.FromResult<EquipmentItem?>(null);
            item.CheckOut();
            return Task.FromResult<EquipmentItem?>(item);
        }
    }
}
