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

        if (await userRepository.ExistsByEmailAsync(email.Value, cancellationToken))
            return Result.Failure<Guid>(UserErrors.EmailAlreadyRegistered);

        if (await breachedPasswordChecker.IsBreachedAsync(command.Password, cancellationToken))
            return Result.Failure<Guid>(UserErrors.PasswordBreached);

        Result<User> user = User.Register(email.Value, passwordHasher.Hash(command.Password));
        if (user.IsFailure) return Result.Failure<Guid>(user.Error);

        userRepository.Add(user.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return user.Value.Id;
    }
}
