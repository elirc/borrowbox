using System.Text;
using BorrowBox.Core.Equipment;

namespace BorrowBox.Core.Reporting;

public sealed record InventorySummary(
    int Total,
    IReadOnlyDictionary<string, int> ByStatus,
    IReadOnlyDictionary<string, int> ByCategory);

public sealed class InventoryReportService(IEquipmentStore equipmentStore)
{
    public async Task<InventorySummary> SummarizeAsync(CancellationToken cancellationToken)
    {
        var items = await equipmentStore.ListAsync(cancellationToken).ConfigureAwait(false);
        return new InventorySummary(
            items.Count,
            items.GroupBy(item => item.Status.ToString()).ToDictionary(group => group.Key, group => group.Count()),
            items.GroupBy(item => item.Category).ToDictionary(group => group.Key, group => group.Count()));
    }

    public async Task<string> ExportCsvAsync(CancellationToken cancellationToken)
    {
        var items = await equipmentStore.ListAsync(cancellationToken).ConfigureAwait(false);
        var csv = new StringBuilder("AssetTag,Name,Category,Status\n");
        foreach (var item in items.OrderBy(item => item.AssetTag))
            csv.AppendJoin(',', Escape(item.AssetTag), Escape(item.Name), Escape(item.Category), Escape(item.Status.ToString())).Append('\n');
        return csv.ToString();
    }

    private static string Escape(string value) =>
        value.IndexOfAny([',', '"', '\r', '\n']) >= 0 ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
}
