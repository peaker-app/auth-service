using AuthService.Application.Abstractions;
using AuthService.Application.Users.RegisterUser;
using AuthService.Application.UnitTests.TestData;
using AuthService.Domain.Users;
using Common.Application.Abstractions;
using Common.Domain.Results;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AuthService.Application.UnitTests.Users;

public sealed class RegisterUserCommandHandlerTests
{
    private static readonly RegisterUserCommand Command =
        new("Hiker@Peaker.io", "hiker", "correct-horse-battery", true);

    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IBreachedPasswordChecker _breachedPasswordChecker = Substitute.For<IBreachedPasswordChecker>();
    private readonly IEmailPseudonymizer _emailPseudonymizer = Substitute.For<IEmailPseudonymizer>();
    private readonly ITermsPolicy _termsPolicy = Substitute.For<ITermsPolicy>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RegisterUserCommandHandler _handler;

    public RegisterUserCommandHandlerTests()
    {
        _passwordHasher.Hash(Arg.Any<string>()).Returns("hash");
        _emailPseudonymizer.Pseudonymize(Arg.Any<Email>()).Returns(Factories.Pseudonym());
        _termsPolicy.CurrentVersion.Returns(Factories.TermsVersion);
        _dateTimeProvider.UtcNow.Returns(Factories.TermsAcceptedAt);
        _handler = new RegisterUserCommandHandler(
            _userRepository,
            _passwordHasher,
            _breachedPasswordChecker,
            _emailPseudonymizer,
            _termsPolicy,
            _dateTimeProvider,
            _unitOfWork);
    }

    [Fact]
    public async Task Handle_WithTheEmailOfADeletedAccount_ReturnsConflictWithoutCreatingAnAccount()
    {
        GivenIdentifiersAreAvailable();
        _userRepository
            .ExistsByEmailAsync(
                Arg.Is<Email>(email => email != null && email.Value == Factories.PseudonymizedEmail),
                Arg.Any<CancellationToken>())
            .Returns(true);

        Result result = await _handler.Handle(Command, CancellationToken.None);

        result.Error.Should().Be(UserErrors.EmailAlreadyRegistered);
        _userRepository.DidNotReceive().Add(Arg.Any<User>());
    }

    [Fact]
    public async Task Handle_WithValidData_PersistsTheUser()
    {
        GivenIdentifiersAreAvailable();

        Result result = await _handler.Handle(Command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _userRepository.Received(1).Add(Arg.Any<User>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInvalidEmail_ReturnsValidationAndDoesNotPersist()
    {
        Result result = await _handler.Handle(Command with { Email = "not-an-email" }, CancellationToken.None);

        result.Error.Should().Be(UserErrors.EmailInvalid);
        _userRepository.DidNotReceive().Add(Arg.Any<User>());
    }

    [Fact]
    public async Task Handle_WithInvalidUsername_ReturnsValidationAndDoesNotPersist()
    {
        Result result = await _handler.Handle(Command with { Username = "_nope_" }, CancellationToken.None);

        result.Error.Should().Be(UserErrors.UsernameInvalid);
        _userRepository.DidNotReceive().Add(Arg.Any<User>());
    }

    [Fact]
    public async Task Handle_WithAnExistingEmail_ReturnsConflictWithoutCreatingASecondAccount()
    {
        GivenIdentifiersAreAvailable();
        GivenTheEmailIsTaken();

        Result result = await _handler.Handle(Command, CancellationToken.None);

        result.Error.Should().Be(UserErrors.EmailAlreadyRegistered);
        _userRepository.DidNotReceive().Add(Arg.Any<User>());
    }

    [Fact]
    public async Task Handle_WithExistingUsername_ReturnsConflict()
    {
        GivenIdentifiersAreAvailable();
        _userRepository.ExistsByUsernameAsync(Arg.Any<Username>(), Arg.Any<CancellationToken>()).Returns(true);

        Result result = await _handler.Handle(Command, CancellationToken.None);

        result.Error.Should().Be(UserErrors.UsernameAlreadyRegistered);
    }

    [Fact]
    public async Task Handle_WithBreachedPassword_ReturnsValidation()
    {
        GivenIdentifiersAreAvailable();
        _breachedPasswordChecker.IsBreachedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        Result result = await _handler.Handle(Command, CancellationToken.None);

        result.Error.Should().Be(UserErrors.PasswordBreached);
    }

    [Fact]
    public async Task Handle_WhenAConcurrentRegistrationTookTheEmail_ReturnsConflict()
    {
        GivenIdentifiersAreAvailable();
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<int>(_ => throw new DuplicateCredentialException(CredentialField.Email, new IOException()));

        Result result = await _handler.Handle(Command, CancellationToken.None);

        result.Error.Should().Be(UserErrors.EmailAlreadyRegistered);
    }

    [Fact]
    public async Task Handle_WhenAConcurrentRegistrationTookTheUsername_ReturnsConflict()
    {
        GivenIdentifiersAreAvailable();
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<int>(_ => throw new DuplicateCredentialException(CredentialField.Username, new IOException()));

        Result result = await _handler.Handle(Command, CancellationToken.None);

        result.Error.Should().Be(UserErrors.UsernameAlreadyRegistered);
    }

    private void GivenTheEmailIsTaken() =>
        _userRepository.ExistsByEmailAsync(Email.Create(Factories.DefaultEmail).Value, Arg.Any<CancellationToken>())
            .Returns(true);

    private void GivenIdentifiersAreAvailable()
    {
        _userRepository.ExistsByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(false);
        _userRepository.ExistsByUsernameAsync(Arg.Any<Username>(), Arg.Any<CancellationToken>()).Returns(false);
        _breachedPasswordChecker.IsBreachedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
    }
}
