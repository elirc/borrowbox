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

        var loan = await new CheckoutService(equipmentStore, borrowerStore, loanStore)
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

        var error = await Assert.ThrowsAsync<BorrowBoxException>(() => new CheckoutService(equipmentStore, borrowerStore, new InMemoryLoanStore())
            .CheckOutAsync(equipment.Id, borrower.Id, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)), CancellationToken.None));

        Assert.Equal("borrower.inactive", error.Code);
    }
}
