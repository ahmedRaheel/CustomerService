using System.Security.Cryptography;
using CustomerService.Application.Abstractions.Notifications;

namespace CustomerService.Infrastructure.External.Notifications;

public sealed class PinHasher : IPinHasher
{
    private const int Iterations = 210_000;
    private const int SaltSize = 32;
    private const int KeySize = 32;

    public (string Hash, string Salt) Hash(string pin)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(pin, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public bool Verify(string pin, string hash, string salt)
    {
        var expected = Convert.FromBase64String(hash);
        var computed = Rfc2898DeriveBytes.Pbkdf2(
            pin,
            Convert.FromBase64String(salt),
            Iterations,
            HashAlgorithmName.SHA256,
            KeySize);

        return CryptographicOperations.FixedTimeEquals(computed, expected);
    }
}
