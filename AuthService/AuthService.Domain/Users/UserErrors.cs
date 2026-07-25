using Common.Domain.Results;

namespace AuthService.Domain.Users;

public static class UserErrors
{
    public static readonly Error EmailEmpty =
        Error.Validation("User.EmailEmpty", "El correo electrónico es obligatorio.");

    public static readonly Error EmailTooLong =
        Error.Validation("User.EmailTooLong", "El correo electrónico supera la longitud máxima permitida.");

    public static readonly Error EmailInvalid =
        Error.Validation("User.EmailInvalid", "El formato del correo electrónico no es válido.");

    public static readonly Error UsernameEmpty =
        Error.Validation("User.UsernameEmpty", "El nombre de usuario es obligatorio.");

    public static readonly Error UsernameInvalid =
        Error.Validation(
            "User.UsernameInvalid",
            $"El nombre de usuario debe tener entre {Username.MinLength} y {Username.MaxLength} caracteres " +
            "alfanuméricos, punto, guion o guion bajo, empezando y acabando en letra o dígito.");

    public static readonly Error UsernameAlreadyRegistered =
        Error.Conflict("User.UsernameAlreadyRegistered", "El nombre de usuario ya está registrado.");

    public static readonly Error PasswordHashMissing =
        Error.Validation("User.PasswordHashMissing", "La credencial de la cuenta es obligatoria.");

    public static readonly Error PasswordBreached =
        Error.Validation("User.PasswordBreached", "La contraseña aparece en listas de filtraciones conocidas.");

    public static readonly Error EmailAlreadyRegistered =
        Error.Conflict("User.EmailAlreadyRegistered", "El correo electrónico ya está registrado.");

    public static readonly Error InvalidCredentials =
        Error.Unauthorized("User.InvalidCredentials", "Las credenciales no son válidas.");

    public static readonly Error AlreadyDeleted =
        Error.Conflict("User.AlreadyDeleted", "La cuenta ya está dada de baja.");

    public static Error NotFound(Guid userId) =>
        Error.NotFound("User.NotFound", $"No existe el usuario {userId}.");
}
