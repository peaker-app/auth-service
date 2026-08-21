using AuthService.Application.Abstractions;
using AuthService.Domain.PasswordResets;
using Common.Application.Abstractions;
using Common.Domain.Results;

namespace AuthService.Application.PasswordResets;

internal interface IPasswordResetTokenRedeemer
{
    Task<Result<Guid>> RedeemAsync(string rawToken, CancellationToken cancellationToken);
}

internal sealed class PasswordResetTokenRedeemer(
    IPasswordResetTokenRepository tokenRepository,
    IPasswordResetTokenGenerator tokenGenerator,
    IDateTimeProvider dateTimeProvider) : IPasswordResetTokenRedeemer
{
    public async Task<Result<Guid>> RedeemAsync(string rawToken, CancellationToken cancellationToken)
    {
        string tokenHash = tokenGenerator.Hash(rawToken);
        PasswordResetToken? token = await tokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (token is null)
        {
            return Result.Failure<Guid>(PasswordResetErrors.InvalidOrExpired);
        }

        Result consumption = token.Consume(dateTimeProvider.UtcNow);

        return consumption.IsFailure
            ? Result.Failure<Guid>(consumption.Error)
            : Result.Success(token.UserId);
    }
}
