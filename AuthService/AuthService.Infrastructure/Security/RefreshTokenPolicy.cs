using AuthService.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace AuthService.Infrastructure.Security;

internal sealed class RefreshTokenPolicy(IOptions<AuthTokenOptions> options) : IRefreshTokenPolicy
{
    public TimeSpan RotationLeeway => options.Value.RefreshTokenRotationLeeway;
}
