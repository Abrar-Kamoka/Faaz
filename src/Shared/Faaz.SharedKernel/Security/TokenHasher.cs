using System.Security.Cryptography;
using System.Text;

namespace Faaz.SharedKernel.Security;

/// <summary>Single source of truth for opaque token hashing across all services.</summary>
public static class TokenHasher
{
    public static string Hash(string plaintext)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plaintext));
        // Base64Url encode (RFC 4648): replace + with -, / with _, strip trailing =
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
