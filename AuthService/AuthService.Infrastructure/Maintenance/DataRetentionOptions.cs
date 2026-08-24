namespace AuthService.Infrastructure.Maintenance;

public sealed class DataRetentionOptions
{
    public const string SectionName = "DataRetention";

    public bool Enabled { get; init; } = true;

    public TimeSpan Interval { get; init; } = TimeSpan.FromHours(24);

    public TimeSpan LoginAttempts { get; init; } = TimeSpan.FromDays(30);

    public TimeSpan EmailDispatches { get; init; } = TimeSpan.FromDays(30);

    public TimeSpan ClosedSessions { get; init; } = TimeSpan.FromDays(90);

    public TimeSpan SpentTokens { get; init; } = TimeSpan.FromDays(30);
}
