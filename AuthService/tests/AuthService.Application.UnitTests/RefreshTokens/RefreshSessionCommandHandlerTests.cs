using AuthService.Application.Abstractions;
using AuthService.Application.Authentication;
using AuthService.Application.RefreshTokens.RefreshSession;
using AuthService.Application.UnitTests.TestData;
using AuthService.Domain.RefreshTokens;
using AuthService.Domain.Users;
using Common.Application.Abstractions;
using Common.Domain.Results;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AuthService.Application.UnitTests.RefreshTokens;

public sealed class RefreshSessionCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly RefreshSessionCommand Command = new("raw-refresh-token", "127.0.0.1");

    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenGenerator _refreshTokenGenerator = Substitute.For<IRefreshTokenGenerator>();
    private readonly IAuthTokenIssuer _tokenIssuer = Substitute.For<IAuthTokenIssuer>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly RefreshSessionCommandHandler _handler;

    public RefreshSessionCommandHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(Now);
        _refreshTokenGenerator.Hash(Arg.Any<string>()).Returns("token-hash");
        _handler = new RefreshSessionCommandHandler(
            _refreshTokenRepository, _userRepository, _refreshTokenGenerator, _tokenIssuer, _unitOfWork, _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WithActiveToken_RotatesAndRevokesPreviousToken()
    {
        User user = Factories.ActiveUser();
        RefreshToken existing = Factories.RefreshTokenFor(user.Id, Now);
        RefreshToken replacement = Factories.RefreshTokenFor(user.Id, Now);
        _refreshTokenRepository.GetByTokenHashAsync("token-hash", Arg.Any<CancellationToken>()).Returns(existing);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _tokenIssuer.Issue(Arg.Any<User>(), Arg.Any<DateTime>(), Arg.Any<string?>()).Returns(Factories.IssuedFor(replacement));

        Result<AuthTokensResponse> result = await _handler.Handle(Command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existing.RevokedAtUtc.Should().Be(Now);
        existing.ReplacedById.Should().Be(replacement.Id);
    }

    [Fact]
    public async Task Handle_WithUnknownToken_ReturnsInvalidOrExpired()
    {
        _refreshTokenRepository.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((RefreshToken?)null);

        Result<AuthTokensResponse> result = await _handler.Handle(Command, CancellationToken.None);

        result.Error.Should().Be(RefreshTokenErrors.InvalidOrExpired);
    }

    [Fact]
    public async Task Handle_WithReusedRevokedToken_RevokesAllActiveSessions()
    {
        Guid userId = Guid.CreateVersion7();
        RefreshToken reused = Factories.RefreshTokenFor(userId, Now);
        reused.Revoke(Now);
        RefreshToken firstActive = Factories.RefreshTokenFor(userId, Now);
        RefreshToken secondActive = Factories.RefreshTokenFor(userId, Now);
        _refreshTokenRepository.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(reused);
        _refreshTokenRepository.GetActiveByUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns([firstActive, secondActive]);

        Result<AuthTokensResponse> result = await _handler.Handle(Command, CancellationToken.None);

        result.Error.Should().Be(RefreshTokenErrors.InvalidOrExpired);
        firstActive.RevokedAtUtc.Should().Be(Now);
        secondActive.RevokedAtUtc.Should().Be(Now);
    }

    [Fact]
    public async Task Handle_WithNaturallyExpiredToken_DoesNotRevokeTheOtherSessions()
    {
        Guid userId = Guid.CreateVersion7();
        RefreshToken expired = Factories.RefreshTokenFor(userId, Now.AddDays(-8));
        RefreshToken activeOnAnotherDevice = Factories.RefreshTokenFor(userId, Now);
        _refreshTokenRepository.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(expired);
        _refreshTokenRepository.GetActiveByUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns([activeOnAnotherDevice]);

        Result<AuthTokensResponse> result = await _handler.Handle(Command, CancellationToken.None);

        result.Error.Should().Be(RefreshTokenErrors.InvalidOrExpired);
        activeOnAnotherDevice.RevokedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenAccountDeleted_ReturnsInvalidOrExpiredWithoutIssuingTokens()
    {
        User user = Factories.DeletedUser();
        RefreshToken existing = Factories.RefreshTokenFor(user.Id, Now);
        _refreshTokenRepository.GetByTokenHashAsync("token-hash", Arg.Any<CancellationToken>()).Returns(existing);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        Result<AuthTokensResponse> result = await _handler.Handle(Command, CancellationToken.None);

        result.Error.Should().Be(RefreshTokenErrors.InvalidOrExpired);
        _tokenIssuer.DidNotReceive().Issue(Arg.Any<User>(), Arg.Any<DateTime>(), Arg.Any<string?>());
    }
}
