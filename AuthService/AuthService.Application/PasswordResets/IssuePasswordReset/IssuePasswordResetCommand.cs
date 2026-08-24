using Common.Application.Messaging;

namespace AuthService.Application.PasswordResets.IssuePasswordReset;

public sealed record IssuePasswordResetCommand(Guid UserId) : ICommand;
