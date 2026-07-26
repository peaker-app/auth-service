using Common.Application.Messaging;

namespace AuthService.Application.EmailConfirmations.ConfirmEmail;

public sealed record ConfirmEmailCommand(string Token) : ICommand;
