namespace CustomerService.Application.Abstractions.Notifications;
public interface IEmailSender { Task<string?> SendAsync(string to, string subject, string body, bool isHtml, CancellationToken ct); }
public interface ISmsSender { Task<string?> SendAsync(string to, string body, CancellationToken ct); }
