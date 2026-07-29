namespace CustomerService.Application.Abstractions.Notifications;

public interface IPinHasher
{
    (string Hash, string Salt) Hash(string pin);
    bool Verify(string pin, string hash, string salt);
}
