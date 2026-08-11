using System.Net;
using MimeKit;

namespace AuthService.Infrastructure.ExternalServices;

internal sealed record ExistingAccountEmailContent(string Subject, string Html, string Text)
{
    private const string ExistingAccountSubject = "Ya tienes una cuenta en Peaker";

    public static ExistingAccountEmailContent For(EmailConfirmationOptions options)
    {
        string url = options.SignInUri.AbsoluteUri;

        return new ExistingAccountEmailContent(ExistingAccountSubject, BuildHtml(url), BuildText(url));
    }

    public static MimeMessage ToMimeMessage(EmailConfirmationOptions options, string recipientEmail)
    {
        ExistingAccountEmailContent content = For(options);

        MimeMessage message = new() { Subject = content.Subject };
        message.From.Add(new MailboxAddress(options.FromName, options.FromAddress));
        message.To.Add(MailboxAddress.Parse(recipientEmail));
        message.Body = new BodyBuilder { HtmlBody = content.Html, TextBody = content.Text }.ToMessageBody();

        return message;
    }

    private static string BuildHtml(string signInUrl) =>
        $"""
         <p>Alguien ha intentado crear una cuenta en Peaker con esta dirección de correo.</p>
         <p>Ya tienes una cuenta, así que no hemos creado ninguna nueva.</p>
         <p><a href="{WebUtility.HtmlEncode(signInUrl)}">Iniciar sesión</a></p>
         <p>Si no has sido tú, puedes ignorar este mensaje: tu cuenta no ha cambiado.</p>
         """;

    private static string BuildText(string signInUrl) =>
        $"""
         Alguien ha intentado crear una cuenta en Peaker con esta dirección de correo.

         Ya tienes una cuenta, así que no hemos creado ninguna nueva.
         {signInUrl}

         Si no has sido tú, puedes ignorar este mensaje: tu cuenta no ha cambiado.
         """;
}
