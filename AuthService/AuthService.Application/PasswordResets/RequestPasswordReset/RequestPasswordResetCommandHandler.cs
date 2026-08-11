using AuthService.Domain.Users;
using Common.Application.Abstractions;
using Common.Application.Messaging;
using Common.Domain.Results;

namespace AuthService.Application.PasswordResets.RequestPasswordReset;

internal sealed class RequestPasswordResetCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<RequestPasswordResetCommand>
{
    public async Task<Result> Handle(RequestPasswordResetCommand command, CancellationToken cancellationToken)
    {
        Result<Email> email = Email.Create(command.Email);

        if (email.IsFailure)
        {
            return Result.Failure(email.Error);
        }

        User? user = await userRepository.GetByEmailAsync(email.Value, cancellationToken);

        if (user is null || !user.CanSignIn)
        {
            return Result.Success();
        }

        Result requested = user.RequestPasswordReset();

        if (requested.IsSuccess)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
