namespace CustomerService.Application.Abstractions.Notifications;
public interface IOtpService
{
    string GenerateCode();
    (string Hash, string Salt) Hash(string code);
    bool Verify(string code, string hash, string salt);
}
