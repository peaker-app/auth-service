using AuthService.Domain.Users;
using Common.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthService.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : EntityConfiguration<User>
{
    public const string EmailIndexName = "ux_users_email";
    public const string UsernameIndexName = "ux_users_username";

    private const string RolesNavigation = "_roles";
    private const string UserForeignKey = "user_id";

    public override void Configure(EntityTypeBuilder<User> builder)
    {
        base.Configure(builder);

        builder.ToTable("users");

        builder.Property(user => user.Email)
            .HasColumnName("email")
            .HasMaxLength(Email.MaxLength)
            .HasConversion(email => email.Value, value => Email.Create(value).Value)
            .IsRequired();

        builder.HasIndex(user => user.Email).IsUnique().HasDatabaseName(EmailIndexName);

        builder.Property(user => user.Username)
            .HasColumnName("username")
            .HasMaxLength(Username.MaxLength)
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasConversion(username => username.Value, value => Username.Create(value).Value)
            .IsRequired();

        builder.HasIndex(user => user.Username).IsUnique().HasDatabaseName(UsernameIndexName);

        builder.Property(user => user.PasswordHash).HasColumnName("password_hash").HasMaxLength(255);
        builder.Property(user => user.EmailConfirmed).HasColumnName("email_confirmed").IsRequired();

        builder.Property(user => user.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasConversion<string>()
            .IsRequired();

        builder.ComplexProperty(user => user.AcceptedTerms, terms =>
        {
            terms.Property(acceptance => acceptance.Version)
                .HasColumnName("terms_version")
                .HasMaxLength(TermsAcceptance.MaxVersionLength)
                .IsRequired();

            terms.Property(acceptance => acceptance.AcceptedAtUtc)
                .HasColumnName("terms_accepted_at_utc")
                .IsRequired();
        });

        ConfigureRoles(builder);
    }

    private static void ConfigureRoles(EntityTypeBuilder<User> builder) =>
        builder.OwnsMany<UserRoleAssignment>(RolesNavigation, assignment =>
        {
            assignment.ToTable("user_roles");
            assignment.WithOwner().HasForeignKey(UserForeignKey);

            assignment.Property(entity => entity.Role)
                .HasColumnName("role")
                .HasMaxLength(20)
                .HasConversion<string>()
                .IsRequired();

            assignment.HasKey(UserForeignKey, nameof(UserRoleAssignment.Role));
        });
}
