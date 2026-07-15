using BorrowBox.Core.Loans;

namespace BorrowBox.Api.Loans;

public sealed record CheckOutRequest(Guid EquipmentId, Guid BorrowerId, DateOnly DueOn);
public sealed record LoanResponse(Guid Id, Guid EquipmentId, Guid BorrowerId, DateTimeOffset CheckedOutAt, DateOnly DueOn, bool IsActive);

public static class LoanEndpoints
{
    public static IEndpointRouteBuilder MapLoanEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/loans", async (CheckOutRequest request, CheckoutService service, CancellationToken cancellationToken) =>
        {
            var loan = await service.CheckOutAsync(request.EquipmentId, request.BorrowerId, request.DueOn, cancellationToken);
            return Results.Created($"/api/v1/loans/{loan.Id}", new LoanResponse(
                loan.Id, loan.EquipmentId, loan.BorrowerId, loan.CheckedOutAt, loan.DueOn, loan.IsActive));
        }).WithTags("Loans");
        return endpoints;
    }
}
