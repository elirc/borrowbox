using BorrowBox.Api.Security;
using BorrowBox.Core.Reporting;

namespace BorrowBox.Api.Reporting;

public static class ReportingEndpoints
{
    public static IEndpointRouteBuilder MapReportingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/reports/inventory")
            .WithTags("Reports")
            .RequireAuthorization(BorrowBoxSecurity.StaffPolicy);
        group.MapGet("/summary", async (InventoryReportService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.SummarizeAsync(cancellationToken)));
        group.MapGet("/export.csv", async (InventoryReportService service, CancellationToken cancellationToken) =>
            Results.Text(await service.ExportCsvAsync(cancellationToken), "text/csv"));
        return endpoints;
    }
}
