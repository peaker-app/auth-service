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

    [Required]
    public string SignInLink { get; init; } = "http://localhost:3000/login";

    [Required]
    public string PasswordResetLinkTemplate { get; init; } = "http://localhost:3000/reset-password?token={token}";

    public TimeSpan TokenLifetime { get; init; } = TimeSpan.FromHours(24);

    public TimeSpan PasswordResetTokenLifetime { get; init; } = TimeSpan.FromHours(1);

    public Uri SignInUri => new(SignInLink, UriKind.Absolute);

    public Uri BuildConfirmationUri(string rawToken) => Build(ConfirmationLinkTemplate, rawToken);

    public Uri BuildPasswordResetUri(string rawToken) => Build(PasswordResetLinkTemplate, rawToken);

    private static Uri Build(string template, string rawToken) => new(
        template.Replace("{token}", Uri.EscapeDataString(rawToken), StringComparison.Ordinal),
        UriKind.Absolute);
}
