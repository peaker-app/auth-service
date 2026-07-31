using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace AuthService.IntegrationTests.Endpoints;

[Collection(nameof(AuthServiceCollection))]
public sealed class SwaggerDocumentTests(AuthServiceApiFactory factory)
{
    [Fact]
    public async Task Document_IsTitledAfterItsOwnService()
    {
        using JsonDocument document = await ReadDocumentAsync();

        document.RootElement.GetProperty("info").GetProperty("title").GetString()
            .Should().Be("auth-service");
    }

    [Fact]
    public async Task Document_DeclaresARelativeServerSoItWorksBehindTheGateway()
    {
        using JsonDocument document = await ReadDocumentAsync();

        document.RootElement.GetProperty("servers")[0].GetProperty("url").GetString()
            .Should().Be("/");
    }

    [Fact]
    public async Task Document_DeclaresTheBearerSecurityScheme()
    {
        using JsonDocument document = await ReadDocumentAsync();

        JsonElement bearer = document.RootElement
            .GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");

        bearer.GetProperty("type").GetString().Should().Be("http");
        bearer.GetProperty("scheme").GetString().Should().Be("bearer");
    }

    [Fact]
    public async Task AuthorizedOperation_RequiresTheBearerScheme()
    {
        using JsonDocument document = await ReadDocumentAsync();

        JsonElement logout = document.RootElement
            .GetProperty("paths").GetProperty("/api/auth/logout").GetProperty("post");

        logout.GetProperty("security")[0].TryGetProperty("Bearer", out _).Should().BeTrue();
    }

    [Fact]
    public async Task AnonymousOperation_DoesNotRequireTheBearerScheme()
    {
        using JsonDocument document = await ReadDocumentAsync();

        JsonElement login = document.RootElement
            .GetProperty("paths").GetProperty("/api/auth/login").GetProperty("post");

        login.TryGetProperty("security", out _).Should().BeFalse();
    }

    private async Task<JsonDocument> ReadDocumentAsync()
    {
        using HttpClient client = factory.CreateClient();

        return JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
    }
}
