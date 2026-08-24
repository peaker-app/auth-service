namespace AuthService.Domain.EmailConfirmations;

public interface IEmailConfirmationTokenRepository
{
    Task<EmailConfirmationToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<EmailConfirmationToken>> GetActiveByUserAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<EmailConfirmationToken?> GetLatestByUserAsync(Guid userId, CancellationToken cancellationToken);

    void Add(EmailConfirmationToken token);
}
