namespace AuthService.Application.Users.ExportMyData;

public sealed record AccountSessionResponse(
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc,
    string? CreatedByIp);

public sealed record AccountExportResponse(
    Guid UserId,
    string Email,
    string Username,
    string Status,
    bool EmailConfirmed,
    IReadOnlyCollection<string> Roles,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyCollection<AccountSessionResponse> ActiveSessions);
