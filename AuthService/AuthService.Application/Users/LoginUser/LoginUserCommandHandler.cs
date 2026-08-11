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
    ILoginThrottle loginThrottle,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<LoginUserCommand, AuthTokensResponse>
{
    private const char EmailSeparator = '@';

    public async Task<Result<AuthTokensResponse>> Handle(LoginUserCommand command, CancellationToken cancellationToken)
    {
        LoginThrottleVerdict verdict = await loginThrottle.EvaluateAsync(
            command.Identifier, command.IpAddress, cancellationToken);

        if (!verdict.IsAllowed)
        {
            return Result.Failure<AuthTokensResponse>(UserErrors.TooManyAttempts);
        }

        User? user = await ResolveUserAsync(command.Identifier, cancellationToken);
        bool passwordMatches = passwordHasher.Verify(command.Password, user?.PasswordHash);

        if (user is null || !passwordMatches || !user.CanSignIn)
        {
            return await FailAsync(command, cancellationToken);
        }

        return await SucceedAsync(user, command, cancellationToken);
    }

    private async Task<User?> ResolveUserAsync(string identifier, CancellationToken cancellationToken)
    {
        if (identifier?.Contains(EmailSeparator, StringComparison.Ordinal) != true)
        {
            Result<Username> username = Username.Create(identifier);

            return username.IsSuccess
                ? await userRepository.GetByUsernameAsync(username.Value, cancellationToken)
                : null;
        }

        Result<Email> email = Email.Create(identifier);

        return email.IsSuccess
            ? await userRepository.GetByEmailAsync(email.Value, cancellationToken)
            : null;
    }

    private async Task<Result<AuthTokensResponse>> FailAsync(
        LoginUserCommand command,
        CancellationToken cancellationToken)
    {
        await loginThrottle.RecordFailureAsync(command.Identifier, command.IpAddress, cancellationToken);

        return Result.Failure<AuthTokensResponse>(UserErrors.InvalidCredentials);
    }

    private async Task<Result<AuthTokensResponse>> SucceedAsync(
        User user,
        LoginUserCommand command,
        CancellationToken cancellationToken)
    {
        IssuedTokens issued = tokenIssuer.Issue(user, dateTimeProvider.UtcNow, command.IpAddress);

        await loginThrottle.ClearAsync(command.Identifier, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return issued.Response;
    }
}
