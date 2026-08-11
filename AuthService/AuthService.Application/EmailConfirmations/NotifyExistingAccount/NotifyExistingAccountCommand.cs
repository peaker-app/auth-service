using Common.Application.Messaging;

namespace AuthService.Application.EmailConfirmations.NotifyExistingAccount;

public sealed record NotifyExistingAccountCommand(Guid UserId) : ICommand;
