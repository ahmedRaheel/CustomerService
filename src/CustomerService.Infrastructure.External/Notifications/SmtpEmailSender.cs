using System.Net;
using System.Net.Mail;
using CustomerService.Application.Abstractions.Notifications;
using CustomerService.Infrastructure.External.Options;
using Microsoft.Extensions.Options;
namespace CustomerService.Infrastructure.External.Notifications;
public sealed class SmtpEmailSender(IOptionsMonitor<EmailOptions> options):IEmailSender
{
    public async Task<string?> SendAsync(string to, string subject, string body, bool isHtml, CancellationToken ct)
    {
        var o = options.CurrentValue;
        using var message = new MailMessage
        {
            From = new MailAddress(o.FromAddress, o.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = isHtml
        };
        message.To.Add(to);
        using var client = new SmtpClient(o.Host, o.Port)
        {
            EnableSsl = o.UseSsl,
            Credentials = new NetworkCredential(o.UserName, o.Password)
        };
        await client.SendMailAsync(message, ct); return null;
    }
}
