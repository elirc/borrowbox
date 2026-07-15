using System.Collections.Concurrent;
using BorrowBox.Core.Reservations;

namespace BorrowBox.Infrastructure.Reservations;

public sealed class InMemoryReservationStore : IReservationStore
{
    private readonly ConcurrentDictionary<Guid, Reservation> _reservations = new();

    public Task AddAsync(Reservation reservation, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _reservations[reservation.Id] = reservation;
        return Task.CompletedTask;
    }

    public Task<Reservation?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _reservations.TryGetValue(id, out var reservation);
        return Task.FromResult(reservation);
    }

    public Task<IReadOnlyCollection<Reservation>> ListAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyCollection<Reservation>>(_reservations.Values.ToArray());
    }
}
