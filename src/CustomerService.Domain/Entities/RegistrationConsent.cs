namespace CustomerService.Domain.Entities;

public sealed class RegistrationConsent : BaseEntity
{
    public Guid RegistrationId { get; set; }
    public Guid TermDocumentId { get; set; }
    public string TermVersion { get; set; } = string.Empty;
    public bool Accepted { get; set; }
    public DateTime AcceptedUtc { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
