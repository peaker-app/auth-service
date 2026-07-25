using AuthService.Application.Abstractions;
using AuthService.Application.Authentication;
using AuthService.Application.UnitTests.TestData;
using AuthService.Application.Users.LoginUser;
using AuthService.Domain.Users;
using Common.Application.Abstractions;
using Common.Domain.Results;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AuthService.Application.UnitTests.Users;

public sealed class LoginUserCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly LoginUserCommand Command = new(Factories.DefaultEmail, "correct-horse-battery", "127.0.0.1");

    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IAuthTokenIssuer _tokenIssuer = Substitute.For<IAuthTokenIssuer>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly LoginUserCommandHandler _handler;

    public LoginUserCommandHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(Now);
        _handler = new LoginUserCommandHandler(_userRepository, _passwordHasher, _tokenIssuer, _unitOfWork, _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WithEmailIdentifier_ReturnsTokens()
    {
        GivenUserFoundByEmail();

        Result<AuthTokensResponse> result = await _handler.Handle(Command, CancellationToken.None);

        result.Value.AccessToken.Should().Be("access-token");
        await _userRepository.DidNotReceive().GetByUsernameAsync(Arg.Any<Username>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithUsernameIdentifier_ReturnsTokens()
    {
        User user = Factories.ActiveUser();
        _userRepository.GetByUsernameAsync(Arg.Any<Username>(), Arg.Any<CancellationToken>()).Returns(user);
        GivenPasswordMatches(user);
        GivenTokensAreIssuedFor(user);

        Result<AuthTokensResponse> result = await _handler.Handle(
            Command with { Identifier = Factories.DefaultUsername }, CancellationToken.None);

        result.Value.AccessToken.Should().Be("access-token");
        await _userRepository.DidNotReceive().GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithUsernameIdentifierIgnoringCase_ResolvesTheUser()
    {
        User user = Factories.ActiveUser();
        _userRepository.GetByUsernameAsync(Arg.Any<Username>(), Arg.Any<CancellationToken>()).Returns(user);
        GivenPasswordMatches(user);
        GivenTokensAreIssuedFor(user);

        Result<AuthTokensResponse> result = await _handler.Handle(
            Command with { Identifier = "HIKER" }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithMalformedIdentifier_ReturnsInvalidCredentials()
    {
        Result<AuthTokensResponse> result = await _handler.Handle(
            Command with { Identifier = "_not valid_" }, CancellationToken.None);

        result.Error.Should().Be(UserErrors.InvalidCredentials);
    }

    [Fact]
    public async Task Handle_WithUnknownIdentifier_ReturnsInvalidCredentialsWithoutPersisting()
    {
        _userRepository.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        Result<AuthTokensResponse> result = await _handler.Handle(Command, CancellationToken.None);

        result.Error.Should().Be(UserErrors.InvalidCredentials);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithWrongPassword_RecordsFailureAndReturnsInvalidCredentials()
    {
        User user = Factories.ActiveUser();
        _userRepository.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(Command.Password, user.PasswordHash).Returns(false);

        Result<AuthTokensResponse> result = await _handler.Handle(Command, CancellationToken.None);

        result.Error.Should().Be(UserErrors.InvalidCredentials);
        user.FailedLoginCount.Should().Be(1);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAccountLocked_ReturnsInvalidCredentialsWithoutVerifying()
    {
        _userRepository.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(Factories.LockedUser(Now));

        Result<AuthTokensResponse> result = await _handler.Handle(Command, CancellationToken.None);

        result.Error.Should().Be(UserErrors.InvalidCredentials);
        _passwordHasher.DidNotReceive().Verify(Arg.Any<string>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task Handle_WhenAccountDeleted_ReturnsInvalidCredentialsWithoutVerifying()
    {
        _userRepository.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(Factories.DeletedUser());

        Result<AuthTokensResponse> result = await _handler.Handle(Command, CancellationToken.None);

        result.Error.Should().Be(UserErrors.InvalidCredentials);
        _passwordHasher.DidNotReceive().Verify(Arg.Any<string>(), Arg.Any<string?>());
    }

    private void GivenUserFoundByEmail()
    {
        User user = Factories.ActiveUser();
        _userRepository.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);
        GivenPasswordMatches(user);
        GivenTokensAreIssuedFor(user);
    }

    private void GivenPasswordMatches(User user) =>
        _passwordHasher.Verify(Command.Password, user.PasswordHash).Returns(true);

    private void GivenTokensAreIssuedFor(User user) =>
        _tokenIssuer.Issue(Arg.Any<User>(), Arg.Any<DateTime>(), Arg.Any<string?>())
            .Returns(Factories.IssuedFor(Factories.RefreshTokenFor(user.Id, Now)));
}
