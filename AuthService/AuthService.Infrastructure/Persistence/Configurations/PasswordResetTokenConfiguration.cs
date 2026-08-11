using AuthService.Domain.PasswordResets;
using Common.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthService.Infrastructure.Persistence.Configurations;

internal sealed class PasswordResetTokenConfiguration : EntityConfiguration<PasswordResetToken>
{
    public override void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        base.Configure(builder);

        builder.ToTable("password_reset_tokens");

        builder.Property(token => token.UserId).HasColumnName("user_id").IsRequired();

        builder.Property(token => token.TokenHash).HasColumnName("token_hash").HasMaxLength(255).IsRequired();
        builder.HasIndex(token => token.TokenHash)
            .IsUnique()
            .HasDatabaseName("ux_password_reset_tokens_token_hash");

        builder.Property(token => token.IssuedAtUtc).HasColumnName("issued_at_utc").IsRequired();
        builder.Property(token => token.ExpiresAtUtc).HasColumnName("expires_at_utc").IsRequired();
        builder.Property(token => token.ConsumedAtUtc).HasColumnName("consumed_at_utc");

        builder.HasIndex(token => new { token.UserId, token.IssuedAtUtc })
            .HasDatabaseName("ix_password_reset_tokens_user_issued");
    }
}
