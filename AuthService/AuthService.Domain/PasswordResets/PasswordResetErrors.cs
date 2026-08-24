using Common.Domain.Results;

namespace AuthService.Domain.PasswordResets;

public static class PasswordResetErrors
{
    public static readonly Error InvalidOrExpired = Error.Validation(
        "PasswordReset.InvalidOrExpired",
        "El enlace para restablecer la contraseña no es válido, ya se ha usado o ha caducado.");

    public static readonly Error RecipientQuotaExceeded = Error.TooManyRequests(
        "PasswordReset.RecipientQuotaExceeded",
        "Se han enviado demasiados correos a esa dirección. Inténtalo más tarde.");

    public static readonly Error GlobalQuotaExceeded = Error.Unavailable(
        "PasswordReset.GlobalQuotaExceeded",
        "El servicio de correo ha agotado su cuota. Inténtalo más tarde.");

    public static readonly Error DeliveryFailed = Error.Unavailable(
        "PasswordReset.DeliveryFailed",
        "No se pudo enviar el correo para restablecer la contraseña. Inténtalo de nuevo en unos instantes.");
}
