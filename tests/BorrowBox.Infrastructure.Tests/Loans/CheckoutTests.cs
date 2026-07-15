using BorrowBox.Core.Borrowers;
using BorrowBox.Core.Common;
using BorrowBox.Core.Equipment;
using BorrowBox.Core.Loans;
using BorrowBox.Infrastructure.Borrowers;
using BorrowBox.Infrastructure.Equipment;
using BorrowBox.Infrastructure.Loans;

namespace BorrowBox.Infrastructure.Tests.Loans;

public sealed class CheckoutTests
{
    [Fact]
    public async Task CheckOut_AvailableEquipment_CreatesActiveLoan()
    {
        var equipmentStore = new InMemoryEquipmentStore();
        var borrowerStore = new InMemoryBorrowerStore();
        var loanStore = new InMemoryLoanStore();
        var equipment = new EquipmentItem(Guid.NewGuid(), "CAM-01", "Camera", "Media");
        var borrower = new Borrower(Guid.NewGuid(), "S123", "Ada");
        await equipmentStore.AddAsync(equipment, CancellationToken.None);
        await borrowerStore.AddAsync(borrower, CancellationToken.None);

        var loan = await new CheckoutService(equipmentStore, borrowerStore, loanStore, new BorrowerLoanPolicy(3))
            .CheckOutAsync(equipment.Id, borrower.Id, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)), CancellationToken.None);

        Assert.True(loan.IsActive);
        Assert.Equal(EquipmentStatus.CheckedOut, equipment.Status);
    }

    [Fact]
    public async Task CheckOut_InactiveBorrower_RejectsRule()
    {
        var equipmentStore = new InMemoryEquipmentStore();
        var borrowerStore = new InMemoryBorrowerStore();
        var equipment = new EquipmentItem(Guid.NewGuid(), "CAM-01", "Camera", "Media");
        var borrower = new Borrower(Guid.NewGuid(), "S123", "Ada");
        borrower.Deactivate();
        await equipmentStore.AddAsync(equipment, CancellationToken.None);
        await borrowerStore.AddAsync(borrower, CancellationToken.None);

        var error = await Assert.ThrowsAsync<BorrowBoxException>(() => new CheckoutService(equipmentStore, borrowerStore, new InMemoryLoanStore(), new BorrowerLoanPolicy(3))
            .CheckOutAsync(equipment.Id, borrower.Id, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)), CancellationToken.None));

        Assert.Equal("borrower.inactive", error.Code);
    }

    [Fact]
    public async Task Return_ActiveLoan_MakesEquipmentAvailable()
    {
        var equipmentStore = new InMemoryEquipmentStore();
        var borrowerStore = new InMemoryBorrowerStore();
        var loanStore = new InMemoryLoanStore();
        var equipment = new EquipmentItem(Guid.NewGuid(), "CAM-01", "Camera", "Media");
        var borrower = new Borrower(Guid.NewGuid(), "S123", "Ada");
        await equipmentStore.AddAsync(equipment, CancellationToken.None);
        await borrowerStore.AddAsync(borrower, CancellationToken.None);
        var loan = await new CheckoutService(equipmentStore, borrowerStore, loanStore, new BorrowerLoanPolicy(3))
            .CheckOutAsync(equipment.Id, borrower.Id, new DateOnly(2026, 8, 1), CancellationToken.None);

        await new ReturnService(equipmentStore, loanStore).ReturnAsync(loan.Id, new DateTimeOffset(2026, 7, 20, 10, 0, 0, TimeSpan.Zero), CancellationToken.None);

        Assert.False(loan.IsActive);
        Assert.Equal(EquipmentStatus.Available, equipment.Status);
    }

    [Fact]
    public async Task CheckOut_AtBorrowerLimit_RejectsNewLoan()
    {
        var equipmentStore = new InMemoryEquipmentStore();
        var borrowerStore = new InMemoryBorrowerStore();
        var loanStore = new InMemoryLoanStore();
        var borrower = new Borrower(Guid.NewGuid(), "S321", "Grace");
        var equipment = new EquipmentItem(Guid.NewGuid(), "CAM-09", "Camera", "Media");
        await borrowerStore.AddAsync(borrower, CancellationToken.None);
        await equipmentStore.AddAsync(equipment, CancellationToken.None);
        await loanStore.AddAsync(new Loan(Guid.NewGuid(), Guid.NewGuid(), borrower.Id, DateTimeOffset.UtcNow, new DateOnly(2099, 1, 1)), CancellationToken.None);
        await loanStore.AddAsync(new Loan(Guid.NewGuid(), Guid.NewGuid(), borrower.Id, DateTimeOffset.UtcNow, new DateOnly(2099, 1, 1)), CancellationToken.None);
        var service = new CheckoutService(equipmentStore, borrowerStore, loanStore, new BorrowerLoanPolicy(2));

        var error = await Assert.ThrowsAsync<BorrowBoxException>(() =>
            service.CheckOutAsync(equipment.Id, borrower.Id, new DateOnly(2099, 1, 1), CancellationToken.None));

        Assert.Equal("borrower.loan_limit", error.Code);
        Assert.Equal(EquipmentStatus.Available, equipment.Status);
    }
}
