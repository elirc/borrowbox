using BorrowBox.Core.Common;

namespace BorrowBox.Core.Equipment;

public enum EquipmentStatus { Available, CheckedOut, Reserved, Inspection, Retired }

public sealed class EquipmentItem
{
    public EquipmentItem(Guid id, string assetTag, string name, string category)
    {
        if (id == Guid.Empty) throw new BorrowBoxException("equipment.id_required", "Equipment ID is required.");
        if (string.IsNullOrWhiteSpace(assetTag)) throw new BorrowBoxException("equipment.asset_tag_required", "Asset tag is required.");
        if (string.IsNullOrWhiteSpace(name)) throw new BorrowBoxException("equipment.name_required", "Name is required.");
        if (string.IsNullOrWhiteSpace(category)) throw new BorrowBoxException("equipment.category_required", "Category is required.");
        Id = id;
        AssetTag = assetTag.Trim().ToUpperInvariant();
        Name = name.Trim();
        Category = category.Trim();
    }

    public Guid Id { get; }
    public string AssetTag { get; }
    public string Name { get; private set; }
    public string Category { get; private set; }
    public EquipmentStatus Status { get; private set; } = EquipmentStatus.Available;
}

public interface IEquipmentStore
{
    Task AddAsync(EquipmentItem item, CancellationToken cancellationToken);
    Task<EquipmentItem?> GetAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class EquipmentService(IEquipmentStore store)
{
    public async Task<EquipmentItem> CreateAsync(string assetTag, string name, string category, CancellationToken cancellationToken)
    {
        var item = new EquipmentItem(Guid.NewGuid(), assetTag, name, category);
        await store.AddAsync(item, cancellationToken).ConfigureAwait(false);
        return item;
    }

    public Task<EquipmentItem?> GetAsync(Guid id, CancellationToken cancellationToken) => store.GetAsync(id, cancellationToken);
}
