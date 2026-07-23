using AuthService.Application.Authentication;
using Common.Application.Messaging;

namespace AuthService.Application.RefreshTokens.RefreshSession;

public sealed record RefreshSessionCommand(string RefreshToken, string? IpAddress) : ICommand<AuthTokensResponse>;
