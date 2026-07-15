using BorrowBox.Core.Equipment;
using BorrowBox.Core.Reservations;
using BorrowBox.Infrastructure.Equipment;
using BorrowBox.Infrastructure.Reservations;

namespace BorrowBox.Infrastructure.Tests.Reservations;

public sealed class ReservationExpiryTests
{
    [Fact]
    public async Task ExpireDue_ReleasesReservedEquipment()
    {
        var equipmentStore = new InMemoryEquipmentStore();
        var reservationStore = new InMemoryReservationStore();
        var equipment = new EquipmentItem(Guid.NewGuid(), "LAP-8", "Laptop", "Computing");
        equipment.Reserve();
        await equipmentStore.AddAsync(equipment, CancellationToken.None);
        var reservation = new Reservation(
            Guid.NewGuid(), equipment.Id, Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddHours(-2), DateTimeOffset.UtcNow.AddHours(-1));
        await reservationStore.AddAsync(reservation, CancellationToken.None);

        var count = await new ReservationExpiryService(reservationStore, equipmentStore)
            .ExpireDueAsync(CancellationToken.None);

        Assert.Equal(1, count);
        Assert.Equal(ReservationStatus.Expired, reservation.Status);
        Assert.Equal(EquipmentStatus.Available, equipment.Status);
    }
}
