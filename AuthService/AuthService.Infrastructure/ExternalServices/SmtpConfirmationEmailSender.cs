using AuthService.Application.Abstractions;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace AuthService.Infrastructure.ExternalServices;

internal sealed class SmtpConfirmationEmailSender(
    IOptions<EmailConfirmationOptions> emailOptions,
    IOptions<SmtpOptions> smtpOptions,
    ILogger<SmtpConfirmationEmailSender> logger) : IConfirmationEmailSender
{
    public async Task<bool> SendAsync(string recipientEmail, string rawToken, CancellationToken cancellationToken)
    {
#pragma warning disable CA1031
        try
        {
            await DeliverAsync(recipientEmail, rawToken, cancellationToken);
            return true;
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Confirmation email delivery failed and will be retried");
            return false;
        }
#pragma warning restore CA1031
    }

    private async Task DeliverAsync(string recipientEmail, string rawToken, CancellationToken cancellationToken)
    {
        SmtpOptions transport = smtpOptions.Value;

        using MimeMessage message = ConfirmationEmailContent.ToMimeMessage(
            emailOptions.Value, recipientEmail, rawToken);

        using SmtpClient client = new() { Timeout = (int)transport.Timeout.TotalMilliseconds };

        await client.ConnectAsync(transport.Host, transport.Port, ToSocketOptions(transport.Security), cancellationToken);
        await AuthenticateAsync(client, transport, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);
    }

    private static async Task AuthenticateAsync(
        SmtpClient client,
        SmtpOptions transport,
        CancellationToken cancellationToken)
    {
        if (transport.HasCredentials)
        {
            await client.AuthenticateAsync(transport.Username, transport.Password, cancellationToken);
        }
    }

    private static SecureSocketOptions ToSocketOptions(SmtpSecurity security) => security switch
    {
        SmtpSecurity.StartTls => SecureSocketOptions.StartTls,
        SmtpSecurity.SslOnConnect => SecureSocketOptions.SslOnConnect,
        _ => SecureSocketOptions.None
    };
}
