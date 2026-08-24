using Common.Application.Messaging;

namespace AuthService.Application.Users.LogoutUser;

public sealed record LogoutUserCommand(string RefreshToken, Guid UserId) : ICommand;
