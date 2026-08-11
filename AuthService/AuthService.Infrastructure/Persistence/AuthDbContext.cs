using AuthService.Domain.EmailConfirmations;
using AuthService.Domain.PasswordResets;
using AuthService.Domain.RefreshTokens;
using AuthService.Domain.Users;
using AuthService.Infrastructure.Persistence.Configurations;
using Common.Application.Abstractions;
using Common.Infrastructure.Persistence.Idempotency;
using Common.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Persistence;

public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<EmailConfirmationToken> EmailConfirmationTokens => Set<EmailConfirmationToken>();

    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new ProcessedMessageConfiguration());
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.Properties<Guid>().HaveColumnType("char(36)");

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (TryResolveDuplicate(exception, out CredentialField field))
        {
            throw new DuplicateCredentialException(field, exception);
        }
    }

    private static bool TryResolveDuplicate(DbUpdateException exception, out CredentialField field)
    {
        string message = exception.InnerException?.Message ?? string.Empty;

        if (message.Contains(UserConfiguration.EmailIndexName, StringComparison.Ordinal))
        {
            field = CredentialField.Email;
            return true;
        }

        field = CredentialField.Username;

        return message.Contains(UserConfiguration.UsernameIndexName, StringComparison.Ordinal);
    }
}
