using Common.Application.Messaging;

namespace AuthService.Application.Users.RegisterUser;

public sealed record RegisterUserCommand(
    string Email,
    string Username,
    string Password,
    bool AcceptedTerms) : ICommand;
