using BorrowBox.Core.Equipment;
using BorrowBox.Infrastructure.Equipment;

namespace BorrowBox.Infrastructure.Tests.Equipment;

public sealed class EquipmentSearchTests
{
    [Fact]
    public async Task Search_FiltersThenPaginatesInAssetTagOrder()
    {
        var store = new InMemoryEquipmentStore();
        var service = new EquipmentService(store);
        await service.CreateAsync("CAM-02", "Camera B", "Media", CancellationToken.None);
        await service.CreateAsync("CALC-01", "Calculator", "Math", CancellationToken.None);
        await service.CreateAsync("CAM-01", "Camera A", "Media", CancellationToken.None);

        var page = await service.SearchAsync("cam", "Media", 1, 1, CancellationToken.None);

        Assert.Equal(2, page.Total);
        Assert.Equal("CAM-01", Assert.Single(page.Items).AssetTag);
    }
}
