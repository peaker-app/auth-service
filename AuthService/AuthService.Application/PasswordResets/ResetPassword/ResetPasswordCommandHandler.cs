using AuthService.Application.Abstractions;
using AuthService.Application.Authentication;
using AuthService.Domain.PasswordResets;
using AuthService.Domain.Users;
using Common.Application.Abstractions;
using Common.Application.Messaging;
using Common.Domain.Results;

namespace AuthService.Application.PasswordResets.ResetPassword;

internal sealed class ResetPasswordCommandHandler(
    IPasswordResetTokenRepository tokenRepository,
    IUserRepository userRepository,
    IPasswordResetTokenGenerator tokenGenerator,
    IPasswordHasher passwordHasher,
    IBreachedPasswordChecker breachedPasswordChecker,
    ISessionRevoker sessionRevoker,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<ResetPasswordCommand>
{
    public async Task<Result> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        string tokenHash = tokenGenerator.Hash(command.Token);
        PasswordResetToken? token = await tokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (token is null)
        {
            return Result.Failure(PasswordResetErrors.InvalidOrExpired);
        }

        Result consumption = token.Consume(dateTimeProvider.UtcNow);

        return consumption.IsFailure
            ? consumption
            : await ApplyAsync(token.UserId, command.NewPassword, cancellationToken);
    }

    private async Task<Result> ApplyAsync(Guid userId, string newPassword, CancellationToken cancellationToken)
    {
        User? user = await userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null || !user.CanSignIn)
        {
            return Result.Failure(PasswordResetErrors.InvalidOrExpired);
        }

        if (await breachedPasswordChecker.IsBreachedAsync(newPassword, cancellationToken))
        {
            return Result.Failure(UserErrors.PasswordBreached);
        }

        Result changed = user.ChangePassword(passwordHasher.Hash(newPassword));

        if (changed.IsFailure)
        {
            return changed;
        }

        await sessionRevoker.RevokeAllAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
