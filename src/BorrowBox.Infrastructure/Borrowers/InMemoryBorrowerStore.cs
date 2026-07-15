using System.Collections.Concurrent;
using BorrowBox.Core.Borrowers;

namespace BorrowBox.Infrastructure.Borrowers;

public sealed class InMemoryBorrowerStore : IBorrowerStore
{
    private readonly ConcurrentDictionary<Guid, Borrower> _borrowers = new();

    public Task AddAsync(Borrower borrower, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _borrowers[borrower.Id] = borrower;
        return Task.CompletedTask;
    }

    public Task<Borrower?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _borrowers.TryGetValue(id, out var borrower);
        return Task.FromResult(borrower);
    }
}
