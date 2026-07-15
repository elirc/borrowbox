using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BorrowBox.Api.Tests.Operations;

public sealed class HealthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public HealthTests(WebApplicationFactory<Program> factory) =>
        _client = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing")).CreateClient();

    [Fact]
    public async Task Ready_ExplainsNonDurableDefault_AndReturnsCorrelationId()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/ready");
        request.Headers.Add("X-Correlation-ID", "test-request-42");

        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<ReadinessResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("test-request-42", response.Headers.GetValues("X-Correlation-ID").Single());
        Assert.NotNull(body);
        Assert.False(body.Durable);
        Assert.Equal("in-memory", body.Persistence);
    }

    private sealed record ReadinessResponse(string Status, string Persistence, bool Durable, string Note);
}
