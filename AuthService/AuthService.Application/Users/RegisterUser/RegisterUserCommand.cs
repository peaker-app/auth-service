using Common.Application.Messaging;

namespace AuthService.Application.Users.RegisterUser;

public sealed record RegisterUserCommand(string Email, string Password) : ICommand<Guid>;
