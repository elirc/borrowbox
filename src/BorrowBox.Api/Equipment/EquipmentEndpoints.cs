using BorrowBox.Core.Equipment;

namespace BorrowBox.Api.Equipment;

public sealed record CreateEquipmentRequest(string AssetTag, string Name, string Category);
public sealed record EquipmentResponse(Guid Id, string AssetTag, string Name, string Category, string Status);

public static class EquipmentEndpoints
{
    public static IEndpointRouteBuilder MapEquipmentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/equipment").WithTags("Equipment");
        group.MapPost("/", async (CreateEquipmentRequest request, EquipmentService service, CancellationToken cancellationToken) =>
        {
            var item = await service.CreateAsync(request.AssetTag, request.Name, request.Category, cancellationToken);
            return Results.Created($"/api/v1/equipment/{item.Id}", Map(item));
        });
        group.MapGet("/{id:guid}", async (Guid id, EquipmentService service, CancellationToken cancellationToken) =>
        {
            var item = await service.GetAsync(id, cancellationToken);
            return item is null ? Results.NotFound() : Results.Ok(Map(item));
        });
        return endpoints;
    }

    internal static EquipmentResponse Map(EquipmentItem item) =>
        new(item.Id, item.AssetTag, item.Name, item.Category, item.Status.ToString());
}
