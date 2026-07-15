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

    public void Return(DateTimeOffset returnedAt) => ReturnedAt = returnedAt.ToUniversalTime();
}

public interface ILoanStore
{
    Task AddAsync(Loan loan, CancellationToken cancellationToken);
    Task<Loan?> GetAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class CheckoutService(IEquipmentStore equipmentStore, IBorrowerStore borrowerStore, ILoanStore loanStore)
{
    public async Task<Loan> CheckOutAsync(Guid equipmentId, Guid borrowerId, DateOnly dueOn, CancellationToken cancellationToken)
    {
        var equipment = await equipmentStore.GetAsync(equipmentId, cancellationToken).ConfigureAwait(false)
            ?? throw new BorrowBoxException("equipment.not_found", "Equipment was not found.");
        var borrower = await borrowerStore.GetAsync(borrowerId, cancellationToken).ConfigureAwait(false)
            ?? throw new BorrowBoxException("borrower.not_found", "Borrower was not found.");
        if (!borrower.IsActive) throw new BorrowBoxException("borrower.inactive", "Borrower is inactive.");

        equipment.CheckOut();
        var loan = new Loan(Guid.NewGuid(), equipment.Id, borrower.Id, DateTimeOffset.UtcNow, dueOn);
        await loanStore.AddAsync(loan, cancellationToken).ConfigureAwait(false);
        return loan;
    }
}

public sealed class ReturnService(IEquipmentStore equipmentStore, ILoanStore loanStore)
{
    public async Task<Loan> ReturnAsync(Guid loanId, DateTimeOffset returnedAt, CancellationToken cancellationToken)
    {
        var loan = await loanStore.GetAsync(loanId, cancellationToken).ConfigureAwait(false)
            ?? throw new BorrowBoxException("loan.not_found", "Loan was not found.");
        var equipment = await equipmentStore.GetAsync(loan.EquipmentId, cancellationToken).ConfigureAwait(false)
            ?? throw new BorrowBoxException("equipment.not_found", "Equipment was not found.");
        loan.Return(returnedAt);
        equipment.Return();
        return loan;
    }
}
