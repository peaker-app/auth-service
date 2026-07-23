using AuthService.Domain.Users.Events;
using Common.Domain.Abstractions;
using Common.Domain.Results;

namespace AuthService.Domain.Users;

public sealed class User : AggregateRoot
{
    public const int MaxFailedAttempts = 5;

    private static readonly TimeSpan BaseLockout = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan MaxLockout = TimeSpan.FromMinutes(30);

    private User()
    {
    }

    private User(Guid id, Email email, string passwordHash) : base(id)
    {
        Email = email;
        PasswordHash = passwordHash;
        Status = UserStatus.Active;
    }

    public Email Email { get; private set; } = null!;

    public string? PasswordHash { get; private set; }

    public bool EmailConfirmed { get; private set; }

    public UserStatus Status { get; private set; }

    public int FailedLoginCount { get; private set; }

    public DateTime? LockedUntilUtc { get; private set; }

    public static Result<User> Register(Email email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return UserErrors.PasswordHashMissing;
        }

        User user = new(Guid.CreateVersion7(), email, passwordHash);
        user.Raise(new UserRegisteredDomainEvent(user.Id, email.Value));

        return user;
    }

    public bool IsLockedOut(DateTime utcNow) => LockedUntilUtc is not null && LockedUntilUtc > utcNow;

    public void RecordFailedLogin(DateTime utcNow)
    {
        FailedLoginCount++;

        if (FailedLoginCount % MaxFailedAttempts != 0)
        {
            return;
        }

        LockedUntilUtc = utcNow.Add(ComputeLockoutDuration());
        Status = UserStatus.Locked;
    }

    public void RecordSuccessfulLogin()
    {
        FailedLoginCount = 0;
        LockedUntilUtc = null;

        if (Status is UserStatus.Locked)
        {
            Status = UserStatus.Active;
        }
    }

    private TimeSpan ComputeLockoutDuration()
    {
        int lockoutLevel = FailedLoginCount / MaxFailedAttempts;
        double minutes = BaseLockout.TotalMinutes * Math.Pow(2, lockoutLevel - 1);

        return TimeSpan.FromMinutes(Math.Min(minutes, MaxLockout.TotalMinutes));
    }
}
