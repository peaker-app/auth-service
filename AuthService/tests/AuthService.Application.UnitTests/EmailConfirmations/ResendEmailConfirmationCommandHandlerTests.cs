using AuthService.Application.EmailConfirmations;
using AuthService.Application.EmailConfirmations.ResendEmailConfirmation;
using AuthService.Application.UnitTests.TestData;
using AuthService.Domain.EmailConfirmations;
using AuthService.Domain.Users;
using Common.Application.Abstractions;
using Common.Domain.Results;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AuthService.Application.UnitTests.EmailConfirmations;

public sealed class ResendEmailConfirmationCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();

    private readonly IEmailConfirmationTokenRepository _tokenRepository =
        Substitute.For<IEmailConfirmationTokenRepository>();

    private readonly IEmailConfirmationIssuer _issuer = Substitute.For<IEmailConfirmationIssuer>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ResendEmailConfirmationCommandHandler _handler;

    public ResendEmailConfirmationCommandHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(Now);
        _issuer.IssueAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()).Returns(Result.Success());
        _handler = new ResendEmailConfirmationCommandHandler(
            _userRepository, _tokenRepository, _issuer, _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WithoutPreviousToken_IssuesANewOne()
    {
        User user = GivenUser(Factories.ActiveUser());

        Result result = await _handler.Handle(new ResendEmailConfirmationCommand(user.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _issuer.Received(1).IssueAsync(user, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnceTheCooldownElapsed_IssuesANewToken()
    {
        User user = GivenUser(Factories.ActiveUser());
        GivenLatestToken(Factories.ConfirmationTokenFor(user.Id, Now - EmailConfirmationToken.ResendCooldown));

        Result result = await _handler.Handle(new ResendEmailConfirmationCommand(user.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithinTheCooldown_ReturnsResendTooSoon()
    {
        User user = GivenUser(Factories.ActiveUser());
        GivenLatestToken(Factories.ConfirmationTokenFor(user.Id, Now.AddSeconds(-1)));

        Result result = await _handler.Handle(new ResendEmailConfirmationCommand(user.Id), CancellationToken.None);

        result.Error.Should().Be(EmailConfirmationErrors.ResendTooSoon);
        await _issuer.DidNotReceive().IssueAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTheEmailIsAlreadyConfirmed_ReturnsEmailAlreadyConfirmed()
    {
        User user = GivenUser(Factories.ConfirmedUser());

        Result result = await _handler.Handle(new ResendEmailConfirmationCommand(user.Id), CancellationToken.None);

        result.Error.Should().Be(UserErrors.EmailAlreadyConfirmed);
    }

    [Fact]
    public async Task Handle_WhenTheAccountIsDeleted_ReturnsNotFound()
    {
        User user = GivenUser(Factories.DeletedUser());

        Result result = await _handler.Handle(new ResendEmailConfirmationCommand(user.Id), CancellationToken.None);

        result.Error.Should().Be(UserErrors.NotFound(user.Id));
    }

    [Fact]
    public async Task Handle_WithUnknownUser_ReturnsNotFound()
    {
        Guid userId = Guid.CreateVersion7();
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        Result result = await _handler.Handle(new ResendEmailConfirmationCommand(userId), CancellationToken.None);

        result.Error.Should().Be(UserErrors.NotFound(userId));
    }

    private User GivenUser(User user)
    {
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        return user;
    }

    private void GivenLatestToken(EmailConfirmationToken token) =>
        _tokenRepository.GetLatestByUserAsync(token.UserId, Arg.Any<CancellationToken>()).Returns(token);
}
