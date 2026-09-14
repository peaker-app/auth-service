namespace AuthService.Infrastructure.Security;

public sealed class AuthTokenOptions
{
    public const string SectionName = "AuthToken";

    public string Issuer { get; init; } = "peaker-auth";

    public IReadOnlyList<string> Audiences { get; init; } = [];

    public string SelfAudience { get; init; } = string.Empty;

    public TimeSpan AccessTokenLifetime { get; init; } = TimeSpan.FromMinutes(15);

    public TimeSpan RefreshTokenLifetime { get; init; } = TimeSpan.FromDays(30);

    public TimeSpan RefreshTokenRotationLeeway { get; init; } = TimeSpan.FromSeconds(30);

    public string? PrivateKeyPem { get; init; }

    public string? PreviousPrivateKeyPem { get; init; }
}
