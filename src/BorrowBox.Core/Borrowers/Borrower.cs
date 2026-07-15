using BorrowBox.Core.Common;

namespace BorrowBox.Core.Borrowers;

public sealed class Borrower
{
    public Borrower(Guid id, string collegeId, string displayName)
    {
        if (id == Guid.Empty) throw new BorrowBoxException("borrower.id_required", "Borrower ID is required.");
        if (string.IsNullOrWhiteSpace(collegeId)) throw new BorrowBoxException("borrower.college_id_required", "College ID is required.");
        if (string.IsNullOrWhiteSpace(displayName)) throw new BorrowBoxException("borrower.name_required", "Display name is required.");
        Id = id;
        CollegeId = collegeId.Trim().ToUpperInvariant();
        DisplayName = displayName.Trim();
    }

    public Guid Id { get; }
    public string CollegeId { get; }
    public string DisplayName { get; }
    public bool IsActive { get; private set; } = true;

    public void Deactivate() => IsActive = false;
}

public interface IBorrowerStore
{
    Task AddAsync(Borrower borrower, CancellationToken cancellationToken);
    Task<Borrower?> GetAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class BorrowerService(IBorrowerStore store)
{
    public async Task<Borrower> RegisterAsync(string collegeId, string displayName, CancellationToken cancellationToken)
    {
        var borrower = new Borrower(Guid.NewGuid(), collegeId, displayName);
        await store.AddAsync(borrower, cancellationToken).ConfigureAwait(false);
        return borrower;
    }

    public Task<Borrower?> GetAsync(Guid id, CancellationToken cancellationToken) => store.GetAsync(id, cancellationToken);
}
