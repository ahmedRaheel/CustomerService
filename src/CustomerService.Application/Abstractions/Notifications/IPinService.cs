namespace CustomerService.Application.Abstractions.Notifications;
public interface IPinService { (string Hash,string Salt) Hash(string pin); bool Verify(string pin,string hash,string salt); }
