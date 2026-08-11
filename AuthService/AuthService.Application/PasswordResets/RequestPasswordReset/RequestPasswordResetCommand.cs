using Common.Application.Messaging;

namespace AuthService.Application.PasswordResets.RequestPasswordReset;

public sealed record RequestPasswordResetCommand(string Email) : ICommand;
