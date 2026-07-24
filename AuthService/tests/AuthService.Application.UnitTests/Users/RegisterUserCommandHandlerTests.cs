using AuthService.Application.Abstractions;
using AuthService.Application.Users.RegisterUser;
using AuthService.Domain.Users;
using Common.Application.Abstractions;
using Common.Domain.Results;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AuthService.Application.UnitTests.Users;

public sealed class RegisterUserCommandHandlerTests
{
    private static readonly RegisterUserCommand Command = new("Hiker@Peaker.io", "hiker", "correct-horse-battery");

    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IBreachedPasswordChecker _breachedPasswordChecker = Substitute.For<IBreachedPasswordChecker>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RegisterUserCommandHandler _handler;

    public RegisterUserCommandHandlerTests()
    {
        _passwordHasher.Hash(Arg.Any<string>()).Returns("hash");
        _handler = new RegisterUserCommandHandler(_userRepository, _passwordHasher, _breachedPasswordChecker, _unitOfWork);
    }

    [Fact]
    public async Task Handle_WithValidData_PersistsUserAndReturnsId()
    {
        GivenIdentifiersAreAvailable();
        _breachedPasswordChecker.IsBreachedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        Result<Guid> result = await _handler.Handle(Command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _userRepository.Received(1).Add(Arg.Any<User>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInvalidEmail_ReturnsValidationAndDoesNotPersist()
    {
        Result<Guid> result = await _handler.Handle(Command with { Email = "not-an-email" }, CancellationToken.None);

        result.Error.Should().Be(UserErrors.EmailInvalid);
        _userRepository.DidNotReceive().Add(Arg.Any<User>());
    }

    [Fact]
    public async Task Handle_WithInvalidUsername_ReturnsValidationAndDoesNotPersist()
    {
        Result<Guid> result = await _handler.Handle(Command with { Username = "_nope_" }, CancellationToken.None);

        result.Error.Should().Be(UserErrors.UsernameInvalid);
        _userRepository.DidNotReceive().Add(Arg.Any<User>());
    }

    [Fact]
    public async Task Handle_WithExistingEmail_ReturnsConflict()
    {
        GivenIdentifiersAreAvailable();
        _userRepository.ExistsByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(true);

        Result<Guid> result = await _handler.Handle(Command, CancellationToken.None);

        result.Error.Should().Be(UserErrors.EmailAlreadyRegistered);
    }

    [Fact]
    public async Task Handle_WithExistingUsername_ReturnsConflict()
    {
        GivenIdentifiersAreAvailable();
        _userRepository.ExistsByUsernameAsync(Arg.Any<Username>(), Arg.Any<CancellationToken>()).Returns(true);

        Result<Guid> result = await _handler.Handle(Command, CancellationToken.None);

        result.Error.Should().Be(UserErrors.UsernameAlreadyRegistered);
    }

    [Fact]
    public async Task Handle_WithBreachedPassword_ReturnsValidation()
    {
        GivenIdentifiersAreAvailable();
        _breachedPasswordChecker.IsBreachedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        Result<Guid> result = await _handler.Handle(Command, CancellationToken.None);

        result.Error.Should().Be(UserErrors.PasswordBreached);
    }

    private void GivenIdentifiersAreAvailable()
    {
        _userRepository.ExistsByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(false);
        _userRepository.ExistsByUsernameAsync(Arg.Any<Username>(), Arg.Any<CancellationToken>()).Returns(false);
    }
}
