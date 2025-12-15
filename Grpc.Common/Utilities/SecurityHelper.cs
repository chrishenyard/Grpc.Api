using System.Security.Cryptography;
using System.Text;

namespace Grpc.Common.Utilities;

public static class SecurityHelper
{
    private const int KeySize = 256 / 8;

    public static string ComputeHmacSha256(string secret, string data)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(secret, nameof(secret));
        ArgumentNullException.ThrowIfNullOrEmpty(data, nameof(data));

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string ComputeSalt()
    {
        var salt = RandomNumberGenerator.GetBytes(KeySize);
        return Convert.ToBase64String(salt);
    }

    public static string ComputeSecretHash(string password, string salt, int iterations = 210_000)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(password, nameof(password));
        ArgumentNullException.ThrowIfNullOrEmpty(salt, nameof(salt));
        if (iterations <= 0)
            throw new ArgumentOutOfRangeException(nameof(iterations), "Iterations must be a positive integer.");

        var passwordBytes = Encoding.UTF8.GetBytes(password);
        var saltBytes = Convert.FromBase64String(salt);
        var hash = Rfc2898DeriveBytes.Pbkdf2(passwordBytes, saltBytes, iterations, HashAlgorithmName.SHA256, KeySize);
        return Convert.ToBase64String(hash);
    }
}
