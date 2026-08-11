using AuthService.Application.PasswordResets.ResetPassword;

namespace AuthService.API.Requests;

public sealed record ResetPasswordRequest(string Token, string NewPassword)
{
    public ResetPasswordCommand ToCommand() => new(Token, NewPassword);
}
