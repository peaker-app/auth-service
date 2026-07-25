using AuthService.Application.Abstractions;
using AuthService.Application.Authentication;
using AuthService.Domain.RefreshTokens;
using AuthService.Domain.Users;
using Common.Application.Abstractions;
using Common.Application.Messaging;
using Common.Domain.Results;

namespace AuthService.Application.RefreshTokens.RefreshSession;

internal sealed class RefreshSessionCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IUserRepository userRepository,
    IRefreshTokenGenerator refreshTokenGenerator,
    IAuthTokenIssuer tokenIssuer,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<RefreshSessionCommand, AuthTokensResponse>
{
    public async Task<Result<AuthTokensResponse>> Handle(
        RefreshSessionCommand command,
        CancellationToken cancellationToken)
    {
        string tokenHash = refreshTokenGenerator.Hash(command.RefreshToken);
        RefreshToken? existing = await refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        DateTime utcNow = dateTimeProvider.UtcNow;

        if (existing is null) return Result.Failure<AuthTokensResponse>(RefreshTokenErrors.InvalidOrExpired);

        if (!existing.IsActive(utcNow))
            return await RevokeCompromisedSessionsAsync(existing.UserId, utcNow, cancellationToken);

        User? user = await userRepository.GetByIdAsync(existing.UserId, cancellationToken);
        if (user is null || user.IsDeleted)
            return Result.Failure<AuthTokensResponse>(RefreshTokenErrors.InvalidOrExpired);

        IssuedTokens issued = tokenIssuer.Issue(user, utcNow, command.IpAddress);
        existing.Revoke(utcNow, issued.RefreshToken.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return issued.Response;
    }

    private async Task<Result<AuthTokensResponse>> RevokeCompromisedSessionsAsync(
        Guid userId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<RefreshToken> activeTokens =
            await refreshTokenRepository.GetActiveByUserAsync(userId, cancellationToken);

        foreach (RefreshToken token in activeTokens)
        {
            token.Revoke(utcNow);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Failure<AuthTokensResponse>(RefreshTokenErrors.InvalidOrExpired);
    }
}
