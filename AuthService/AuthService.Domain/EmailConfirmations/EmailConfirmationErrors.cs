using Common.Domain.Results;

namespace AuthService.Domain.EmailConfirmations;

public static class EmailConfirmationErrors
{
    public static readonly Error InvalidOrExpired = Error.Validation(
        "EmailConfirmation.InvalidOrExpired",
        "El enlace de confirmación no es válido, ya se ha usado o ha caducado.");

    public static readonly Error ResendTooSoon = Error.TooManyRequests(
        "EmailConfirmation.ResendTooSoon",
        $"Espera {EmailConfirmationToken.ResendCooldown.TotalMinutes:0} minutos antes de pedir otro enlace.");

    public static readonly Error DeliveryFailed = Error.Unavailable(
        "EmailConfirmation.DeliveryFailed",
        "No se pudo enviar el correo de confirmación. Inténtalo de nuevo en unos instantes.");
}
