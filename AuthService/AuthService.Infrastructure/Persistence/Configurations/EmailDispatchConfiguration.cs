using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthService.Infrastructure.Persistence.Configurations;

internal sealed class EmailDispatchConfiguration : IEntityTypeConfiguration<EmailDispatch>
{
    private const int HashLength = 64;

    public void Configure(EntityTypeBuilder<EmailDispatch> builder)
    {
        builder.ToTable("email_dispatch_log");

        builder.HasKey(dispatch => dispatch.Id);

        builder.Property(dispatch => dispatch.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(dispatch => dispatch.RecipientHash)
            .HasColumnName("recipient_hash")
            .HasMaxLength(HashLength)
            .IsRequired();

        builder.Property(dispatch => dispatch.SentAtUtc).HasColumnName("sent_at_utc").IsRequired();

        builder.HasIndex(dispatch => new { dispatch.RecipientHash, dispatch.SentAtUtc })
            .HasDatabaseName("ix_email_dispatch_recipient_sent");

        builder.HasIndex(dispatch => dispatch.SentAtUtc)
            .HasDatabaseName("ix_email_dispatch_sent");
    }
}
