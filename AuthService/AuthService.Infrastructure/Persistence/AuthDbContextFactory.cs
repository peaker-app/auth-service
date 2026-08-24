using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AuthService.Infrastructure.Persistence;

internal sealed class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    private const string ConnectionStringVariable = "ConnectionStrings__AuthDatabase";

    private const string ModelOnlyConnectionString =
        "server=localhost;port=3307;database=peaker_auth";

    public AuthDbContext CreateDbContext(string[] args)
    {
        DbContextOptions<AuthDbContext> options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseMySQL(ResolveConnectionString())
            .Options;

        return new AuthDbContext(options);
    }

    private static string ResolveConnectionString()
    {
        string? configured = Environment.GetEnvironmentVariable(ConnectionStringVariable);

        return string.IsNullOrWhiteSpace(configured)
            ? ModelOnlyConnectionString
            : configured;
    }
}
