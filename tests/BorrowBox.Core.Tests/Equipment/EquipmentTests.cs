using BorrowBox.Core.Common;
using BorrowBox.Core.Equipment;

namespace BorrowBox.Core.Tests.Equipment;

public sealed class EquipmentTests
{
    [Fact]
    public void Create_NormalizesAssetTagAndStartsAvailable()
    {
        var item = new EquipmentItem(Guid.NewGuid(), " cam-01 ", "Camera", "Media");
        Assert.Equal("CAM-01", item.AssetTag);
        Assert.Equal(EquipmentStatus.Available, item.Status);
    }

    [Fact]
    public void Create_BlankName_ReturnsUsefulRuleCode()
    {
        var error = Assert.Throws<BorrowBoxException>(() => new EquipmentItem(Guid.NewGuid(), "CAM-01", " ", "Media"));
        Assert.Equal("equipment.name_required", error.Code);
    }

    [Fact]
    public void Retire_AvailableItem_PreventsLaterEdits()
    {
        var item = new EquipmentItem(Guid.NewGuid(), "CAM-01", "Camera", "Media");
        item.Retire();

        var error = Assert.Throws<BorrowBoxException>(() => item.UpdateDetails("New", "Media"));

        Assert.Equal(EquipmentStatus.Retired, item.Status);
        Assert.Equal("equipment.retired", error.Code);
    }
}
