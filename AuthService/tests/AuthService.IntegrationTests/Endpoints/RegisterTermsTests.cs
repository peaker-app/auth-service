using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace AuthService.IntegrationTests.Endpoints;

[Collection(nameof(AuthServiceCollection))]
public sealed class RegisterTermsTests(AuthServiceApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_WithoutAcceptingTheTerms_ReturnsBadRequest()
    {
        using HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                email = ApiTestHelpers.UniqueEmail(),
                username = ApiTestHelpers.UniqueUsername(),
                password = ApiTestHelpers.DefaultPassword,
                acceptedTerms = false
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_AcceptingTheTerms_RecordsTheVersionAndTheTimestamp()
    {
        RegisteredUser user = await _client.RegisterUserAsync();
        Guid userId = await factory.FindUserIdByEmailAsync(user.Email);

        (string version, DateTime acceptedAt) = await factory.GetTermsAcceptanceAsync(userId);

        version.Should().NotBeNullOrWhiteSpace();
        version.Should().NotBe("unrecorded");
        acceptedAt.Should().BeAfter(DateTime.UtcNow.AddMinutes(-5));
    }

    [Fact]
    public async Task Register_WithoutTheTermsFieldInTheBody_ReturnsBadRequestAndCreatesNoAccount()
    {
        string email = ApiTestHelpers.UniqueEmail();

        using HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                email,
                username = ApiTestHelpers.UniqueUsername(),
                password = ApiTestHelpers.DefaultPassword
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await factory.CountUsersByEmailAsync(email)).Should().Be(0);
    }

    [Fact]
    public async Task Register_WithoutAcceptingTheTerms_CreatesNoAccount()
    {
        string email = ApiTestHelpers.UniqueEmail();

        using HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                email,
                username = ApiTestHelpers.UniqueUsername(),
                password = ApiTestHelpers.DefaultPassword,
                acceptedTerms = false
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await factory.CountUsersByEmailAsync(email)).Should().Be(0);
    }
}
