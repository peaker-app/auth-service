using Common.Application.Messaging;

namespace AuthService.Application.PasswordResets.ResetPassword;

public sealed record ResetPasswordCommand(string Token, string NewPassword) : ICommand;
