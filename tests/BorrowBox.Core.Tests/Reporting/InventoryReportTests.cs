using BorrowBox.Core.Equipment;
using BorrowBox.Core.Reporting;

namespace BorrowBox.Core.Tests.Reporting;

public sealed class InventoryReportTests
{
    [Fact]
    public async Task ExportCsv_EscapesCommaAndQuote()
    {
        var items = new[]
        {
            new EquipmentItem(Guid.NewGuid(), "CAM-1", "Camera, compact", "Photo \"Lab\"")
        };
        var report = new InventoryReportService(new StubEquipmentStore(items));

        var csv = await report.ExportCsvAsync(CancellationToken.None);

        Assert.Contains("\"Camera, compact\"", csv);
        Assert.Contains("\"Photo \"\"Lab\"\"\"", csv);
    }

    [Fact]
    public async Task Summary_GroupsByStatusAndCategory()
    {
        var available = new EquipmentItem(Guid.NewGuid(), "CAM-1", "Camera", "Media");
        var checkedOut = new EquipmentItem(Guid.NewGuid(), "CAM-2", "Camera", "Media");
        checkedOut.CheckOut();
        var report = new InventoryReportService(new StubEquipmentStore([available, checkedOut]));

        var summary = await report.SummarizeAsync(CancellationToken.None);

        Assert.Equal(2, summary.Total);
        Assert.Equal(1, summary.ByStatus["Available"]);
        Assert.Equal(1, summary.ByStatus["CheckedOut"]);
        Assert.Equal(2, summary.ByCategory["Media"]);
    }

    private sealed class StubEquipmentStore(IReadOnlyCollection<EquipmentItem> items) : IEquipmentStore
    {
        public Task AddAsync(EquipmentItem item, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<EquipmentItem?> GetAsync(Guid id, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyCollection<EquipmentItem>> ListAsync(CancellationToken cancellationToken) => Task.FromResult(items);
        public Task SaveAsync(EquipmentItem item, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
