namespace CustomerService.Infrastructure.External.Options;
public sealed class EmailOptions
{
    public const string SectionName="Email"; 
    public string Provider{get;set;}="Smtp";
    public string Host{get;set;}="smtp-mail.outlook.com"; 
    public int Port{get;set;}=587;
    public bool UseSsl{get;set;}=true; 
    public string FromAddress{get;set;}="raheelahmad@msn.com"; 
    public string FromName{get;set;}="Customer Service";
    public string UserName{get;set;}="raheelahmad@msn.com"; 
    public string Password{get;set;}="";
}

public sealed class SmsOptions 
{
    public const string SectionName="Sms"; 
    public string Provider{get;set;}="Twilio"; 
}
public sealed class TwilioOptions
{
    public const string SectionName = "Twilio";
    public string AccountSid { get; set; } = "";
    public string AuthToken { get; set; } = "";
    public string FromNumber { get; set; } = "";
    public string? MessagingServiceSid { get; set; }
}
