using System.Collections.Concurrent;
using BorrowBox.Core.Loans;

namespace BorrowBox.Infrastructure.Loans;

public sealed class InMemoryLoanStore : ILoanStore
{
    private readonly ConcurrentDictionary<Guid, Loan> _loans = new();

    public Task AddAsync(Loan loan, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _loans[loan.Id] = loan;
        return Task.CompletedTask;
    }

    public Task<Loan?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _loans.TryGetValue(id, out var loan);
        return Task.FromResult(loan);
    }

    public Task<IReadOnlyCollection<Loan>> ListActiveByBorrowerAsync(Guid borrowerId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyCollection<Loan>>(
            _loans.Values.Where(loan => loan.BorrowerId == borrowerId && loan.IsActive).ToArray());
    }
}
