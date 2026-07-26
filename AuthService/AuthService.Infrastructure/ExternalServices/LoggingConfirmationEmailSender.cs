using AuthService.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuthService.Infrastructure.ExternalServices;

// Motivo: sustituto de Resend cuando no hay API key. Escribe el enlace —y con él el token— en el log,
// algo que DEVELOPMENT.md §4.11 prohíbe en cualquier entorno real; solo llega a ejecutarse en
// Development porque fuera de Development la validación de EmailConfirmationOptions exige la API key y
// el arranque falla. Sin él no hay forma de completar el flujo en local sin una cuenta de Resend.
internal sealed class LoggingConfirmationEmailSender(
    IOptions<EmailConfirmationOptions> options,
    ILogger<LoggingConfirmationEmailSender> logger) : IConfirmationEmailSender
{
    public Task<bool> SendAsync(string recipientEmail, string rawToken, CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "Development confirmation link for {Recipient}: {ConfirmationLink}",
            recipientEmail,
            options.Value.BuildConfirmationUri(rawToken).AbsoluteUri);

        return Task.FromResult(true);
    }
}
