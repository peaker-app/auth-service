using System.Net;
using AuthService.Domain.Users;
using FluentAssertions;
using Xunit;

namespace AuthService.IntegrationTests.Endpoints;

[Collection(nameof(AuthServiceCollection))]
public sealed class AdminUsersEndpointTests(AuthServiceApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Unlock_WithoutToken_ReturnsUnauthorized()
    {
        using HttpResponseMessage response = await _client.UnlockUserAsync(null, Guid.CreateVersion7());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Unlock_WithARegularAccount_ReturnsForbidden()
    {
        TokenPair tokens = await RegisterAndSignInAsync();

        using HttpResponseMessage response = await _client.UnlockUserAsync(tokens.AccessToken, Guid.CreateVersion7());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Lock_WithAnAdminAccount_SuspendsTheAccountAndBlocksSignIn()
    {
        string adminToken = await SignInAsAdminAsync();
        RegisteredUser victim = await _client.RegisterUserAsync();
        Guid victimId = await factory.FindUserIdByEmailAsync(victim.Email);

        using HttpResponseMessage response = await _client.LockUserAsync(adminToken, victimId);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await factory.GetStatusAsync(victimId)).Should().Be(nameof(UserStatus.Locked));

        using HttpResponseMessage login = await _client.LoginAsync(victim.Email, ApiTestHelpers.DefaultPassword);
        login.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Lock_WithARegularAccount_ReturnsForbidden()
    {
        TokenPair tokens = await RegisterAndSignInAsync();
        RegisteredUser victim = await _client.RegisterUserAsync();
        Guid victimId = await factory.FindUserIdByEmailAsync(victim.Email);

        using HttpResponseMessage response = await _client.LockUserAsync(tokens.AccessToken, victimId);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await factory.GetStatusAsync(victimId)).Should().Be(nameof(UserStatus.Active));
    }

    [Fact]
    public async Task Lock_WhenAnAdminTargetsThemselves_ReturnsBadRequest()
    {
        RegisteredUser admin = await _client.RegisterUserAsync();
        Guid adminId = await factory.FindUserIdByEmailAsync(admin.Email);
        await factory.GrantAdminAsync(adminId);
        TokenPair tokens = await _client.LoginWithTokensAsync(admin.Email);

        using HttpResponseMessage response = await _client.LockUserAsync(tokens.AccessToken, adminId);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Unlock_WithAnAdminAccount_RestoresTheLockedAccount()
    {
        string adminToken = await SignInAsAdminAsync();
        RegisteredUser victim = await _client.RegisterUserAsync();
        Guid victimId = await factory.FindUserIdByEmailAsync(victim.Email);
        using (await _client.LockUserAsync(adminToken, victimId))
        {
        }

        using HttpResponseMessage response = await _client.UnlockUserAsync(adminToken, victimId);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await factory.GetStatusAsync(victimId)).Should().Be(nameof(UserStatus.Active));
    }

    [Fact]
    public async Task Unlock_OnAnAccountThatIsNotLocked_ReturnsConflict()
    {
        string adminToken = await SignInAsAdminAsync();
        RegisteredUser other = await _client.RegisterUserAsync();
        Guid otherId = await factory.FindUserIdByEmailAsync(other.Email);

        using HttpResponseMessage response = await _client.UnlockUserAsync(adminToken, otherId);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Unlock_OnAnUnknownAccount_ReturnsNotFound()
    {
        string adminToken = await SignInAsAdminAsync();

        using HttpResponseMessage response = await _client.UnlockUserAsync(adminToken, Guid.CreateVersion7());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GrantRole_WithAnAdminAccount_PromotesTheTarget()
    {
        string adminToken = await SignInAsAdminAsync();
        RegisteredUser target = await _client.RegisterUserAsync();
        Guid targetId = await factory.FindUserIdByEmailAsync(target.Email);

        using HttpResponseMessage response = await _client.GrantRoleAsync(adminToken, targetId);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await factory.IsInRoleAsync(targetId, UserRole.Admin)).Should().BeTrue();
    }

    [Fact]
    public async Task GrantRole_WithARegularAccount_ReturnsForbiddenAndDoesNotPromote()
    {
        TokenPair tokens = await RegisterAndSignInAsync();
        RegisteredUser target = await _client.RegisterUserAsync();
        Guid targetId = await factory.FindUserIdByEmailAsync(target.Email);

        using HttpResponseMessage response = await _client.GrantRoleAsync(tokens.AccessToken, targetId);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await factory.IsInRoleAsync(targetId, UserRole.Admin)).Should().BeFalse();
    }

    [Fact]
    public async Task GrantRole_WhenTheTargetAlreadyHasTheRole_ReturnsConflict()
    {
        string adminToken = await SignInAsAdminAsync();
        RegisteredUser target = await _client.RegisterUserAsync();
        Guid targetId = await factory.FindUserIdByEmailAsync(target.Email);
        await factory.GrantAdminAsync(targetId);

        using HttpResponseMessage response = await _client.GrantRoleAsync(adminToken, targetId);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GrantRole_WithAnUnknownRole_ReturnsBadRequest()
    {
        string adminToken = await SignInAsAdminAsync();
        RegisteredUser target = await _client.RegisterUserAsync();
        Guid targetId = await factory.FindUserIdByEmailAsync(target.Email);

        using HttpResponseMessage response = await _client.GrantRoleAsync(adminToken, targetId, "Emperor");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GrantRole_WithoutARoleInTheBody_ReturnsBadRequestAndDoesNotPromote()
    {
        string adminToken = await SignInAsAdminAsync();
        RegisteredUser target = await _client.RegisterUserAsync();
        Guid targetId = await factory.FindUserIdByEmailAsync(target.Email);

        using HttpResponseMessage response = await _client.GrantRoleWithEmptyBodyAsync(adminToken, targetId);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await factory.IsInRoleAsync(targetId, UserRole.Admin)).Should().BeFalse();
    }

    [Fact]
    public async Task RevokeRole_WithAnAdminAccount_DemotesAnotherAdmin()
    {
        string adminToken = await SignInAsAdminAsync();
        RegisteredUser target = await _client.RegisterUserAsync();
        Guid targetId = await factory.FindUserIdByEmailAsync(target.Email);
        await factory.GrantAdminAsync(targetId);

        using HttpResponseMessage response = await _client.RevokeRoleAsync(adminToken, targetId);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await factory.IsInRoleAsync(targetId, UserRole.Admin)).Should().BeFalse();
    }

    [Fact]
    public async Task RevokeRole_WhenAnAdminTargetsThemselves_ReturnsConflict()
    {
        RegisteredUser admin = await _client.RegisterUserAsync();
        Guid adminId = await factory.FindUserIdByEmailAsync(admin.Email);
        await factory.GrantAdminAsync(adminId);
        TokenPair tokens = await _client.LoginWithTokensAsync(admin.Email);

        using HttpResponseMessage response = await _client.RevokeRoleAsync(tokens.AccessToken, adminId);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await factory.IsInRoleAsync(adminId, UserRole.Admin)).Should().BeTrue();
    }

    [Fact]
    public async Task RevokeRole_WhenTheTargetDoesNotHaveTheRole_ReturnsConflict()
    {
        string adminToken = await SignInAsAdminAsync();
        RegisteredUser target = await _client.RegisterUserAsync();
        Guid targetId = await factory.FindUserIdByEmailAsync(target.Email);

        using HttpResponseMessage response = await _client.RevokeRoleAsync(adminToken, targetId);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private async Task<TokenPair> RegisterAndSignInAsync()
    {
        RegisteredUser user = await _client.RegisterUserAsync();

        return await _client.LoginWithTokensAsync(user.Email);
    }

    private async Task<string> SignInAsAdminAsync()
    {
        RegisteredUser admin = await _client.RegisterUserAsync();
        await factory.GrantAdminAsync(await factory.FindUserIdByEmailAsync(admin.Email));
        TokenPair tokens = await _client.LoginWithTokensAsync(admin.Email);

        return tokens.AccessToken;
    }
}
