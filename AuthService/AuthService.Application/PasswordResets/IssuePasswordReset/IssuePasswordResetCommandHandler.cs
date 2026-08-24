using AuthService.Domain.PasswordResets;
using AuthService.Domain.Users;
using Common.Application.Messaging;
using Common.Domain.Results;

namespace AuthService.Application.PasswordResets.IssuePasswordReset;

internal sealed class IssuePasswordResetCommandHandler(
    IUserRepository userRepository,
    IPasswordResetIssuer passwordResetIssuer) : ICommandHandler<IssuePasswordResetCommand>
{
    public async Task<Result> Handle(IssuePasswordResetCommand command, CancellationToken cancellationToken)
    {
        User? user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null || !user.CanSignIn)
        {
            return Result.Success();
        }

        Result issued = await passwordResetIssuer.IssueAsync(user, cancellationToken);

        return issued.IsFailure && issued.Error == PasswordResetErrors.RecipientQuotaExceeded
            ? Result.Success()
            : issued;
    }
}
