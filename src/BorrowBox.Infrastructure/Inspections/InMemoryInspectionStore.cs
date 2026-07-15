using System.Collections.Concurrent;
using BorrowBox.Core.Inspections;

namespace BorrowBox.Infrastructure.Inspections;

public sealed class InMemoryInspectionStore : IInspectionStore
{
    private readonly ConcurrentDictionary<Guid, Inspection> _items = new();
    public Task AddAsync(Inspection inspection, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _items[inspection.Id] = inspection;
        return Task.CompletedTask;
    }
    public Task<Inspection?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _items.TryGetValue(id, out var inspection);
        return Task.FromResult(inspection);
    }
    public Task<IReadOnlyCollection<Inspection>> ListAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyCollection<Inspection>>(_items.Values.ToArray());
    }
}
