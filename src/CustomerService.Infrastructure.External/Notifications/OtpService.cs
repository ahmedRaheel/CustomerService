using System.Security.Cryptography;
using System.Text;
using CustomerService.Application.Abstractions.Notifications;

namespace CustomerService.Infrastructure.External.Notifications;

public sealed class OtpService : IOtpService
{
    public string GenerateCode() => RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

    public (string Hash, string Salt) Hash(string code)
    {
        var salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        return (Compute(code, salt), salt);
    }

    public bool Verify(string code, string hash, string salt)
    {
        var computed = Convert.FromBase64String(Compute(code, salt));
        var expected = Convert.FromBase64String(hash);
        return CryptographicOperations.FixedTimeEquals(computed, expected);
    }

    private static string Compute(string code, string salt)
    {
        using var hmac = new HMACSHA256(Convert.FromBase64String(salt));
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(code)));
    }
}
