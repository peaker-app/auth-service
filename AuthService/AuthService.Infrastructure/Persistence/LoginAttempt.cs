namespace AuthService.Infrastructure.Persistence;

public sealed class LoginAttempt
{
    public required Guid Id { get; init; }

    public required string IdentifierHash { get; init; }

    public required string IpHash { get; init; }

    public required DateTime AttemptedAtUtc { get; init; }
}
