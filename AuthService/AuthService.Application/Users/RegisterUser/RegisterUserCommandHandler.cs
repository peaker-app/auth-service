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
    IUnitOfWork unitOfWork) : ICommandHandler<RegisterUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        Result<Email> email = Email.Create(command.Email);
        if (email.IsFailure) return Result.Failure<Guid>(email.Error);

        Result<Username> username = Username.Create(command.Username);
        if (username.IsFailure) return Result.Failure<Guid>(username.Error);

        Result availability = await EnsureAvailableAsync(email.Value, username.Value, cancellationToken);
        if (availability.IsFailure) return Result.Failure<Guid>(availability.Error);

        if (await breachedPasswordChecker.IsBreachedAsync(command.Password, cancellationToken))
            return Result.Failure<Guid>(UserErrors.PasswordBreached);

        Result<User> user = User.Register(email.Value, username.Value, passwordHasher.Hash(command.Password));
        if (user.IsFailure) return Result.Failure<Guid>(user.Error);

        userRepository.Add(user.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return user.Value.Id;
    }

    private async Task<Result> EnsureAvailableAsync(
        Email email,
        Username username,
        CancellationToken cancellationToken)
    {
        if (await userRepository.ExistsByEmailAsync(email, cancellationToken))
            return Result.Failure(UserErrors.EmailAlreadyRegistered);

        return await userRepository.ExistsByUsernameAsync(username, cancellationToken)
            ? Result.Failure(UserErrors.UsernameAlreadyRegistered)
            : Result.Success();
    }
}
