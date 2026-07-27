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
    private const char EmailSeparator = '@';

    public async Task<Result<AuthTokensResponse>> Handle(LoginUserCommand command, CancellationToken cancellationToken)
    {
        User? user = await ResolveUserAsync(command.Identifier, cancellationToken);
        DateTime utcNow = dateTimeProvider.UtcNow;

        // Motivo: la verificación se ejecuta siempre, incluso sin usuario o con la cuenta bloqueada,
        // para que el fallo tarde lo mismo en los tres casos (DESIGN.md §4.3).
        bool passwordMatches = passwordHasher.Verify(command.Password, user?.PasswordHash);

        if (user is null || !passwordMatches || !user.CanSignIn(utcNow))
            return await FailAsync(user, utcNow, cancellationToken);

        user.RecordSuccessfulLogin();
        IssuedTokens issued = tokenIssuer.Issue(user, utcNow, command.IpAddress);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return issued.Response;
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
