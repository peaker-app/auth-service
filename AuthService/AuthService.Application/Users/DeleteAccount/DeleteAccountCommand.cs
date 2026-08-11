using Common.Application.Messaging;

namespace AuthService.Application.Users.DeleteAccount;

public sealed record DeleteAccountCommand(Guid UserId, string Password) : ICommand;
