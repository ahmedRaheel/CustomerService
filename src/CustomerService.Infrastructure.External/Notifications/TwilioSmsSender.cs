using System.Net.Http.Headers;
using System.Text;
using CustomerService.Application.Abstractions.Notifications;
using CustomerService.Infrastructure.External.Options;
using Microsoft.Extensions.Options;
namespace CustomerService.Infrastructure.External.Notifications;
public sealed class TwilioSmsSender(HttpClient http, IOptionsMonitor<TwilioOptions> options) : ISmsSender
{
    public async Task<string?> SendAsync(string to, string body, CancellationToken ct)
    {
        var o = options.CurrentValue;
        if (string.IsNullOrWhiteSpace(o.AccountSid) || string.IsNullOrWhiteSpace(o.AuthToken))
            throw new InvalidOperationException("Twilio configuration is incomplete.");

        var data = new Dictionary<string, string>{
              {"To",to},
              {"Body",body}
        };
        if (!string.IsNullOrWhiteSpace(o.MessagingServiceSid))
        {
            data["MessagingServiceSid"] = o.MessagingServiceSid;
           
        }
        else 
        {
            data["From"] = o.FromNumber;
        }           

        using var req = new HttpRequestMessage(HttpMethod.Post, $"https://api.twilio.com/2010-04-01/Accounts/{o.AccountSid}/Messages.json")
        {
            Content = new FormUrlEncodedContent(data)
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{o.AccountSid}:{o.AuthToken}")));
        using var res = await http.SendAsync(req, ct); var text = await res.Content.ReadAsStringAsync(ct); if (!res.IsSuccessStatusCode) throw new InvalidOperationException($"Twilio error {(int)res.StatusCode}: {text}");
        using var doc = System.Text.Json.JsonDocument.Parse(text); return doc.RootElement.TryGetProperty("sid", out var sid) ? sid.GetString() : null;
    }
}
