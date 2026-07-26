using Common.Application.Messaging;

namespace AuthService.Application.EmailConfirmations.ResendEmailConfirmation;

public sealed record ResendEmailConfirmationCommand(Guid UserId) : ICommand;
