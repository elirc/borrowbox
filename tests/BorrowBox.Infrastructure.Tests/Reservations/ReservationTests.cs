using BorrowBox.Core.Borrowers;
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
        var service = new ReservationService(equipmentStore, borrowerStore, reservationStore);

        var reservation = await service.ReserveAsync(equipment.Id, borrower.Id, DateTimeOffset.UtcNow.AddHours(2), CancellationToken.None);
        await service.CancelAsync(reservation.Id, CancellationToken.None);

        Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
        Assert.Equal(EquipmentStatus.Available, equipment.Status);
    }
}
