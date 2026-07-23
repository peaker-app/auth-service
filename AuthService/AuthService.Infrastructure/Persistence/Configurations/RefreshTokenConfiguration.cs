using AuthService.Domain.RefreshTokens;
using Common.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthService.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : EntityConfiguration<RefreshToken>
{
    public override void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        base.Configure(builder);

        builder.ToTable("refresh_tokens");

        builder.Property(token => token.UserId).HasColumnName("user_id").IsRequired();

        builder.Property(token => token.TokenHash).HasColumnName("token_hash").HasMaxLength(255).IsRequired();
        builder.HasIndex(token => token.TokenHash).IsUnique().HasDatabaseName("ux_refresh_tokens_token_hash");

        builder.Property(token => token.ExpiresAtUtc).HasColumnName("expires_at_utc").IsRequired();
        builder.Property(token => token.RevokedAtUtc).HasColumnName("revoked_at_utc");
        builder.Property(token => token.ReplacedById).HasColumnName("replaced_by_id");
        builder.Property(token => token.CreatedByIp).HasColumnName("created_by_ip").HasMaxLength(45);

        builder.HasIndex(token => token.UserId).HasDatabaseName("ix_refresh_tokens_user_id");
    }
}
