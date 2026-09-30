using BorrowBox.Core.Borrowers;
using BorrowBox.Core.Common;
using BorrowBox.Core.Equipment;
using BorrowBox.Core.Reservations;
using BorrowBox.Infrastructure.Borrowers;
using BorrowBox.Infrastructure.Equipment;
using BorrowBox.Infrastructure.Reservations;

namespace BorrowBox.Infrastructure.Tests.Reservations;

public sealed class ReservationTests
{
    [Fact]
    public async Task Cancel_ReleasesEquipment()
    {
        var equipmentStore = new InMemoryEquipmentStore();
        var borrowerStore = new InMemoryBorrowerStore();
        var reservationStore = new InMemoryReservationStore();
        var equipment = new EquipmentItem(Guid.NewGuid(), "MIC-1", "Microphone", "Audio");
        var borrower = new Borrower(Guid.NewGuid(), "student@example.edu", "Student");
        await equipmentStore.AddAsync(equipment, CancellationToken.None);
        await borrowerStore.AddAsync(borrower, CancellationToken.None);
        var service = new ReservationService(equipmentStore, borrowerStore, reservationStore,
            new FixedClock(new DateTimeOffset(2026, 1, 2, 10, 0, 0, TimeSpan.Zero)));

        var reservation = await service.ReserveAsync(equipment.Id, borrower.Id,
            new DateTimeOffset(2026, 1, 2, 12, 0, 0, TimeSpan.Zero), CancellationToken.None);
        await service.CancelAsync(reservation.Id, CancellationToken.None);

        Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
        Assert.Equal(EquipmentStatus.Available, equipment.Status);
    }

    [Fact]
    public async Task InvalidExpiry_DoesNotReserveEquipment()
    {
        var equipmentStore = new InMemoryEquipmentStore();
        var borrowerStore = new InMemoryBorrowerStore();
        var reservationStore = new InMemoryReservationStore();
        var equipment = new EquipmentItem(Guid.NewGuid(), "MIC-2", "Microphone", "Audio");
        var borrower = new Borrower(Guid.NewGuid(), "student2@example.edu", "Student Two");
        await equipmentStore.AddAsync(equipment, CancellationToken.None);
        await borrowerStore.AddAsync(borrower, CancellationToken.None);
        var service = new ReservationService(equipmentStore, borrowerStore, reservationStore,
            new FixedClock(new DateTimeOffset(2026, 1, 2, 10, 0, 0, TimeSpan.Zero)));

        await Assert.ThrowsAsync<BorrowBoxException>(() => service.ReserveAsync(
            equipment.Id, borrower.Id, new DateTimeOffset(2026, 1, 2, 9, 0, 0, TimeSpan.Zero), CancellationToken.None));

        Assert.Equal(EquipmentStatus.Available, equipment.Status);
        Assert.Empty(await reservationStore.ListAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Cancel_MismatchedEquipment_DoesNotChangeReservation()
    {
        var equipmentStore = new InMemoryEquipmentStore();
        var borrowerStore = new InMemoryBorrowerStore();
        var reservationStore = new InMemoryReservationStore();
        var equipment = new EquipmentItem(Guid.NewGuid(), "MIC-3", "Microphone", "Audio");
        var borrower = new Borrower(Guid.NewGuid(), "student3@example.edu", "Student Three");
        var reservation = new Reservation(Guid.NewGuid(), equipment.Id, borrower.Id,
            new DateTimeOffset(2026, 1, 2, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 1, 2, 12, 0, 0, TimeSpan.Zero));
        await equipmentStore.AddAsync(equipment, CancellationToken.None);
        await borrowerStore.AddAsync(borrower, CancellationToken.None);
        await reservationStore.AddAsync(reservation, CancellationToken.None);

        await Assert.ThrowsAsync<BorrowBoxException>(() => new ReservationService(
            equipmentStore, borrowerStore, reservationStore).CancelAsync(reservation.Id, CancellationToken.None));

        Assert.Equal(ReservationStatus.Active, reservation.Status);
        Assert.Equal(EquipmentStatus.Available, equipment.Status);
    }

    private sealed record FixedClock(DateTimeOffset UtcNow) : IClock;
}
