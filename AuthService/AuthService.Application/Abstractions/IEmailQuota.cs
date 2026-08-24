namespace AuthService.Application.Abstractions;

public enum EmailQuotaVerdict
{
    Allowed = 0,
    RecipientExhausted = 1,
    GlobalExhausted = 2
}

public interface IEmailQuota
{
    Task<EmailQuotaVerdict> TryReserveAsync(string recipientEmail, CancellationToken cancellationToken);
}
