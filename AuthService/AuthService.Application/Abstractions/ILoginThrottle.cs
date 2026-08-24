namespace AuthService.Application.Abstractions;

public sealed record LoginThrottleVerdict(bool IsAllowed, TimeSpan RetryAfter)
{
    public static readonly LoginThrottleVerdict Allowed = new(true, TimeSpan.Zero);

    public static LoginThrottleVerdict Blocked(TimeSpan retryAfter) => new(false, retryAfter);
}

public interface ILoginThrottle
{
    Task<LoginThrottleVerdict> EvaluateAsync(
        string identifier,
        string? ipAddress,
        CancellationToken cancellationToken);

    Task RecordFailureAsync(string identifier, string? ipAddress, CancellationToken cancellationToken);

    Task ClearAsync(string identifier, CancellationToken cancellationToken);
}
