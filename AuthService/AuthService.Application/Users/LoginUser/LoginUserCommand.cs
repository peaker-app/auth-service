using AuthService.Application.Authentication;
using Common.Application.Messaging;

namespace AuthService.Application.Users.LoginUser;

public sealed record LoginUserCommand(string Identifier, string Password, string? IpAddress)
    : ICommand<AuthTokensResponse>;
