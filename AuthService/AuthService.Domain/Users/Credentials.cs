using Common.Domain.Results;

namespace AuthService.Domain.Users;

public sealed record Credentials(Email Email, Username Username)
{
    public static Result<Credentials> Create(string email, string username)
    {
        Result<Email> parsedEmail = Email.Create(email);

        if (parsedEmail.IsFailure)
        {
            return Result.Failure<Credentials>(parsedEmail.Error);
        }

        Result<Username> parsedUsername = Username.Create(username);

        return parsedUsername.IsFailure
            ? Result.Failure<Credentials>(parsedUsername.Error)
            : new Credentials(parsedEmail.Value, parsedUsername.Value);
    }
}
