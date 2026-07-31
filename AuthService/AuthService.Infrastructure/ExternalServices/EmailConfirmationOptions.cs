using System.ComponentModel.DataAnnotations;

namespace AuthService.Infrastructure.ExternalServices;

public sealed class EmailConfirmationOptions
{
    public const string SectionName = "EmailConfirmation";

    public string? ApiKey { get; init; }

    [Required]
    public Uri BaseAddress { get; init; } = new("https://api.resend.com/");

    [Required]
    public string FromAddress { get; init; } = "no-reply@peaker.io";

    [Required]
    public string FromName { get; init; } = "Peaker";

    [Required]
    public string ConfirmationLinkTemplate { get; init; } = "http://localhost:3000/confirm-email?token={token}";

    public TimeSpan TokenLifetime { get; init; } = TimeSpan.FromHours(24);

    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(10);

    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);

    public Uri BuildConfirmationUri(string rawToken) => new(
        ConfirmationLinkTemplate.Replace("{token}", Uri.EscapeDataString(rawToken), StringComparison.Ordinal),
        UriKind.Absolute);
}
