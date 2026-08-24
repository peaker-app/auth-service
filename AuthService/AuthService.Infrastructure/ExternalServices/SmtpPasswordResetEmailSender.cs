using AuthService.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AuthService.Infrastructure.ExternalServices;

internal sealed class SmtpPasswordResetEmailSender(
    IOptions<EmailConfirmationOptions> emailOptions,
    SmtpMailer mailer,
    ILogger<SmtpPasswordResetEmailSender> logger) : IPasswordResetEmailSender
{
    public async Task<bool> SendAsync(string recipientEmail, string rawToken, CancellationToken cancellationToken)
    {
#pragma warning disable CA1031
        try
        {
            using MimeMessage message = PasswordResetEmailContent.ToMimeMessage(
                emailOptions.Value, recipientEmail, rawToken);

            await mailer.DeliverAsync(message, cancellationToken);

            return true;
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Password reset email delivery failed and will be retried");
            return false;
        }
#pragma warning restore CA1031
    }
}
