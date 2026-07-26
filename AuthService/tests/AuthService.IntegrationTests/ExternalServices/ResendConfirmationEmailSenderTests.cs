using System.Net;
using System.Text.Json;
using AuthService.Infrastructure.ExternalServices;
using AuthService.IntegrationTests.Fakes;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AuthService.IntegrationTests.ExternalServices;

public sealed class ResendConfirmationEmailSenderTests
{
    private const string Recipient = "hiker@peaker.io";
    private const string RawToken = "raw-token";

    [Fact]
    public async Task SendAsync_PostsTheResendPayloadWithLowerCaseFields()
    {
        using CapturingHttpMessageHandler handler = new(HttpStatusCode.OK);
        using HttpClient client = ClientOver(handler);

        await SenderOver(client).SendAsync(Recipient, RawToken, CancellationToken.None);

        using JsonDocument payload = JsonDocument.Parse(handler.LastBody!);
        payload.RootElement.GetProperty("from").GetString().Should().Be("Peaker <no-reply@peaker.io>");
        payload.RootElement.GetProperty("to")[0].GetString().Should().Be(Recipient);
        payload.RootElement.GetProperty("subject").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SendAsync_PutsTheConfirmationLinkInBothBodies()
    {
        using CapturingHttpMessageHandler handler = new(HttpStatusCode.OK);
        using HttpClient client = ClientOver(handler);

        await SenderOver(client).SendAsync(Recipient, RawToken, CancellationToken.None);

        using JsonDocument payload = JsonDocument.Parse(handler.LastBody!);
        payload.RootElement.GetProperty("html").GetString().Should().Contain(RawToken);
        payload.RootElement.GetProperty("text").GetString().Should().Contain(RawToken);
    }

    [Fact]
    public async Task SendAsync_AuthenticatesWithTheConfiguredApiKey()
    {
        using CapturingHttpMessageHandler handler = new(HttpStatusCode.OK);
        using HttpClient client = ClientOver(handler);

        await SenderOver(client).SendAsync(Recipient, RawToken, CancellationToken.None);

        handler.LastRequest!.Headers.Authorization!.Scheme.Should().Be("Bearer");
        handler.LastRequest.Headers.Authorization.Parameter.Should().Be("test-api-key");
    }

    [Fact]
    public async Task SendAsync_WhenResendRejectsTheRequest_ReportsUndelivered()
    {
        using CapturingHttpMessageHandler handler = new(HttpStatusCode.UnprocessableEntity);
        using HttpClient client = ClientOver(handler);

        bool delivered = await SenderOver(client).SendAsync(Recipient, RawToken, CancellationToken.None);

        delivered.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_WhenResendIsUnreachable_ReportsUndelivered()
    {
        using ThrowingHttpMessageHandler handler = new();
        using HttpClient client = ClientOver(handler);

        bool delivered = await SenderOver(client).SendAsync(Recipient, RawToken, CancellationToken.None);

        delivered.Should().BeFalse();
    }

    private static HttpClient ClientOver(HttpMessageHandler handler) =>
        new(handler) { BaseAddress = new Uri("https://api.resend.com/") };

    private static ResendConfirmationEmailSender SenderOver(HttpClient client) =>
        new(
            client,
            Options.Create(new EmailConfirmationOptions
            {
                ApiKey = "test-api-key",
                FromAddress = "no-reply@peaker.io",
                FromName = "Peaker",
                ConfirmationLinkTemplate = "https://peaker.io/confirm-email?token={token}"
            }),
            NullLogger<ResendConfirmationEmailSender>.Instance);
}
