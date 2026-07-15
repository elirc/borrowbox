using System.Net;
using System.Net.Http.Json;
using BorrowBox.Api.Equipment;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BorrowBox.Api.Tests.Equipment;

public sealed class EquipmentApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public EquipmentApiTests(WebApplicationFactory<Program> factory) =>
        _client = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing")).CreateClient();

    [Fact]
    public async Task CreateThenGet_ReturnsEquipment()
    {
        var create = await _client.PostAsJsonAsync("/api/v1/equipment", new CreateEquipmentRequest("CAM-01", "Camera", "Media"));
        var item = await create.Content.ReadFromJsonAsync<EquipmentResponse>();
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.NotNull(item);

        var get = await _client.GetAsync($"/api/v1/equipment/{item.Id}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
    }
}
