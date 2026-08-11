namespace AuthService.Infrastructure.ExternalServices;

public sealed class EmailQuotaOptions
{
    public const string SectionName = "EmailQuota";

    public int PerRecipientPerHour { get; init; } = 3;

    public int PerRecipientPerDay { get; init; } = 5;

    public int GlobalPerHour { get; init; } = 150;
}
