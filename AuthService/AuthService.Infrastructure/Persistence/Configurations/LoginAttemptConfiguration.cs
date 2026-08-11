using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthService.Infrastructure.Persistence.Configurations;

internal sealed class LoginAttemptConfiguration : IEntityTypeConfiguration<LoginAttempt>
{
    public void Configure(EntityTypeBuilder<LoginAttempt> builder)
    {
        builder.ToTable("login_attempts");

        builder.HasKey(attempt => attempt.Id);

        builder.Property(attempt => attempt.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(attempt => attempt.IdentifierHash)
            .HasColumnName("identifier_hash")
            .HasMaxLength(SubjectHash.HexLength)
            .IsRequired();

        builder.Property(attempt => attempt.IpHash)
            .HasColumnName("ip_hash")
            .HasMaxLength(SubjectHash.HexLength)
            .IsRequired();

        builder.Property(attempt => attempt.AttemptedAtUtc).HasColumnName("attempted_at_utc").IsRequired();

        builder.HasIndex(attempt => new { attempt.IdentifierHash, attempt.IpHash, attempt.AttemptedAtUtc })
            .HasDatabaseName("ix_login_attempts_identifier_ip_time");
    }
}
