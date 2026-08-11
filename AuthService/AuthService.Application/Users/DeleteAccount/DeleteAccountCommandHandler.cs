using AuthService.Application.Abstractions;
using AuthService.Application.Authentication;
using AuthService.Domain.Users;
using Common.Application.Abstractions;
using Common.Application.Messaging;
using Common.Domain.Results;

namespace AuthService.Application.Users.DeleteAccount;

internal sealed class DeleteAccountCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IEmailPseudonymizer emailPseudonymizer,
    ISessionRevoker sessionRevoker,
    IUnitOfWork unitOfWork) : ICommandHandler<DeleteAccountCommand>
{
    public async Task<Result> Handle(DeleteAccountCommand command, CancellationToken cancellationToken)
    {
        User? user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound(command.UserId));
        }

        return passwordHasher.Verify(command.Password, user.PasswordHash)
            ? await DeleteAsync(user, cancellationToken)
            : Result.Failure(UserErrors.InvalidCredentials);
    }

    private async Task<Result> DeleteAsync(User user, CancellationToken cancellationToken)
    {
        await sessionRevoker.RevokeAllAsync(user, cancellationToken);

        Result deletion = user.Delete(emailPseudonymizer.Pseudonymize(user.Email));

        if (deletion.IsFailure)
        {
            return deletion;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
