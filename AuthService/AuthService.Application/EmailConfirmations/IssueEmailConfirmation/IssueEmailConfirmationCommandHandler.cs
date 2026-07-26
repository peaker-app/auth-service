using AuthService.Domain.Users;
using Common.Application.Messaging;
using Common.Domain.Results;

namespace AuthService.Application.EmailConfirmations.IssueEmailConfirmation;

internal sealed class IssueEmailConfirmationCommandHandler(
    IUserRepository userRepository,
    IEmailConfirmationIssuer emailConfirmationIssuer) : ICommandHandler<IssueEmailConfirmationCommand>
{
    public async Task<Result> Handle(IssueEmailConfirmationCommand command, CancellationToken cancellationToken)
    {
        User? user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound(command.UserId));
        }

        if (user.EmailConfirmed || user.IsDeleted)
        {
            return Result.Success();
        }

        return await emailConfirmationIssuer.IssueAsync(user, cancellationToken);
    }
}
