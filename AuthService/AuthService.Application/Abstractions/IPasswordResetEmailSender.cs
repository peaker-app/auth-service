namespace AuthService.Application.Abstractions;

public interface IPasswordResetEmailSender
{
    Task<bool> SendAsync(string recipientEmail, string rawToken, CancellationToken cancellationToken);
}
