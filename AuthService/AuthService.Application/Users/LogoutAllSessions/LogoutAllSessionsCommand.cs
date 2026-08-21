using Common.Application.Messaging;

namespace AuthService.Application.Users.LogoutAllSessions;

public sealed record LogoutAllSessionsCommand(Guid UserId) : ICommand;
