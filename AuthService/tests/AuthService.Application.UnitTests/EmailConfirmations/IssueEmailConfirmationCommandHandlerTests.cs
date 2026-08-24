using AuthService.Application.EmailConfirmations;
using AuthService.Application.EmailConfirmations.IssueEmailConfirmation;
using AuthService.Application.UnitTests.TestData;
using AuthService.Domain.Users;
using Common.Domain.Results;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AuthService.Application.UnitTests.EmailConfirmations;

public sealed class IssueEmailConfirmationCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IEmailConfirmationIssuer _issuer = Substitute.For<IEmailConfirmationIssuer>();
    private readonly IssueEmailConfirmationCommandHandler _handler;

    public IssueEmailConfirmationCommandHandlerTests()
    {
        _issuer.IssueAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()).Returns(Result.Success());
        _handler = new IssueEmailConfirmationCommandHandler(_userRepository, _issuer);
    }

    [Fact]
    public async Task Handle_ForAnUnconfirmedAccount_IssuesTheConfirmation()
    {
        User user = GivenUser(Factories.ActiveUser());

        Result result = await _handler.Handle(new IssueEmailConfirmationCommand(user.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _issuer.Received(1).IssueAsync(user, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ForAnAlreadyConfirmedAccount_DoesNothing()
    {
        User user = GivenUser(Factories.ConfirmedUser());

        Result result = await _handler.Handle(new IssueEmailConfirmationCommand(user.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _issuer.DidNotReceive().IssueAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ForADeletedAccount_DoesNothing()
    {
        User user = GivenUser(Factories.DeletedUser());

        Result result = await _handler.Handle(new IssueEmailConfirmationCommand(user.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _issuer.DidNotReceive().IssueAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithUnknownUser_ReturnsNotFound()
    {
        Guid userId = Guid.CreateVersion7();
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        Result result = await _handler.Handle(new IssueEmailConfirmationCommand(userId), CancellationToken.None);

        result.Error.Should().Be(UserErrors.NotFound(userId));
    }

    private User GivenUser(User user)
    {
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        return user;
    }
}
