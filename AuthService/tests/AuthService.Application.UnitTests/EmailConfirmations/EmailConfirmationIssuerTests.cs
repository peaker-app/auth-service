using AuthService.Application.Abstractions;
using AuthService.Application.EmailConfirmations;
using AuthService.Application.UnitTests.TestData;
using AuthService.Domain.EmailConfirmations;
using AuthService.Domain.Users;
using Common.Application.Abstractions;
using Common.Domain.Results;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AuthService.Application.UnitTests.EmailConfirmations;

public sealed class EmailConfirmationIssuerTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly IEmailConfirmationTokenRepository _tokenRepository =
        Substitute.For<IEmailConfirmationTokenRepository>();

    private readonly IEmailConfirmationTokenGenerator _tokenGenerator =
        Substitute.For<IEmailConfirmationTokenGenerator>();

    private readonly IConfirmationEmailSender _emailSender = Substitute.For<IConfirmationEmailSender>();
    private readonly IEmailQuota _emailQuota = Substitute.For<IEmailQuota>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly EmailConfirmationIssuer _issuer;

    public EmailConfirmationIssuerTests()
    {
        _dateTimeProvider.UtcNow.Returns(Now);
        _tokenGenerator.Generate(Now).Returns(new GeneratedEmailConfirmationToken(
            "raw-token", "hashed-token", Now.AddHours(24)));
        _emailSender.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _tokenRepository.GetActiveByUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);

        _emailQuota.TryReserveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(EmailQuotaVerdict.Allowed);

        _issuer = new EmailConfirmationIssuer(
            _tokenRepository, _tokenGenerator, _emailSender, _emailQuota, _unitOfWork, _dateTimeProvider);
    }

    [Fact]
    public async Task IssueAsync_WhenTheRecipientQuotaIsExhausted_NeitherIssuesNorSends()
    {
        _emailQuota.TryReserveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(EmailQuotaVerdict.RecipientExhausted);

        Result result = await _issuer.IssueAsync(Factories.ActiveUser(), CancellationToken.None);

        result.Error.Should().Be(EmailConfirmationErrors.RecipientQuotaExceeded);
        _tokenRepository.DidNotReceive().Add(Arg.Any<EmailConfirmationToken>());
        await _emailSender.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IssueAsync_WhenTheGlobalQuotaIsExhausted_ReturnsGlobalQuotaExceeded()
    {
        _emailQuota.TryReserveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(EmailQuotaVerdict.GlobalExhausted);

        Result result = await _issuer.IssueAsync(Factories.ActiveUser(), CancellationToken.None);

        result.Error.Should().Be(EmailConfirmationErrors.GlobalQuotaExceeded);
    }

    [Fact]
    public async Task IssueAsync_PersistsOnlyTheHashedToken()
    {
        User user = Factories.ActiveUser();

        await _issuer.IssueAsync(user, CancellationToken.None);

        _tokenRepository.Received(1).Add(Arg.Is<EmailConfirmationToken>(token =>
            token!.TokenHash == "hashed-token" && token.UserId == user.Id));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IssueAsync_SendsTheRawTokenToTheAccountEmail()
    {
        User user = Factories.ActiveUser();

        Result result = await _issuer.IssueAsync(user, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _emailSender.Received(1).SendAsync(
            Factories.DefaultEmail, "raw-token", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IssueAsync_InvalidatesThePreviouslyActiveTokens()
    {
        User user = Factories.ActiveUser();
        EmailConfirmationToken previous = Factories.ConfirmationTokenFor(user.Id, Now.AddHours(-1));
        _tokenRepository.GetActiveByUserAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns([previous]);

        await _issuer.IssueAsync(user, CancellationToken.None);

        previous.IsActive(Now).Should().BeFalse();
    }

    [Fact]
    public async Task IssueAsync_WhenDeliveryFails_ReturnsDeliveryFailed()
    {
        _emailSender.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        Result result = await _issuer.IssueAsync(Factories.ActiveUser(), CancellationToken.None);

        result.Error.Should().Be(EmailConfirmationErrors.DeliveryFailed);
    }
}
