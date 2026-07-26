using AuthService.Application.Abstractions;
using AuthService.Application.EmailConfirmations.ConfirmEmail;
using AuthService.Application.UnitTests.TestData;
using AuthService.Domain.EmailConfirmations;
using AuthService.Domain.Users;
using Common.Application.Abstractions;
using Common.Domain.Results;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AuthService.Application.UnitTests.EmailConfirmations;

public sealed class ConfirmEmailCommandHandlerTests
{
    private const string TokenHash = "confirmation-hash";

    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly IEmailConfirmationTokenRepository _tokenRepository =
        Substitute.For<IEmailConfirmationTokenRepository>();

    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();

    private readonly IEmailConfirmationTokenGenerator _tokenGenerator =
        Substitute.For<IEmailConfirmationTokenGenerator>();

    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ConfirmEmailCommandHandler _handler;

    public ConfirmEmailCommandHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(Now);
        _tokenGenerator.Hash(Arg.Any<string>()).Returns(TokenHash);
        _handler = new ConfirmEmailCommandHandler(
            _tokenRepository, _userRepository, _tokenGenerator, _unitOfWork, _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WithActiveToken_ConfirmsTheAccount()
    {
        User user = Factories.ActiveUser();
        GivenToken(Factories.ConfirmationTokenFor(user.Id, Now), user);

        Result result = await _handler.Handle(new ConfirmEmailCommand("raw"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.EmailConfirmed.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithActiveToken_ConsumesIt()
    {
        User user = Factories.ActiveUser();
        EmailConfirmationToken token = Factories.ConfirmationTokenFor(user.Id, Now);
        GivenToken(token, user);

        await _handler.Handle(new ConfirmEmailCommand("raw"), CancellationToken.None);

        token.ConsumedAtUtc.Should().Be(Now);
    }

    [Fact]
    public async Task Handle_WithUnknownToken_ReturnsInvalidOrExpired()
    {
        _tokenRepository.GetByTokenHashAsync(TokenHash, Arg.Any<CancellationToken>())
            .Returns((EmailConfirmationToken?)null);

        Result result = await _handler.Handle(new ConfirmEmailCommand("raw"), CancellationToken.None);

        result.Error.Should().Be(EmailConfirmationErrors.InvalidOrExpired);
    }

    [Fact]
    public async Task Handle_WithAlreadyConsumedToken_ReturnsInvalidOrExpired()
    {
        User user = Factories.ActiveUser();
        EmailConfirmationToken token = Factories.ConfirmationTokenFor(user.Id, Now);
        token.Consume(Now);
        GivenToken(token, user);

        Result result = await _handler.Handle(new ConfirmEmailCommand("raw"), CancellationToken.None);

        result.Error.Should().Be(EmailConfirmationErrors.InvalidOrExpired);
    }

    [Fact]
    public async Task Handle_WithExpiredToken_ReturnsInvalidOrExpired()
    {
        User user = Factories.ActiveUser();
        GivenToken(Factories.ConfirmationTokenFor(user.Id, Now.AddDays(-2)), user);

        Result result = await _handler.Handle(new ConfirmEmailCommand("raw"), CancellationToken.None);

        result.Error.Should().Be(EmailConfirmationErrors.InvalidOrExpired);
    }

    [Fact]
    public async Task Handle_WhenTheAccountIsDeleted_ReturnsInvalidOrExpired()
    {
        User user = Factories.DeletedUser();
        GivenToken(Factories.ConfirmationTokenFor(user.Id, Now), user);

        Result result = await _handler.Handle(new ConfirmEmailCommand("raw"), CancellationToken.None);

        result.Error.Should().Be(EmailConfirmationErrors.InvalidOrExpired);
    }

    [Fact]
    public async Task Handle_WhenTheEmailIsAlreadyConfirmed_ReturnsEmailAlreadyConfirmed()
    {
        User user = Factories.ConfirmedUser();
        GivenToken(Factories.ConfirmationTokenFor(user.Id, Now), user);

        Result result = await _handler.Handle(new ConfirmEmailCommand("raw"), CancellationToken.None);

        result.Error.Should().Be(UserErrors.EmailAlreadyConfirmed);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private void GivenToken(EmailConfirmationToken token, User user)
    {
        _tokenRepository.GetByTokenHashAsync(TokenHash, Arg.Any<CancellationToken>()).Returns(token);
        _userRepository.GetByIdAsync(token.UserId, Arg.Any<CancellationToken>()).Returns(user);
    }
}
