using Common.Application.Messaging;

namespace AuthService.Application.Users.UnlockUser;

public sealed record UnlockUserCommand(Guid ActorId, Guid UserId) : ICommand;
