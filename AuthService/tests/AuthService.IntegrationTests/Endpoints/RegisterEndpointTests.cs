using System.Net;
using FluentAssertions;
using Xunit;

namespace AuthService.IntegrationTests.Endpoints;

[Collection(nameof(AuthServiceCollection))]
public sealed class RegisterEndpointTests(AuthServiceApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_WithNewEmail_ReturnsCreated()
    {
        using HttpResponseMessage response = await _client.RegisterAsync(ApiTestHelpers.UniqueEmail());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        string email = ApiTestHelpers.UniqueEmail();
        (await _client.RegisterAsync(email)).Dispose();

        using HttpResponseMessage response = await _client.RegisterAsync(email);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithShortPassword_ReturnsBadRequest()
    {
        using HttpResponseMessage response = await _client.RegisterAsync(ApiTestHelpers.UniqueEmail(), "short");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
