namespace AuthService.Domain.Users;

public sealed class UserRoleAssignment
{
    private UserRoleAssignment()
    {
    }

    private UserRoleAssignment(UserRole role) => Role = role;

    public UserRole Role { get; private set; }

    internal static UserRoleAssignment Of(UserRole role) => new(role);
}
