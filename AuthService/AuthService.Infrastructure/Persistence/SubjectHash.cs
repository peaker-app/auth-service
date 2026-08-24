using System.Security.Cryptography;
using System.Text;

namespace AuthService.Infrastructure.Persistence;

public static class SubjectHash
{
    public const int HexLength = 64;

    public static string Of(string subject) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(subject.ToUpperInvariant())));
}
