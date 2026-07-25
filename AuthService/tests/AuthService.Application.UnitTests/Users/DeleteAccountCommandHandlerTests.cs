using AuthService.Application.UnitTests.TestData;
using AuthService.Application.Users.DeleteAccount;
using AuthService.Domain.RefreshTokens;
using AuthService.Domain.Users;
using Common.Application.Abstractions;
using Common.Domain.Results;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AuthService.Application.UnitTests.Users;

public sealed class DeleteAccountCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly DeleteAccountCommandHandler _handler;

    public DeleteAccountCommandHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(Now);
        _refreshTokenRepository
            .GetActiveByUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _handler = new DeleteAccountCommandHandler(
            _userRepository, _refreshTokenRepository, _unitOfWork, _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WithActiveAccount_MarksTheUserAsDeleted()
    {
        User user = GivenExistingUser(Factories.ActiveUser());

        Result result = await _handler.Handle(new DeleteAccountCommand(user.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.Status.Should().Be(UserStatus.Deleted);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithActiveAccount_RevokesEveryActiveSession()
    {
        User user = GivenExistingUser(Factories.ActiveUser());
        RefreshToken first = Factories.RefreshTokenFor(user.Id, Now);
        RefreshToken second = Factories.RefreshTokenFor(user.Id, Now);
        _refreshTokenRepository
            .GetActiveByUserAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns([first, second]);

        await _handler.Handle(new DeleteAccountCommand(user.Id), CancellationToken.None);

        first.RevokedAtUtc.Should().Be(Now);
        second.RevokedAtUtc.Should().Be(Now);
    }

    [Fact]
    public async Task Handle_WithUnknownUser_ReturnsNotFound()
    {
        Guid userId = Guid.CreateVersion7();
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        Result result = await _handler.Handle(new DeleteAccountCommand(userId), CancellationToken.None);

        result.Error.Should().Be(UserErrors.NotFound(userId));
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAlreadyDeletedAccount_ReturnsConflictWithoutPersisting()
    {
        User user = GivenExistingUser(Factories.DeletedUser());

        Result result = await _handler.Handle(new DeleteAccountCommand(user.Id), CancellationToken.None);

        result.Error.Should().Be(UserErrors.AlreadyDeleted);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private User GivenExistingUser(User user)
    {
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        return user;
    }
}
