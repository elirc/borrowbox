using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BorrowBox.Api.Tests.Security;

public sealed class AuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public AuthorizationTests(WebApplicationFactory<Program> factory) =>
        _factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));

    [Fact]
    public async Task Retire_WithoutIdentity_ReturnsUnauthorized()
    {
        var response = await _factory.CreateClient().DeleteAsync($"/api/v1/equipment/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Retire_StudentRole_ReturnsForbidden()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/equipment/{Guid.NewGuid()}");
        request.Headers.Add("X-Demo-Role", "Student");
        var response = await _factory.CreateClient().SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
