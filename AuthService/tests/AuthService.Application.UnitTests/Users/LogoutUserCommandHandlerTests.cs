using AuthService.Application.Abstractions;
using AuthService.Application.UnitTests.TestData;
using AuthService.Application.Users.LogoutUser;
using AuthService.Domain.RefreshTokens;
using Common.Application.Abstractions;
using Common.Domain.Results;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AuthService.Application.UnitTests.Users;

public sealed class LogoutUserCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IRefreshTokenGenerator _refreshTokenGenerator = Substitute.For<IRefreshTokenGenerator>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly LogoutUserCommandHandler _handler;

    public LogoutUserCommandHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(Now);
        _refreshTokenGenerator.Hash(Arg.Any<string>()).Returns("token-hash");
        _handler = new LogoutUserCommandHandler(
            _refreshTokenRepository, _refreshTokenGenerator, _unitOfWork, _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WithOwnedActiveToken_RevokesToken()
    {
        Guid userId = Guid.CreateVersion7();
        RefreshToken token = Factories.RefreshTokenFor(userId, Now);
        _refreshTokenRepository.GetByTokenHashAsync("token-hash", Arg.Any<CancellationToken>()).Returns(token);

        Result result = await _handler.Handle(new LogoutUserCommand("raw", userId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        token.RevokedAtUtc.Should().Be(Now);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithTokenOwnedByAnotherUser_SucceedsWithoutRevoking()
    {
        RefreshToken token = Factories.RefreshTokenFor(Guid.CreateVersion7(), Now);
        _refreshTokenRepository.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(token);

        Result result = await _handler.Handle(new LogoutUserCommand("raw", Guid.CreateVersion7()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        token.RevokedAtUtc.Should().BeNull();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithUnknownToken_Succeeds()
    {
        _refreshTokenRepository.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((RefreshToken?)null);

        Result result = await _handler.Handle(new LogoutUserCommand("raw", Guid.CreateVersion7()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}
