using AuthService.Application.Abstractions;
using AuthService.Application.Authentication;
using AuthService.Application.UnitTests.TestData;
using AuthService.Application.Users.DeleteAccount;
using AuthService.Domain.Users;
using Common.Application.Abstractions;
using Common.Domain.Results;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AuthService.Application.UnitTests.Users;

public sealed class DeleteAccountCommandHandlerTests
{
    private const string Password = "correct-horse-battery";

    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IEmailPseudonymizer _emailPseudonymizer = Substitute.For<IEmailPseudonymizer>();
    private readonly ISessionRevoker _sessionRevoker = Substitute.For<ISessionRevoker>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly DeleteAccountCommandHandler _handler;

    public DeleteAccountCommandHandlerTests()
    {
        _passwordHasher.Verify(Password, Arg.Any<string?>()).Returns(true);
        _emailPseudonymizer.Pseudonymize(Arg.Any<Email>()).Returns(Factories.Pseudonym());
        _handler = new DeleteAccountCommandHandler(
            _userRepository, _passwordHasher, _emailPseudonymizer, _sessionRevoker, _unitOfWork);
    }

    [Fact]
    public async Task Handle_WithTheCorrectPassword_MarksTheUserAsDeleted()
    {
        User user = GivenExistingUser(Factories.ActiveUser());

        Result result = await _handler.Handle(CommandFor(user), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.Status.Should().Be(UserStatus.Deleted);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithTheCorrectPassword_ReplacesTheEmailWithThePseudonym()
    {
        User user = GivenExistingUser(Factories.ActiveUser());

        await _handler.Handle(CommandFor(user), CancellationToken.None);

        user.Email.Value.Should().Be(Factories.PseudonymizedEmail);
    }

    [Fact]
    public async Task Handle_WithTheCorrectPassword_RevokesEverySession()
    {
        User user = GivenExistingUser(Factories.ActiveUser());

        await _handler.Handle(CommandFor(user), CancellationToken.None);

        await _sessionRevoker.Received(1).RevokeAllAsync(user, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithTheWrongPassword_ReturnsUnauthorizedWithoutDeleting()
    {
        User user = GivenExistingUser(Factories.ActiveUser());
        _passwordHasher.Verify("wrong", Arg.Any<string?>()).Returns(false);

        Result result = await _handler.Handle(
            new DeleteAccountCommand(user.Id, "wrong"), CancellationToken.None);

        result.Error.Should().Be(UserErrors.InvalidCredentials);
        user.Status.Should().Be(UserStatus.Active);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithUnknownUser_ReturnsNotFound()
    {
        Guid userId = Guid.CreateVersion7();
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        Result result = await _handler.Handle(
            new DeleteAccountCommand(userId, Password), CancellationToken.None);

        result.Error.Should().Be(UserErrors.NotFound(userId));
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAlreadyDeletedAccount_ReturnsConflictWithoutPersisting()
    {
        User user = GivenExistingUser(Factories.DeletedUser());

        Result result = await _handler.Handle(CommandFor(user), CancellationToken.None);

        result.Error.Should().Be(UserErrors.AlreadyDeleted);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static DeleteAccountCommand CommandFor(User user) => new(user.Id, Password);

    private User GivenExistingUser(User user)
    {
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        return user;
    }
}
