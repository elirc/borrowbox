using BorrowBox.Core.Equipment;
using BorrowBox.Core.Inspections;
using BorrowBox.Infrastructure.Equipment;
using BorrowBox.Infrastructure.Inspections;

namespace BorrowBox.Infrastructure.Tests.Inspections;

public sealed class InspectionTests
{
    [Theory]
    [InlineData(InspectionOutcome.Passed, EquipmentStatus.Available)]
    [InlineData(InspectionOutcome.Repair, EquipmentStatus.Inspection)]
    [InlineData(InspectionOutcome.Retired, EquipmentStatus.Retired)]
    public async Task Complete_AppliesOutcomeToEquipment(InspectionOutcome outcome, EquipmentStatus expectedStatus)
    {
        var equipmentStore = new InMemoryEquipmentStore();
        var inspectionStore = new InMemoryInspectionStore();
        var equipment = new EquipmentItem(Guid.NewGuid(), "TAB-2", "Tablet", "Computing");
        await equipmentStore.AddAsync(equipment, CancellationToken.None);
        var service = new InspectionService(equipmentStore, inspectionStore);

        var inspection = await service.ReportDamageAsync(equipment.Id, "Cracked case", CancellationToken.None);
        await service.CompleteAsync(inspection.Id, outcome, "Technician reviewed it", CancellationToken.None);

        Assert.Equal(outcome, inspection.Outcome);
        Assert.Equal(expectedStatus, equipment.Status);
    }
}
