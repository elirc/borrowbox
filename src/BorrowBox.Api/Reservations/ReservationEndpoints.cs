using BorrowBox.Core.Reservations;

namespace BorrowBox.Api.Reservations;

public sealed record CreateReservationRequest(Guid EquipmentId, Guid BorrowerId, DateTimeOffset ExpiresAt);
public sealed record ReservationResponse(Guid Id, Guid EquipmentId, Guid BorrowerId, DateTimeOffset ReservedAt, DateTimeOffset ExpiresAt, string Status);

public static class ReservationEndpoints
{
    public static IEndpointRouteBuilder MapReservationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/reservations", async (CreateReservationRequest request, ReservationService service, CancellationToken cancellationToken) =>
        {
            var reservation = await service.ReserveAsync(request.EquipmentId, request.BorrowerId, request.ExpiresAt, cancellationToken);
            return Results.Created($"/api/v1/reservations/{reservation.Id}", ToResponse(reservation));
        }).WithTags("Reservations");
        endpoints.MapPost("/api/v1/reservations/{id:guid}/cancel", async (Guid id, ReservationService service, CancellationToken cancellationToken) =>
            Results.Ok(ToResponse(await service.CancelAsync(id, cancellationToken)))).WithTags("Reservations");
        return endpoints;
    }

    private static ReservationResponse ToResponse(Reservation reservation) => new(
        reservation.Id, reservation.EquipmentId, reservation.BorrowerId,
        reservation.ReservedAt, reservation.ExpiresAt, reservation.Status.ToString());
}
