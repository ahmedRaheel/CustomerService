namespace CustomerService.Domain.Entities;

public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public Guid? RegistrationId { get; set; }
    public NotificationChannel Channel { get; set; }
    public string Destination { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = string.Empty;
    public string VariablesJson { get; set; } = "{}";
    public OutboxStatus Status { get; set; }
    public int AttemptCount { get; set; }
    public int MaxAttempts { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? NextAttemptUtc { get; set; }
    public DateTime? ProcessedUtc { get; set; }
    public string? ProviderMessageId { get; set; }
    public string? LastError { get; set; }
}
public enum OutboxStatus { Pending = 1, Processing = 2, Sent = 3, Failed = 4, DeadLetter = 5 }
