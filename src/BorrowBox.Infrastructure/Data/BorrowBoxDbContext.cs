using Microsoft.EntityFrameworkCore;

namespace BorrowBox.Infrastructure.Data;

public sealed class BorrowBoxDbContext(DbContextOptions<BorrowBoxDbContext> options) : DbContext(options)
{
    public DbSet<EquipmentRow> Equipment => Set<EquipmentRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var equipment = modelBuilder.Entity<EquipmentRow>();
        equipment.ToTable("Equipment");
        equipment.HasKey(row => row.Id);
        equipment.HasIndex(row => row.AssetTag).IsUnique();
        equipment.Property(row => row.AssetTag).HasMaxLength(50).IsRequired();
        equipment.Property(row => row.Name).HasMaxLength(200).IsRequired();
        equipment.Property(row => row.Category).HasMaxLength(100).IsRequired();
        equipment.Property(row => row.Status).HasMaxLength(30).IsRequired();
    }
}

public sealed class EquipmentRow
{
    public Guid Id { get; set; }
    public required string AssetTag { get; set; }
    public required string Name { get; set; }
    public required string Category { get; set; }
    public required string Status { get; set; }
}
