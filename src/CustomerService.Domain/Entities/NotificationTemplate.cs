namespace CustomerService.Domain.Entities;

public sealed class NotificationTemplate : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; }
    public string? SubjectTemplate { get; set; }
    public string BodyTemplate { get; set; } = string.Empty;
    public bool IsHtml { get; set; }
    public bool IsActive { get; set; }
    public int Version { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}
public enum NotificationChannel { Email = 1, Sms = 2 }
