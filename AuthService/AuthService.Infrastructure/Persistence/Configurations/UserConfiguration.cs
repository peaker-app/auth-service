using AuthService.Domain.Users;
using Common.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthService.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : EntityConfiguration<User>
{
    public override void Configure(EntityTypeBuilder<User> builder)
    {
        base.Configure(builder);

        builder.ToTable("users");

        builder.Property(user => user.Email)
            .HasColumnName("email")
            .HasMaxLength(Email.MaxLength)
            .HasConversion(email => email.Value, value => Email.Create(value).Value)
            .IsRequired();

        builder.HasIndex(user => user.Email).IsUnique().HasDatabaseName("ux_users_email");

        builder.Property(user => user.PasswordHash).HasColumnName("password_hash").HasMaxLength(255);
        builder.Property(user => user.EmailConfirmed).HasColumnName("email_confirmed").IsRequired();

        builder.Property(user => user.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(user => user.FailedLoginCount).HasColumnName("failed_login_count").IsRequired();
        builder.Property(user => user.LockedUntilUtc).HasColumnName("locked_until_utc");
    }
}
