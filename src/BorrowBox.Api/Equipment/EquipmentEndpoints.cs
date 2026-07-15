using BorrowBox.Core.Equipment;

namespace BorrowBox.Api.Equipment;

public sealed record CreateEquipmentRequest(string AssetTag, string Name, string Category);
public sealed record UpdateEquipmentRequest(string Name, string Category);
public sealed record EquipmentResponse(Guid Id, string AssetTag, string Name, string Category, string Status);
public sealed record EquipmentPageResponse(IReadOnlyCollection<EquipmentResponse> Items, int Page, int PageSize, int Total);

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
        group.MapGet("/", async (string? query, string? category, int? page, int? pageSize, EquipmentService service, CancellationToken cancellationToken) =>
        {
            var result = await service.SearchAsync(query, category, page ?? 1, pageSize ?? 20, cancellationToken);
            return Results.Ok(new EquipmentPageResponse(result.Items.Select(Map).ToArray(), result.Page, result.PageSize, result.Total));
        });
        group.MapGet("/{id:guid}", async (Guid id, EquipmentService service, CancellationToken cancellationToken) =>
        {
            var item = await service.GetAsync(id, cancellationToken);
            return item is null ? Results.NotFound() : Results.Ok(Map(item));
        });
        group.MapPut("/{id:guid}", async (Guid id, UpdateEquipmentRequest request, EquipmentService service, CancellationToken cancellationToken) =>
        {
            var item = await service.UpdateAsync(id, request.Name, request.Category, cancellationToken);
            return item is null ? Results.NotFound() : Results.Ok(Map(item));
        });
        group.MapDelete("/{id:guid}", async (Guid id, EquipmentService service, CancellationToken cancellationToken) =>
            await service.RetireAsync(id, cancellationToken) ? Results.NoContent() : Results.NotFound());
        return endpoints;
    }

    internal static EquipmentResponse Map(EquipmentItem item) =>
        new(item.Id, item.AssetTag, item.Name, item.Category, item.Status.ToString());
}
