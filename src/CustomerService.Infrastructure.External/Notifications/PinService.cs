using System.Security.Cryptography;
using CustomerService.Application.Abstractions.Notifications;

namespace CustomerService.Infrastructure.External.Notifications;

public sealed class PinService : IPinService
{
    private const int SaltSize = 32;
    private const int HashSize = 32;
    private const int Iterations = 210000;

    public (string Hash, string Salt) Hash(string pin)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            pin,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return (
            Convert.ToBase64String(hash),
            Convert.ToBase64String(salt));
    }

    public bool Verify(string pin, string hash, string salt)
    {
        var storedHash = Convert.FromBase64String(hash);
        var storedSalt = Convert.FromBase64String(salt);
        var calculatedHash = Rfc2898DeriveBytes.Pbkdf2(
            pin,
            storedSalt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return CryptographicOperations.FixedTimeEquals(
            calculatedHash,
            storedHash);
    }
}
