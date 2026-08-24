using AuthService.Application.Abstractions;
using Isopoh.Cryptography.Argon2;

namespace AuthService.Infrastructure.Security;

internal sealed class Argon2IdPasswordHasher : IPasswordHasher
{
    private static readonly Lazy<string> DecoyHash =
        new(() => Argon2.Hash(Guid.CreateVersion7().ToString()));

    public string Hash(string password) => Argon2.Hash(password);

    public bool Verify(string password, string? passwordHash)
    {
        if (!string.IsNullOrEmpty(passwordHash))
        {
            return Argon2.Verify(passwordHash, password);
        }

        // Motivo: sin hash almacenado hay que gastar el mismo tiempo que en una verificación real.
        // Si no, la latencia distingue "el identificador no existe" de "la contraseña es incorrecta"
        // y permite enumerar correos y usernames, que es lo que DESIGN.md §4.3 prohíbe revelar.
        _ = Argon2.Verify(DecoyHash.Value, password);

        return false;
    }
}
