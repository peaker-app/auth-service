namespace AuthService.Infrastructure.Security;

public sealed class AuthTokenOptions
{
    public const string SectionName = "AuthToken";

    public string Issuer { get; init; } = "peaker-auth";

    public string Audience { get; init; } = "peaker-api";

    public TimeSpan AccessTokenLifetime { get; init; } = TimeSpan.FromMinutes(15);

    public TimeSpan RefreshTokenLifetime { get; init; } = TimeSpan.FromDays(7);

    public string? PrivateKeyPem { get; init; }
}
