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

    public void UpdateDetails(string name, string category)
    {
        if (Status == EquipmentStatus.Retired) throw new BorrowBoxException("equipment.retired", "Retired equipment cannot change.");
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(category))
            throw new BorrowBoxException("equipment.details_required", "Name and category are required.");
        Name = name.Trim();
        Category = category.Trim();
    }

    public void Retire()
    {
        if (Status != EquipmentStatus.Available) throw new BorrowBoxException("equipment.retire_unavailable", "Only available equipment can be retired.");
        Status = EquipmentStatus.Retired;
    }

    public void CheckOut()
    {
        if (Status != EquipmentStatus.Available) throw new BorrowBoxException("equipment.not_available", "Equipment is not available.");
        Status = EquipmentStatus.CheckedOut;
    }
}

public interface IEquipmentStore
{
    Task AddAsync(EquipmentItem item, CancellationToken cancellationToken);
    Task<EquipmentItem?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<EquipmentItem>> ListAsync(CancellationToken cancellationToken);
}

public sealed record EquipmentPage(IReadOnlyCollection<EquipmentItem> Items, int Page, int PageSize, int Total);

public sealed class EquipmentService(IEquipmentStore store)
{
    public async Task<EquipmentItem> CreateAsync(string assetTag, string name, string category, CancellationToken cancellationToken)
    {
        var item = new EquipmentItem(Guid.NewGuid(), assetTag, name, category);
        await store.AddAsync(item, cancellationToken).ConfigureAwait(false);
        return item;
    }

    public Task<EquipmentItem?> GetAsync(Guid id, CancellationToken cancellationToken) => store.GetAsync(id, cancellationToken);

    public async Task<EquipmentPage> SearchAsync(string? query, string? category, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (page < 1 || pageSize is < 1 or > 50) throw new BorrowBoxException("equipment.page_invalid", "Page must be positive and page size must be 1 to 50.");
        var items = await store.ListAsync(cancellationToken).ConfigureAwait(false);
        var filtered = items.Where(item =>
            (string.IsNullOrWhiteSpace(query) || item.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || item.AssetTag.Contains(query, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(category) || item.Category.Equals(category, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(item => item.AssetTag)
            .ToArray();
        return new EquipmentPage(filtered.Skip((page - 1) * pageSize).Take(pageSize).ToArray(), page, pageSize, filtered.Length);
    }

    public async Task<EquipmentItem?> UpdateAsync(Guid id, string name, string category, CancellationToken cancellationToken)
    {
        var item = await store.GetAsync(id, cancellationToken).ConfigureAwait(false);
        item?.UpdateDetails(name, category);
        return item;
    }

    public async Task<bool> RetireAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await store.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (item is null) return false;
        item.Retire();
        return true;
    }
}
