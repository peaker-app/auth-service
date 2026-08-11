using AuthService.Domain.Users.Events;
using Common.Domain.Abstractions;
using Common.Domain.Results;

namespace AuthService.Domain.Users;

public sealed class User : AggregateRoot
{
    private readonly List<UserRoleAssignment> _roles = [];

    private User()
    {
    }

    private User(Guid id, UserDraft draft) : base(id)
    {
        Email = draft.Email;
        Username = draft.Username;
        PasswordHash = draft.PasswordHash;
        AcceptedTerms = draft.AcceptedTerms;
        Status = UserStatus.Active;
    }

    public Email Email { get; private set; } = null!;

    public Username Username { get; private set; } = null!;

    public string? PasswordHash { get; private set; }

    public bool EmailConfirmed { get; private set; }

    public UserStatus Status { get; private set; }

    public TermsAcceptance AcceptedTerms { get; private set; } = null!;

    public IReadOnlyCollection<UserRole> Roles => [.. _roles.Select(assignment => assignment.Role)];

    public bool IsDeleted => Status is UserStatus.Deleted;

    public bool IsLocked => Status is UserStatus.Locked;

    public bool CanSignIn => !IsDeleted && !IsLocked;

    public static Result<User> Register(UserDraft draft)
    {
        if (string.IsNullOrWhiteSpace(draft.PasswordHash))
        {
            return UserErrors.PasswordHashMissing;
        }

        if (string.IsNullOrWhiteSpace(draft.AcceptedTerms.Version))
        {
            return UserErrors.TermsNotAccepted;
        }

        User user = new(Guid.CreateVersion7(), draft);
        user.Raise(new UserRegisteredDomainEvent(user.Id, draft.Email.Value, draft.Username.Value));
        user.Raise(new EmailConfirmationRequestedDomainEvent(user.Id));

        return user;
    }

    public void RecordDuplicateRegistrationAttempt()
    {
        if (IsDeleted)
        {
            return;
        }

        Raise(new DuplicateRegistrationAttemptedDomainEvent(Id));
    }

    public Result ConfirmEmail()
    {
        if (EmailConfirmed)
        {
            return Result.Failure(UserErrors.EmailAlreadyConfirmed);
        }

        EmailConfirmed = true;
        Raise(new UserEmailConfirmedDomainEvent(Id));

        return Result.Success();
    }

    public Result RequestPasswordReset()
    {
        if (!CanSignIn)
        {
            return Result.Failure(UserErrors.CannotSignIn);
        }

        Raise(new PasswordResetRequestedDomainEvent(Id));

        return Result.Success();
    }

    public Result ChangePassword(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return Result.Failure(UserErrors.PasswordHashMissing);
        }

        if (!CanSignIn)
        {
            return Result.Failure(UserErrors.CannotSignIn);
        }

        PasswordHash = passwordHash;

        return Result.Success();
    }

    public Result Delete(Email pseudonymizedEmail)
    {
        if (IsDeleted)
        {
            return Result.Failure(UserErrors.AlreadyDeleted);
        }

        Status = UserStatus.Deleted;
        Email = pseudonymizedEmail;
        Raise(new UserDeletedDomainEvent(Id));

        return Result.Success();
    }

    public bool IsInRole(UserRole role) => _roles.Exists(assignment => assignment.Role == role);

    public Result Grant(UserRole role)
    {
        if (IsDeleted)
        {
            return Result.Failure(UserErrors.AlreadyDeleted);
        }

        if (IsInRole(role))
        {
            return Result.Failure(UserErrors.RoleAlreadyGranted);
        }

        _roles.Add(UserRoleAssignment.Of(role));

        return Result.Success();
    }

    public Result Revoke(UserRole role)
    {
        int removed = _roles.RemoveAll(assignment => assignment.Role == role);

        return removed is 0
            ? Result.Failure(UserErrors.RoleNotGranted)
            : Result.Success();
    }

    public Result Lock()
    {
        if (IsDeleted)
        {
            return Result.Failure(UserErrors.AlreadyDeleted);
        }

        if (IsLocked)
        {
            return Result.Failure(UserErrors.AlreadyLocked);
        }

        Status = UserStatus.Locked;

        return Result.Success();
    }

    public Result Unlock()
    {
        if (IsDeleted)
        {
            return Result.Failure(UserErrors.AlreadyDeleted);
        }

        if (!IsLocked)
        {
            return Result.Failure(UserErrors.NotLocked);
        }

        Status = UserStatus.Active;

        return Result.Success();
    }
}
