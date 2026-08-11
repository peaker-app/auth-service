using AuthService.Application.PasswordResets.RequestPasswordReset;

namespace AuthService.API.Requests;

public sealed record ForgotPasswordRequest(string Email)
{
    public RequestPasswordResetCommand ToCommand() => new(Email);
}
