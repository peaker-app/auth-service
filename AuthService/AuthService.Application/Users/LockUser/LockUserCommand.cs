using Common.Application.Messaging;

namespace AuthService.Application.Users.LockUser;

public sealed record LockUserCommand(Guid ActorId, Guid UserId) : ICommand;
