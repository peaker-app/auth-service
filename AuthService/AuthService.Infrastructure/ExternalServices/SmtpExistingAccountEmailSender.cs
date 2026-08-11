using AuthService.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AuthService.Infrastructure.ExternalServices;

internal sealed class SmtpExistingAccountEmailSender(
    IOptions<EmailConfirmationOptions> emailOptions,
    SmtpMailer mailer,
    ILogger<SmtpExistingAccountEmailSender> logger) : IExistingAccountEmailSender
{
    public async Task<bool> SendAsync(string recipientEmail, CancellationToken cancellationToken)
    {
#pragma warning disable CA1031
        try
        {
            using MimeMessage message = ExistingAccountEmailContent.ToMimeMessage(
                emailOptions.Value, recipientEmail);

            await mailer.DeliverAsync(message, cancellationToken);

            return true;
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Existing account notice delivery failed and will be retried");
            return false;
        }
#pragma warning restore CA1031
    }
}
