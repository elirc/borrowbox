using BorrowBox.Core.Borrowers;
using BorrowBox.Core.Common;
using BorrowBox.Core.Equipment;

namespace BorrowBox.Core.Reservations;

public enum ReservationStatus { Active, Collected, Cancelled, Expired }

public sealed class Reservation
{
    public Reservation(Guid id, Guid equipmentId, Guid borrowerId, DateTimeOffset reservedAt, DateTimeOffset expiresAt)
    {
        if (id == Guid.Empty || equipmentId == Guid.Empty || borrowerId == Guid.Empty)
            throw new BorrowBoxException("reservation.identity_required", "Reservation, equipment, and borrower IDs are required.");
        if (expiresAt <= reservedAt) throw new BorrowBoxException("reservation.expiry_invalid", "Expiry must be after reservation time.");
        Id = id;
        EquipmentId = equipmentId;
        BorrowerId = borrowerId;
        ReservedAt = reservedAt.ToUniversalTime();
        ExpiresAt = expiresAt.ToUniversalTime();
    }

    public Guid Id { get; }
    public Guid EquipmentId { get; }
    public Guid BorrowerId { get; }
    public DateTimeOffset ReservedAt { get; }
    public DateTimeOffset ExpiresAt { get; }
    public ReservationStatus Status { get; private set; } = ReservationStatus.Active;

    public void Collect()
    {
        EnsureActive();
        Status = ReservationStatus.Collected;
    }

    public void Cancel()
    {
        EnsureActive();
        Status = ReservationStatus.Cancelled;
    }

    public void Expire()
    {
        EnsureActive();
        Status = ReservationStatus.Expired;
    }

    private void EnsureActive()
    {
        if (Status != ReservationStatus.Active)
            throw new BorrowBoxException("reservation.not_active", "Reservation is no longer active.");
    }
}

public interface IReservationStore
{
    Task AddAsync(Reservation reservation, CancellationToken cancellationToken);
    Task<Reservation?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Reservation>> ListAsync(CancellationToken cancellationToken);
}

public sealed class ReservationService(
    IEquipmentStore equipmentStore,
    IBorrowerStore borrowerStore,
    IReservationStore reservationStore)
{
    public async Task<Reservation> ReserveAsync(Guid equipmentId, Guid borrowerId, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        var equipment = await equipmentStore.GetAsync(equipmentId, cancellationToken).ConfigureAwait(false)
            ?? throw new BorrowBoxException("equipment.not_found", "Equipment was not found.");
        var borrower = await borrowerStore.GetAsync(borrowerId, cancellationToken).ConfigureAwait(false)
            ?? throw new BorrowBoxException("borrower.not_found", "Borrower was not found.");
        if (!borrower.IsActive) throw new BorrowBoxException("borrower.inactive", "Borrower is inactive.");

        equipment.Reserve();
        var reservation = new Reservation(Guid.NewGuid(), equipment.Id, borrower.Id, DateTimeOffset.UtcNow, expiresAt);
        await equipmentStore.SaveAsync(equipment, cancellationToken).ConfigureAwait(false);
        await reservationStore.AddAsync(reservation, cancellationToken).ConfigureAwait(false);
        return reservation;
    }

    public async Task<Reservation> CancelAsync(Guid id, CancellationToken cancellationToken)
    {
        var reservation = await GetActiveAsync(id, cancellationToken).ConfigureAwait(false);
        var equipment = await GetEquipmentAsync(reservation, cancellationToken).ConfigureAwait(false);
        reservation.Cancel();
        equipment.ReleaseReservation();
        await equipmentStore.SaveAsync(equipment, cancellationToken).ConfigureAwait(false);
        return reservation;
    }

    private async Task<Reservation> GetActiveAsync(Guid id, CancellationToken cancellationToken)
    {
        var reservation = await reservationStore.GetAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new BorrowBoxException("reservation.not_found", "Reservation was not found.");
        if (reservation.Status != ReservationStatus.Active)
            throw new BorrowBoxException("reservation.not_active", "Reservation is no longer active.");
        return reservation;
    }

    private async Task<EquipmentItem> GetEquipmentAsync(Reservation reservation, CancellationToken cancellationToken) =>
        await equipmentStore.GetAsync(reservation.EquipmentId, cancellationToken).ConfigureAwait(false)
            ?? throw new BorrowBoxException("equipment.not_found", "Equipment was not found.");
}

public sealed class ReservationExpiryService(
    IReservationStore reservationStore,
    IEquipmentStore equipmentStore)
{
    public async Task<int> ExpireDueAsync(CancellationToken cancellationToken)
    {
        // Sprint 9 keeps time access simple. Sprint 14 will make this deterministic.
        var now = DateTimeOffset.Now;
        var reservations = await reservationStore.ListAsync(cancellationToken).ConfigureAwait(false);
        var expiredCount = 0;

        foreach (var reservation in reservations.Where(item =>
                     item.Status == ReservationStatus.Active && item.ExpiresAt <= now))
        {
            var equipment = await equipmentStore.GetAsync(reservation.EquipmentId, cancellationToken).ConfigureAwait(false);
            if (equipment is null) continue;
            reservation.Expire();
            equipment.ReleaseReservation();
            await equipmentStore.SaveAsync(equipment, cancellationToken).ConfigureAwait(false);
            expiredCount++;
        }

        return expiredCount;
    }
}
