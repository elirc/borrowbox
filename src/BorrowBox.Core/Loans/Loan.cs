using BorrowBox.Core.Borrowers;
using BorrowBox.Core.Common;
using BorrowBox.Core.Equipment;

namespace BorrowBox.Core.Loans;

public sealed class Loan
{
    public Loan(Guid id, Guid equipmentId, Guid borrowerId, DateTimeOffset checkedOutAt, DateOnly dueOn)
    {
        if (id == Guid.Empty || equipmentId == Guid.Empty || borrowerId == Guid.Empty)
            throw new BorrowBoxException("loan.identity_required", "Loan, equipment, and borrower IDs are required.");
        if (dueOn < DateOnly.FromDateTime(checkedOutAt.UtcDateTime))
            throw new BorrowBoxException("loan.due_date_invalid", "Due date cannot be before checkout.");
        Id = id;
        EquipmentId = equipmentId;
        BorrowerId = borrowerId;
        CheckedOutAt = checkedOutAt.ToUniversalTime();
        DueOn = dueOn;
    }

    public Guid Id { get; }
    public Guid EquipmentId { get; }
    public Guid BorrowerId { get; }
    public DateTimeOffset CheckedOutAt { get; }
    public DateOnly DueOn { get; }
    public DateTimeOffset? ReturnedAt { get; private set; }
    public bool IsActive => ReturnedAt is null;

    public void Return(DateTimeOffset returnedAt)
    {
        if (!IsActive) throw new BorrowBoxException("loan.already_returned", "Loan has already been returned.");
        if (returnedAt < CheckedOutAt) throw new BorrowBoxException("loan.return_time_invalid", "Return time cannot be before checkout.");
        ReturnedAt = returnedAt.ToUniversalTime();
    }
}

public interface ILoanStore
{
    Task AddAsync(Loan loan, CancellationToken cancellationToken);
    Task<Loan?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Loan>> ListActiveByBorrowerAsync(Guid borrowerId, CancellationToken cancellationToken);
}

public sealed class BorrowerLoanPolicy(int maximumActiveLoans)
{
    public int MaximumActiveLoans { get; } = maximumActiveLoans > 0
        ? maximumActiveLoans
        : throw new ArgumentOutOfRangeException(nameof(maximumActiveLoans));

    public void EnsureCanBorrow(int activeLoanCount)
    {
        if (activeLoanCount >= MaximumActiveLoans)
            throw new BorrowBoxException("borrower.loan_limit", $"Borrower may have at most {MaximumActiveLoans} active loans.");
    }
}

public sealed class CheckoutService(
    IEquipmentStore equipmentStore,
    IBorrowerStore borrowerStore,
    ILoanStore loanStore,
    BorrowerLoanPolicy policy,
    IClock? clock = null)
{
    public async Task<Loan> CheckOutAsync(Guid equipmentId, Guid borrowerId, DateOnly dueOn, CancellationToken cancellationToken)
    {
        var checkedOutAt = (clock ?? new SystemClock()).UtcNow;
        var equipment = await equipmentStore.GetAsync(equipmentId, cancellationToken).ConfigureAwait(false)
            ?? throw new BorrowBoxException("equipment.not_found", "Equipment was not found.");
        var borrower = await borrowerStore.GetAsync(borrowerId, cancellationToken).ConfigureAwait(false)
            ?? throw new BorrowBoxException("borrower.not_found", "Borrower was not found.");
        if (!borrower.IsActive) throw new BorrowBoxException("borrower.inactive", "Borrower is inactive.");
        var activeLoans = await loanStore.ListActiveByBorrowerAsync(borrower.Id, cancellationToken).ConfigureAwait(false);
        policy.EnsureCanBorrow(activeLoans.Count);

        var loan = new Loan(Guid.NewGuid(), equipment.Id, borrower.Id, checkedOutAt, dueOn);
        equipment = await equipmentStore.TryCheckOutAsync(equipmentId, cancellationToken).ConfigureAwait(false)
            ?? throw new BorrowBoxException("equipment.not_available", "Equipment is not available.");
        await loanStore.AddAsync(loan, cancellationToken).ConfigureAwait(false);
        return loan;
    }
}

public sealed class ReturnService(IEquipmentStore equipmentStore, ILoanStore loanStore, IClock? clock = null)
{
    public Task<Loan> ReturnAsync(Guid loanId, CancellationToken cancellationToken) =>
        ReturnAsync(loanId, (clock ?? new SystemClock()).UtcNow, cancellationToken);

    public async Task<Loan> ReturnAsync(Guid loanId, DateTimeOffset returnedAt, CancellationToken cancellationToken)
    {
        var loan = await loanStore.GetAsync(loanId, cancellationToken).ConfigureAwait(false)
            ?? throw new BorrowBoxException("loan.not_found", "Loan was not found.");
        if (!loan.IsActive)
            throw new BorrowBoxException("loan.already_returned", "Loan has already been returned.");
        var equipment = await equipmentStore.GetAsync(loan.EquipmentId, cancellationToken).ConfigureAwait(false)
            ?? throw new BorrowBoxException("equipment.not_found", "Equipment was not found.");
        if (equipment.Status != EquipmentStatus.CheckedOut)
            throw new BorrowBoxException("equipment.not_checked_out", "Equipment is not checked out for this loan.");
        loan.Return(returnedAt);
        equipment.Return();
        await equipmentStore.SaveAsync(equipment, cancellationToken).ConfigureAwait(false);
        return loan;
    }
}
