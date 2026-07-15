using BorrowBox.Core.Borrowers;

namespace BorrowBox.Api.Borrowers;

public sealed record RegisterBorrowerRequest(string CollegeId, string DisplayName);
public sealed record BorrowerResponse(Guid Id, string CollegeId, string DisplayName, bool IsActive);

public static class BorrowerEndpoints
{
    public static IEndpointRouteBuilder MapBorrowerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/borrowers").WithTags("Borrowers");
        group.MapPost("/", async (RegisterBorrowerRequest request, BorrowerService service, CancellationToken cancellationToken) =>
        {
            var borrower = await service.RegisterAsync(request.CollegeId, request.DisplayName, cancellationToken);
            return Results.Created($"/api/v1/borrowers/{borrower.Id}", Map(borrower));
        });
        group.MapGet("/{id:guid}", async (Guid id, BorrowerService service, CancellationToken cancellationToken) =>
        {
            var borrower = await service.GetAsync(id, cancellationToken);
            return borrower is null ? Results.NotFound() : Results.Ok(Map(borrower));
        });
        return endpoints;
    }

    private static BorrowerResponse Map(Borrower borrower) => new(borrower.Id, borrower.CollegeId, borrower.DisplayName, borrower.IsActive);
}
