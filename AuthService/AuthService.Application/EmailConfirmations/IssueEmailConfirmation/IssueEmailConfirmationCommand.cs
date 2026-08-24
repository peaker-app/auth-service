using Common.Application.Messaging;

namespace AuthService.Application.EmailConfirmations.IssueEmailConfirmation;

public sealed record IssueEmailConfirmationCommand(Guid UserId) : ICommand;
