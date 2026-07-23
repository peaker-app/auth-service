using AuthService.Application.Abstractions;
using Isopoh.Cryptography.Argon2;

namespace AuthService.Infrastructure.Security;

internal sealed class Argon2idPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => Argon2.Hash(password);

    public bool Verify(string password, string? passwordHash) =>
        !string.IsNullOrEmpty(passwordHash) && Argon2.Verify(passwordHash, password);
}
