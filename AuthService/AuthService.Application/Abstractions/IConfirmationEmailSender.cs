namespace AuthService.Application.Abstractions;

public interface IConfirmationEmailSender
{
    Task<bool> SendAsync(string recipientEmail, string rawToken, CancellationToken cancellationToken);
}
