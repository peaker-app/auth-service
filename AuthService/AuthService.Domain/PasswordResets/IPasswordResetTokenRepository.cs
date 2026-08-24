namespace AuthService.Domain.PasswordResets;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<PasswordResetToken>> GetActiveByUserAsync(
        Guid userId,
        CancellationToken cancellationToken);

    void Add(PasswordResetToken token);
}
