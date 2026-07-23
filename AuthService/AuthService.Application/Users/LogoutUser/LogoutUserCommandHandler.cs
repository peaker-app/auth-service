using AuthService.Application.Abstractions;
using AuthService.Domain.RefreshTokens;
using Common.Application.Abstractions;
using Common.Application.Messaging;
using Common.Domain.Results;

namespace AuthService.Application.Users.LogoutUser;

internal sealed class LogoutUserCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IRefreshTokenGenerator refreshTokenGenerator,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<LogoutUserCommand>
{
    public async Task<Result> Handle(LogoutUserCommand command, CancellationToken cancellationToken)
    {
        string tokenHash = refreshTokenGenerator.Hash(command.RefreshToken);
        RefreshToken? token = await refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        DateTime utcNow = dateTimeProvider.UtcNow;

        if (token is null || token.UserId != command.UserId || !token.IsActive(utcNow))
            return Result.Success();

        token.Revoke(utcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
