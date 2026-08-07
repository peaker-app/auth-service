using System.ComponentModel.DataAnnotations;

namespace AuthService.Infrastructure.ExternalServices;

public sealed class EmailConfirmationOptions
{
    public const string SectionName = "EmailConfirmation";

    [Required]
    public string FromAddress { get; init; } = "no-reply@peaker.io";

    [Required]
    public string FromName { get; init; } = "Peaker";

    [Required]
    public string ConfirmationLinkTemplate { get; init; } = "http://localhost:3000/confirm-email?token={token}";

    public TimeSpan TokenLifetime { get; init; } = TimeSpan.FromHours(24);

    public Uri BuildConfirmationUri(string rawToken) => new(
        ConfirmationLinkTemplate.Replace("{token}", Uri.EscapeDataString(rawToken), StringComparison.Ordinal),
        UriKind.Absolute);
}
