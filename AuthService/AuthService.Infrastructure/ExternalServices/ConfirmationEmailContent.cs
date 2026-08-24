using System.Globalization;
using System.Net;
using MimeKit;

namespace AuthService.Infrastructure.ExternalServices;

internal sealed record ConfirmationEmailContent(string Subject, string Html, string Text)
{
    private const string ConfirmationSubject = "Confirma tu correo en Peaker";

    public static ConfirmationEmailContent For(EmailConfirmationOptions options, string rawToken)
    {
        string url = options.BuildConfirmationUri(rawToken).AbsoluteUri;
        string hours = options.TokenLifetime.TotalHours.ToString("0", CultureInfo.InvariantCulture);

        return new ConfirmationEmailContent(ConfirmationSubject, BuildHtml(url, hours), BuildText(url, hours));
    }

    public static MimeMessage ToMimeMessage(
        EmailConfirmationOptions options,
        string recipientEmail,
        string rawToken)
    {
        ConfirmationEmailContent content = For(options, rawToken);

        MimeMessage message = new() { Subject = content.Subject };
        message.From.Add(new MailboxAddress(options.FromName, options.FromAddress));
        message.To.Add(MailboxAddress.Parse(recipientEmail));
        message.Body = new BodyBuilder { HtmlBody = content.Html, TextBody = content.Text }.ToMessageBody();

        return message;
    }

    private static string BuildHtml(string confirmationUrl, string hours) =>
        $"""
         <p>¡Bienvenido a Peaker!</p>
         <p>Confirma tu dirección de correo para poder registrar tus ascensiones:</p>
         <p><a href="{WebUtility.HtmlEncode(confirmationUrl)}">Confirmar mi correo</a></p>
         <p>El enlace caduca en {hours} horas y solo puede usarse una vez.</p>
         """;

    private static string BuildText(string confirmationUrl, string hours) =>
        $"""
         ¡Bienvenido a Peaker!

         Confirma tu dirección de correo para poder registrar tus ascensiones:
         {confirmationUrl}

         El enlace caduca en {hours} horas y solo puede usarse una vez.
         """;
}
