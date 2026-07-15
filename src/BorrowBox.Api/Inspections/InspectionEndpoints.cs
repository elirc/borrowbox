using BorrowBox.Core.Inspections;
using BorrowBox.Api.Security;

namespace BorrowBox.Api.Inspections;

public sealed record ReportDamageRequest(Guid EquipmentId, string Damage);
public sealed record CompleteInspectionRequest(InspectionOutcome Outcome, string Notes);

public static class InspectionEndpoints
{
    public static IEndpointRouteBuilder MapInspectionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/inspections", async (ReportDamageRequest request, InspectionService service, CancellationToken cancellationToken) =>
        {
            var inspection = await service.ReportDamageAsync(request.EquipmentId, request.Damage, cancellationToken);
            return Results.Created($"/api/v1/inspections/{inspection.Id}", inspection);
        }).WithTags("Inspections").RequireAuthorization(BorrowBoxSecurity.StaffPolicy);
        endpoints.MapPost("/api/v1/inspections/{id:guid}/complete", async (Guid id, CompleteInspectionRequest request, InspectionService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.CompleteAsync(id, request.Outcome, request.Notes, cancellationToken)))
            .WithTags("Inspections").RequireAuthorization(BorrowBoxSecurity.StaffPolicy);
        return endpoints;
    }
}
