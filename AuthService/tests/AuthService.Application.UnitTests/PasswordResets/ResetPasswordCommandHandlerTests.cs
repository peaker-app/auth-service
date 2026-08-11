using AuthService.Application.Abstractions;
using AuthService.Application.Authentication;
using AuthService.Application.PasswordResets.ResetPassword;
using AuthService.Application.UnitTests.TestData;
using AuthService.Domain.PasswordResets;
using AuthService.Domain.Users;
using Common.Application.Abstractions;
using Common.Domain.Results;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AuthService.Application.UnitTests.PasswordResets;

public sealed class ResetPasswordCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly ResetPasswordCommand Command = new("raw-token", "brand-new-password");

    private readonly IPasswordResetTokenRepository _tokenRepository =
        Substitute.For<IPasswordResetTokenRepository>();

    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordResetTokenGenerator _tokenGenerator = Substitute.For<IPasswordResetTokenGenerator>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IBreachedPasswordChecker _breachedPasswordChecker = Substitute.For<IBreachedPasswordChecker>();
    private readonly ISessionRevoker _sessionRevoker = Substitute.For<ISessionRevoker>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ResetPasswordCommandHandler _handler;

    public ResetPasswordCommandHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(Now);
        _tokenGenerator.Hash(Command.Token).Returns("reset-hash");
        _passwordHasher.Hash(Command.NewPassword).Returns("argon2id$new");
        _handler = new ResetPasswordCommandHandler(
            _tokenRepository,
            _userRepository,
            _tokenGenerator,
            _passwordHasher,
            _breachedPasswordChecker,
            _sessionRevoker,
            _unitOfWork,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WithAnActiveToken_ChangesThePassword()
    {
        User user = GivenTokenFor(Factories.ActiveUser());

        Result result = await _handler.Handle(Command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.PasswordHash.Should().Be("argon2id$new");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAnActiveToken_RevokesEverySession()
    {
        User user = GivenTokenFor(Factories.ActiveUser());

        await _handler.Handle(Command, CancellationToken.None);

        await _sessionRevoker.Received(1).RevokeAllAsync(user, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAnUnknownToken_ReturnsInvalidOrExpired()
    {
        _tokenRepository.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((PasswordResetToken?)null);

        Result result = await _handler.Handle(Command, CancellationToken.None);

        result.Error.Should().Be(PasswordResetErrors.InvalidOrExpired);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAConsumedToken_ReturnsInvalidOrExpired()
    {
        User user = Factories.ActiveUser();
        PasswordResetToken token = Factories.ResetTokenFor(user.Id, Now);
        token.Consume(Now);
        _tokenRepository.GetByTokenHashAsync("reset-hash", Arg.Any<CancellationToken>()).Returns(token);

        Result result = await _handler.Handle(Command, CancellationToken.None);

        result.Error.Should().Be(PasswordResetErrors.InvalidOrExpired);
    }

    [Fact]
    public async Task Handle_WithABreachedPassword_IsRejectedAndLeavesTheHashUntouched()
    {
        User user = GivenTokenFor(Factories.ActiveUser());
        _breachedPasswordChecker.IsBreachedAsync(Command.NewPassword, Arg.Any<CancellationToken>()).Returns(true);

        Result result = await _handler.Handle(Command, CancellationToken.None);

        result.Error.Should().Be(UserErrors.PasswordBreached);
        user.PasswordHash.Should().Be(Factories.DefaultHash);
    }

    [Fact]
    public async Task Handle_WhenTheAccountIsDeleted_ReturnsInvalidOrExpired()
    {
        GivenTokenFor(Factories.DeletedUser());

        Result result = await _handler.Handle(Command, CancellationToken.None);

        result.Error.Should().Be(PasswordResetErrors.InvalidOrExpired);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private User GivenTokenFor(User user)
    {
        _tokenRepository.GetByTokenHashAsync("reset-hash", Arg.Any<CancellationToken>())
            .Returns(Factories.ResetTokenFor(user.Id, Now));
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        return user;
    }
}
