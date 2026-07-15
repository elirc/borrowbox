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

    [Fact]
    public async Task Return_SameLoanTwice_IsRejected()
    {
        var equipmentStore = new InMemoryEquipmentStore();
        var loanStore = new InMemoryLoanStore();
        var equipment = new EquipmentItem(Guid.NewGuid(), "MIC-8", "Microphone", "Audio", EquipmentStatus.CheckedOut);
        var loan = new Loan(Guid.NewGuid(), equipment.Id, Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-1), new DateOnly(2099, 1, 1));
        await equipmentStore.AddAsync(equipment, CancellationToken.None);
        await loanStore.AddAsync(loan, CancellationToken.None);
        var service = new ReturnService(equipmentStore, loanStore);
        await service.ReturnAsync(loan.Id, DateTimeOffset.UtcNow, CancellationToken.None);

        var error = await Assert.ThrowsAsync<BorrowBoxException>(() =>
            service.ReturnAsync(loan.Id, DateTimeOffset.UtcNow, CancellationToken.None));

        Assert.Equal("loan.already_returned", error.Code);
    }

    [Fact]
    public async Task CheckOut_TwoCompetingRequests_OnlyOneSucceeds()
    {
        var equipment = new EquipmentItem(Guid.NewGuid(), "MIC-9", "Microphone", "Audio");
        var equipmentStore = new RacingEquipmentStore(equipment);
        var borrowerStore = new InMemoryBorrowerStore();
        var loanStore = new InMemoryLoanStore();
        var firstBorrower = new Borrower(Guid.NewGuid(), "S1", "First");
        var secondBorrower = new Borrower(Guid.NewGuid(), "S2", "Second");
        await borrowerStore.AddAsync(firstBorrower, CancellationToken.None);
        await borrowerStore.AddAsync(secondBorrower, CancellationToken.None);
        var service = new CheckoutService(equipmentStore, borrowerStore, loanStore, new BorrowerLoanPolicy(3));

        var attempts = await Task.WhenAll(
            Attempt(() => service.CheckOutAsync(equipment.Id, firstBorrower.Id, new DateOnly(2099, 1, 1), CancellationToken.None)),
            Attempt(() => service.CheckOutAsync(equipment.Id, secondBorrower.Id, new DateOnly(2099, 1, 1), CancellationToken.None)));

        Assert.Equal(1, attempts.Count(success => success));
    }

    private static async Task<bool> Attempt(Func<Task<Loan>> action)
    {
        try { await action(); return true; }
        catch (BorrowBoxException) { return false; }
    }

    private sealed class RacingEquipmentStore(EquipmentItem source) : IEquipmentStore
    {
        private readonly TaskCompletionSource _bothReading = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _readers;

        public Task AddAsync(EquipmentItem item, CancellationToken cancellationToken) => Task.CompletedTask;
        public async Task<EquipmentItem?> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _readers) == 2) _bothReading.SetResult();
            await _bothReading.Task.WaitAsync(cancellationToken);
            return new EquipmentItem(source.Id, source.AssetTag, source.Name, source.Category, source.Status);
        }
        public Task<IReadOnlyCollection<EquipmentItem>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<EquipmentItem>>([source]);
        public Task SaveAsync(EquipmentItem item, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
