using AuthService.Application.Abstractions;
using AuthService.Domain.Users;
using Common.Application.Abstractions;
using Common.Application.Messaging;
using Common.Domain.Results;

namespace AuthService.Application.Users.RegisterUser;

internal sealed class RegisterUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IBreachedPasswordChecker breachedPasswordChecker,
    IEmailPseudonymizer emailPseudonymizer,
    ITermsPolicy termsPolicy,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork) : ICommandHandler<RegisterUserCommand>
{
    public async Task<Result> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        Result<Credentials> credentials = Credentials.Create(command.Email, command.Username);

        if (credentials.IsFailure)
        {
            return Result.Failure(credentials.Error);
        }

        if (await userRepository.ExistsByUsernameAsync(credentials.Value.Username, cancellationToken))
        {
            return Result.Failure(UserErrors.UsernameAlreadyRegistered);
        }

        return await breachedPasswordChecker.IsBreachedAsync(command.Password, cancellationToken)
            ? Result.Failure(UserErrors.PasswordBreached)
            : await AcceptAsync(credentials.Value, command.Password, cancellationToken);
    }

    private async Task<Result> AcceptAsync(
        Credentials credentials,
        string password,
        CancellationToken cancellationToken)
    {
        if (await IsEmailTakenAsync(credentials.Email, cancellationToken))
        {
            return Result.Failure(UserErrors.EmailAlreadyRegistered);
        }

        Result<User> user = User.Register(BuildDraft(credentials, password));

        return user.IsFailure
            ? Result.Failure(user.Error)
            : await PersistAsync(user.Value, cancellationToken);
    }

    private UserDraft BuildDraft(Credentials credentials, string password) => new(
        credentials.Email,
        credentials.Username,
        passwordHasher.Hash(password),
        TermsAcceptance.Of(termsPolicy.CurrentVersion, dateTimeProvider.UtcNow));

    private async Task<bool> IsEmailTakenAsync(Email email, CancellationToken cancellationToken) =>
        await userRepository.ExistsByEmailAsync(email, cancellationToken)
        || await userRepository.ExistsByEmailAsync(emailPseudonymizer.Pseudonymize(email), cancellationToken);

    private async Task<Result> PersistAsync(User user, CancellationToken cancellationToken)
    {
        userRepository.Add(user);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicateCredentialException exception)
        {
            return exception.Field is CredentialField.Username
                ? Result.Failure(UserErrors.UsernameAlreadyRegistered)
                : Result.Failure(UserErrors.EmailAlreadyRegistered);
        }

        return Result.Success();
    }
}
