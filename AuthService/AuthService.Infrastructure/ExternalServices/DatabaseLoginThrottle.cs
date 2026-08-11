using AuthService.Application.Abstractions;
using AuthService.Infrastructure.Persistence;
using Common.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AuthService.Infrastructure.ExternalServices;

internal sealed class DatabaseLoginThrottle(
    AuthDbContext context,
    IOptions<LoginThrottleOptions> options,
    IDateTimeProvider dateTimeProvider) : ILoginThrottle
{
    private const string UnknownIpAddress = "unknown";

    public async Task<LoginThrottleVerdict> EvaluateAsync(
        string identifier,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        LoginThrottleOptions limits = options.Value;
        DateTime utcNow = dateTimeProvider.UtcNow;

        DateTime? lastAttempt = await MostRecentAttemptAsync(identifier, ipAddress, utcNow, cancellationToken);

        if (lastAttempt is null)
        {
            return LoginThrottleVerdict.Allowed;
        }

        int failures = await CountFailuresAsync(identifier, ipAddress, utcNow, cancellationToken);

        if (failures < limits.FreeAttempts)
        {
            return LoginThrottleVerdict.Allowed;
        }

        TimeSpan elapsed = utcNow - lastAttempt.Value;
        TimeSpan required = ComputeDelay(failures, limits);

        return elapsed >= required
            ? LoginThrottleVerdict.Allowed
            : LoginThrottleVerdict.Blocked(required - elapsed);
    }

    public async Task RecordFailureAsync(
        string identifier,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        context.Set<LoginAttempt>().Add(new LoginAttempt
        {
            Id = Guid.CreateVersion7(),
            IdentifierHash = SubjectHash.Of(identifier),
            IpHash = SubjectHash.Of(ipAddress ?? UnknownIpAddress),
            AttemptedAtUtc = dateTimeProvider.UtcNow
        });

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task ClearAsync(string identifier, CancellationToken cancellationToken)
    {
        string identifierHash = SubjectHash.Of(identifier);

        await context.Set<LoginAttempt>()
            .Where(attempt => attempt.IdentifierHash == identifierHash)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static TimeSpan ComputeDelay(int failures, LoginThrottleOptions limits)
    {
        int level = failures - limits.FreeAttempts + 1;
        double seconds = limits.BaseDelay.TotalSeconds * Math.Pow(2, level - 1);

        return TimeSpan.FromSeconds(Math.Min(seconds, limits.MaxDelay.TotalSeconds));
    }

    private IQueryable<LoginAttempt> WithinWindow(string identifier, string? ipAddress, DateTime utcNow)
    {
        string identifierHash = SubjectHash.Of(identifier);
        string ipHash = SubjectHash.Of(ipAddress ?? UnknownIpAddress);
        DateTime windowStart = utcNow - options.Value.Window;

        return context.Set<LoginAttempt>()
            .AsNoTracking()
            .Where(attempt =>
                attempt.IdentifierHash == identifierHash &&
                attempt.IpHash == ipHash &&
                attempt.AttemptedAtUtc >= windowStart);
    }

    private Task<int> CountFailuresAsync(
        string identifier,
        string? ipAddress,
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        WithinWindow(identifier, ipAddress, utcNow).CountAsync(cancellationToken);

    private Task<DateTime?> MostRecentAttemptAsync(
        string identifier,
        string? ipAddress,
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        WithinWindow(identifier, ipAddress, utcNow)
            .OrderByDescending(attempt => attempt.AttemptedAtUtc)
            .Select(attempt => (DateTime?)attempt.AttemptedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
}
