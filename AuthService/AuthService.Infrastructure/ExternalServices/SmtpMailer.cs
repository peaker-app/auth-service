using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace AuthService.Infrastructure.ExternalServices;

internal sealed class SmtpMailer(IOptions<SmtpOptions> smtpOptions)
{
    public async Task DeliverAsync(MimeMessage message, CancellationToken cancellationToken)
    {
        SmtpOptions transport = smtpOptions.Value;

        using SmtpClient client = new() { Timeout = (int)transport.Timeout.TotalMilliseconds };

        await client.ConnectAsync(
            transport.Host, transport.Port, ToSocketOptions(transport.Security), cancellationToken);
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
