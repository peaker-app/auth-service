namespace AuthService.Domain.RefreshTokens;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task<bool> IsActiveAsync(Guid refreshTokenId, DateTime utcNow, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<RefreshToken>> GetActiveByUserAsync(
        Guid userId,
        DateTime utcNow,
        CancellationToken cancellationToken);

    void Add(RefreshToken refreshToken);
}
