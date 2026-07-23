using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AuthService.Infrastructure.Persistence;

internal sealed class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    private const string DesignTimeConnectionString =
        "server=localhost;port=3307;database=peaker_auth;user=peaker;password=peaker";

    public AuthDbContext CreateDbContext(string[] args)
    {
        DbContextOptions<AuthDbContext> options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseMySQL(DesignTimeConnectionString)
            .Options;

        return new AuthDbContext(options);
    }
}
