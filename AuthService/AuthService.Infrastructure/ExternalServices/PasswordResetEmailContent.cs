using System.Globalization;
using System.Net;
using MimeKit;

namespace AuthService.Infrastructure.ExternalServices;

internal sealed record PasswordResetEmailContent(string Subject, string Html, string Text)
{
    private const string ResetSubject = "Restablece tu contraseña de Peaker";

    public static PasswordResetEmailContent For(EmailConfirmationOptions options, string rawToken)
    {
        string url = options.BuildPasswordResetUri(rawToken).AbsoluteUri;
        string minutes = options.PasswordResetTokenLifetime.TotalMinutes.ToString("0", CultureInfo.InvariantCulture);

        return new PasswordResetEmailContent(ResetSubject, BuildHtml(url, minutes), BuildText(url, minutes));
    }

    public static MimeMessage ToMimeMessage(
        EmailConfirmationOptions options,
        string recipientEmail,
        string rawToken)
    {
        PasswordResetEmailContent content = For(options, rawToken);

        MimeMessage message = new() { Subject = content.Subject };
        message.From.Add(new MailboxAddress(options.FromName, options.FromAddress));
        message.To.Add(MailboxAddress.Parse(recipientEmail));
        message.Body = new BodyBuilder { HtmlBody = content.Html, TextBody = content.Text }.ToMessageBody();

        return message;
    }

    private static string BuildHtml(string resetUrl, string minutes) =>
        $"""
         <p>Hemos recibido una petición para restablecer tu contraseña de Peaker.</p>
         <p><a href="{WebUtility.HtmlEncode(resetUrl)}">Elegir una contraseña nueva</a></p>
         <p>El enlace caduca en {minutes} minutos y solo puede usarse una vez.</p>
         <p>Si no has sido tú, ignora este correo: tu contraseña actual sigue siendo válida.</p>
         """;

    private static string BuildText(string resetUrl, string minutes) =>
        $"""
         Hemos recibido una petición para restablecer tu contraseña de Peaker.

         Elige una contraseña nueva aquí:
         {resetUrl}

         El enlace caduca en {minutes} minutos y solo puede usarse una vez.

         Si no has sido tú, ignora este correo: tu contraseña actual sigue siendo válida.
         """;
}
