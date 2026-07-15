using BorrowBox.Core.Equipment;
using BorrowBox.Infrastructure.Data;
using BorrowBox.Infrastructure.Equipment;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BorrowBox.Infrastructure.Tests.Equipment;

public sealed class SqliteEquipmentStoreTests
{
    [Fact]
    public async Task Save_PersistsChangesAcrossContexts()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<BorrowBoxDbContext>().UseSqlite(connection).Options;

        await using (var setup = new BorrowBoxDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
            var store = new SqliteEquipmentStore(setup);
            var item = new EquipmentItem(Guid.NewGuid(), "cam-7", "Camera", "Media");
            await store.AddAsync(item, CancellationToken.None);
            item.UpdateDetails("Video camera", "Production");
            await store.SaveAsync(item, CancellationToken.None);
        }

        await using var verify = new BorrowBoxDbContext(options);
        var loaded = await new SqliteEquipmentStore(verify).ListAsync(CancellationToken.None);

        var itemFromDatabase = Assert.Single(loaded);
        Assert.Equal("CAM-7", itemFromDatabase.AssetTag);
        Assert.Equal("Video camera", itemFromDatabase.Name);
        Assert.Equal("Production", itemFromDatabase.Category);
    }
}
