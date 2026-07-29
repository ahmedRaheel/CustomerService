using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CustomerService.IntegrationTests.Health;
public sealed class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public HealthEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;
    [Fact]
    public async Task Health_Should_Return_Success()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health").ConfigureAwait(false);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}