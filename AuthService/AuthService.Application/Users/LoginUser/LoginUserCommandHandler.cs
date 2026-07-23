using AuthService.Application.Abstractions;
using AuthService.Application.Authentication;
using AuthService.Domain.Users;
using Common.Application.Abstractions;
using Common.Application.Messaging;
using Common.Domain.Results;

namespace AuthService.Application.Users.LoginUser;

internal sealed class LoginUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IAuthTokenIssuer tokenIssuer,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<LoginUserCommand, AuthTokensResponse>
{
    public async Task<Result<AuthTokensResponse>> Handle(LoginUserCommand command, CancellationToken cancellationToken)
    {
        Result<Email> email = Email.Create(command.Email);
        if (email.IsFailure) return Result.Failure<AuthTokensResponse>(UserErrors.InvalidCredentials);

        User? user = await userRepository.GetByEmailAsync(email.Value, cancellationToken);
        DateTime utcNow = dateTimeProvider.UtcNow;

        if (user is null || user.IsLockedOut(utcNow) || !passwordHasher.Verify(command.Password, user.PasswordHash))
            return await FailAsync(user, utcNow, cancellationToken);

        user.RecordSuccessfulLogin();
        IssuedTokens issued = tokenIssuer.Issue(user, utcNow, command.IpAddress);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return issued.Response;
    }

    private async Task<Result<AuthTokensResponse>> FailAsync(
        User? user,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (user is not null)
        {
            user.RecordFailedLogin(utcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Failure<AuthTokensResponse>(UserErrors.InvalidCredentials);
    }
}
