using AuthService.Application.Abstractions;
using AuthService.Application.Users.RegisterUser;
using AuthService.Application.UnitTests.TestData;
using AuthService.Domain.Users;
using AuthService.Domain.Users.Events;
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
    public async Task Handle_WithTheEmailOfADeletedAccount_NeverCreatesAnAccountNorAnnouncesIt()
    {
        GivenIdentifiersAreAvailable();
        _userRepository
            .ExistsByEmailAsync(
                Arg.Is<Email>(email => email != null && email.Value == Factories.PseudonymizedEmail),
                Arg.Any<CancellationToken>())
            .Returns(true);

        Result result = await _handler.Handle(Command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
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
    public async Task Handle_WithAnExistingEmail_SucceedsWithoutCreatingASecondAccount()
    {
        GivenIdentifiersAreAvailable();
        GivenTheEmailBelongsTo(Factories.ActiveUser());

        Result result = await _handler.Handle(Command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _userRepository.DidNotReceive().Add(Arg.Any<User>());
    }

    [Fact]
    public async Task Handle_WithAnExistingEmail_RaisesTheDuplicateAttemptEvent()
    {
        GivenIdentifiersAreAvailable();
        User existing = Factories.ActiveUser();
        existing.ClearDomainEvents();
        GivenTheEmailBelongsTo(existing);

        await _handler.Handle(Command, CancellationToken.None);

        existing.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<DuplicateRegistrationAttemptedDomainEvent>();
    }

    [Fact]
    public async Task Handle_WithAnExistingEmailOfADeletedAccount_StaysSilent()
    {
        GivenIdentifiersAreAvailable();
        User deleted = Factories.DeletedUser();
        deleted.ClearDomainEvents();
        GivenTheEmailBelongsTo(deleted);

        Result result = await _handler.Handle(Command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        deleted.DomainEvents.Should().BeEmpty();
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
    public async Task Handle_WhenAConcurrentRegistrationTookTheEmail_StillSucceeds()
    {
        GivenIdentifiersAreAvailable();
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<int>(_ => throw new DuplicateCredentialException(CredentialField.Email, new IOException()));

        Result result = await _handler.Handle(Command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
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

    private void GivenTheEmailBelongsTo(User user) =>
        _userRepository.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(user);

    private void GivenIdentifiersAreAvailable()
    {
        _userRepository.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        _userRepository.ExistsByUsernameAsync(Arg.Any<Username>(), Arg.Any<CancellationToken>()).Returns(false);
        _breachedPasswordChecker.IsBreachedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
    }
}
