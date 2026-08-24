using AuthService.Application.EmailConfirmations.ConfirmEmail;

namespace AuthService.API.Requests;

public sealed record ConfirmEmailRequest(string Token)
{
    public ConfirmEmailCommand ToCommand() => new(Token);
}
