namespace AuthService.Application.Abstractions;

public interface IExistingAccountEmailSender
{
    Task<bool> SendAsync(string recipientEmail, CancellationToken cancellationToken);
}
