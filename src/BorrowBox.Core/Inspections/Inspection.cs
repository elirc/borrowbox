using BorrowBox.Core.Common;
using BorrowBox.Core.Equipment;

namespace BorrowBox.Core.Inspections;

public enum InspectionOutcome { Passed, Repair, Retired }

public sealed class Inspection
{
    public Inspection(Guid id, Guid equipmentId, string reportedDamage, DateTimeOffset openedAt)
    {
        if (id == Guid.Empty || equipmentId == Guid.Empty) throw new BorrowBoxException("inspection.identity_required", "Inspection and equipment IDs are required.");
        if (string.IsNullOrWhiteSpace(reportedDamage)) throw new BorrowBoxException("inspection.damage_required", "A damage description is required.");
        Id = id;
        EquipmentId = equipmentId;
        ReportedDamage = reportedDamage.Trim();
        OpenedAt = openedAt.ToUniversalTime();
    }

    public Guid Id { get; }
    public Guid EquipmentId { get; }
    public string ReportedDamage { get; }
    public DateTimeOffset OpenedAt { get; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public InspectionOutcome? Outcome { get; private set; }
    public string? ResolutionNotes { get; private set; }
    public bool IsOpen => CompletedAt is null;

    public void Complete(InspectionOutcome outcome, string notes, DateTimeOffset completedAt)
    {
        if (!IsOpen) throw new BorrowBoxException("inspection.closed", "Inspection is already closed.");
        if (string.IsNullOrWhiteSpace(notes)) throw new BorrowBoxException("inspection.notes_required", "Resolution notes are required.");
        Outcome = outcome;
        ResolutionNotes = notes.Trim();
        CompletedAt = completedAt.ToUniversalTime();
    }
}

public interface IInspectionStore
{
    Task AddAsync(Inspection inspection, CancellationToken cancellationToken);
    Task<Inspection?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Inspection>> ListAsync(CancellationToken cancellationToken);
}

public sealed class InspectionService(IEquipmentStore equipmentStore, IInspectionStore inspectionStore)
{
    public async Task<Inspection> ReportDamageAsync(Guid equipmentId, string damage, CancellationToken cancellationToken)
    {
        var equipment = await equipmentStore.GetAsync(equipmentId, cancellationToken).ConfigureAwait(false)
            ?? throw new BorrowBoxException("equipment.not_found", "Equipment was not found.");
        equipment.MarkForInspection();
        var inspection = new Inspection(Guid.NewGuid(), equipment.Id, damage, DateTimeOffset.UtcNow);
        await equipmentStore.SaveAsync(equipment, cancellationToken).ConfigureAwait(false);
        await inspectionStore.AddAsync(inspection, cancellationToken).ConfigureAwait(false);
        return inspection;
    }

    public async Task<Inspection> CompleteAsync(Guid id, InspectionOutcome outcome, string notes, CancellationToken cancellationToken)
    {
        var inspection = await inspectionStore.GetAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new BorrowBoxException("inspection.not_found", "Inspection was not found.");
        var equipment = await equipmentStore.GetAsync(inspection.EquipmentId, cancellationToken).ConfigureAwait(false)
            ?? throw new BorrowBoxException("equipment.not_found", "Equipment was not found.");
        inspection.Complete(outcome, notes, DateTimeOffset.UtcNow);
        if (outcome == InspectionOutcome.Passed) equipment.PassInspection();
        if (outcome == InspectionOutcome.Retired) equipment.RetireAfterInspection();
        await equipmentStore.SaveAsync(equipment, cancellationToken).ConfigureAwait(false);
        return inspection;
    }
}
