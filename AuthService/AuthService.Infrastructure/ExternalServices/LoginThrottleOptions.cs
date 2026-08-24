using System.ComponentModel.DataAnnotations;

namespace AuthService.Infrastructure.ExternalServices;

public sealed class LoginThrottleOptions
{
    public const string SectionName = "LoginThrottle";

    [Range(1, 50)]
    public int FreeAttempts { get; init; } = 5;

    public TimeSpan BaseDelay { get; init; } = TimeSpan.FromSeconds(30);

    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromMinutes(15);

    public TimeSpan Window { get; init; } = TimeSpan.FromMinutes(15);
}
