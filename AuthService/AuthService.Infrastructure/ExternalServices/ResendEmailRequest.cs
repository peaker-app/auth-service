using System.Globalization;
using System.Net;

namespace AuthService.Infrastructure.ExternalServices;

internal sealed record ResendEmailRequest(string From, string[] To, string Subject, string Html, string Text)
{
    private const string ConfirmationSubject = "Confirma tu correo en Peaker";

    public static ResendEmailRequest Confirmation(
        EmailConfirmationOptions options,
        string recipientEmail,
        string rawToken)
    {
        string url = options.BuildConfirmationUri(rawToken).AbsoluteUri;
        string hours = options.TokenLifetime.TotalHours.ToString("0", CultureInfo.InvariantCulture);

        return new ResendEmailRequest(
            $"{options.FromName} <{options.FromAddress}>",
            [recipientEmail],
            ConfirmationSubject,
            BuildHtml(url, hours),
            BuildText(url, hours));
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
