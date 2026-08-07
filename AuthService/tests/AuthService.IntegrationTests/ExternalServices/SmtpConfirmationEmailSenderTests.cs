using System.Text.Json;
using AuthService.Infrastructure.ExternalServices;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AuthService.IntegrationTests.ExternalServices;

public sealed class SmtpConfirmationEmailSenderTests : IAsyncLifetime
{
    private const int SmtpPort = 1025;
    private const int ApiPort = 8025;
    private const string Recipient = "hiker@peaker.io";

    private readonly IContainer _mailpit = new ContainerBuilder("axllent/mailpit:v1.21")
        .WithPortBinding(SmtpPort, assignRandomHostPort: true)
        .WithPortBinding(ApiPort, assignRandomHostPort: true)
        .WithWaitStrategy(Wait.ForUnixContainer()
            .UntilHttpRequestIsSucceeded(request => request.ForPort(ApiPort).ForPath("/readyz")))
        .Build();

    [Fact]
    public async Task SendAsync_DeliversTheConfirmationLinkToTheRecipient()
    {
        string rawToken = Guid.NewGuid().ToString("N");

        bool delivered = await SenderOver(MailpitTransport())
            .SendAsync(Recipient, rawToken, CancellationToken.None);

        delivered.Should().BeTrue();

        JsonElement message = await FetchOnlyMessageAsync();
        message.GetProperty("To")[0].GetProperty("Address").GetString().Should().Be(Recipient);
        message.GetProperty("From").GetProperty("Address").GetString().Should().Be("no-reply@peaker.io");
        message.GetProperty("From").GetProperty("Name").GetString().Should().Be("Peaker");
        message.GetProperty("Subject").GetString().Should().NotBeNullOrWhiteSpace();
        message.GetProperty("HTML").GetString().Should().Contain(rawToken);
        message.GetProperty("Text").GetString().Should().Contain(rawToken);
    }

    [Fact]
    public async Task SendAsync_WhenTheServerIsUnreachable_ReportsUndelivered()
    {
        SmtpOptions unreachable = new()
        {
            Host = "localhost",
            Port = 1,
            Timeout = TimeSpan.FromSeconds(2)
        };

        bool delivered = await SenderOver(unreachable).SendAsync(Recipient, "raw-token", CancellationToken.None);

        delivered.Should().BeFalse();
    }

    private SmtpOptions MailpitTransport() => new()
    {
        Host = _mailpit.Hostname,
        Port = _mailpit.GetMappedPublicPort(SmtpPort),
        Security = SmtpSecurity.None,
        Timeout = TimeSpan.FromSeconds(10)
    };

    private static SmtpConfirmationEmailSender SenderOver(SmtpOptions transport) =>
        new(
            Options.Create(new EmailConfirmationOptions
            {
                FromAddress = "no-reply@peaker.io",
                FromName = "Peaker",
                ConfirmationLinkTemplate = "https://peaker.io/confirm-email?token={token}"
            }),
            Options.Create(transport),
            NullLogger<SmtpConfirmationEmailSender>.Instance);

    private async Task<JsonElement> FetchOnlyMessageAsync()
    {
        using HttpClient client = new()
        {
            BaseAddress = new Uri($"http://{_mailpit.Hostname}:{_mailpit.GetMappedPublicPort(ApiPort)}")
        };

        string summaries = await client.GetStringAsync(new Uri("api/v1/messages", UriKind.Relative));

        using JsonDocument inbox = JsonDocument.Parse(summaries);
        string id = inbox.RootElement.GetProperty("messages")[0].GetProperty("ID").GetString()!;

        string detail = await client.GetStringAsync(new Uri($"api/v1/message/{id}", UriKind.Relative));

        using JsonDocument message = JsonDocument.Parse(detail);
        return message.RootElement.Clone();
    }

    async Task IAsyncLifetime.InitializeAsync() => await _mailpit.StartAsync();

    async Task IAsyncLifetime.DisposeAsync() => await _mailpit.DisposeAsync();
}
