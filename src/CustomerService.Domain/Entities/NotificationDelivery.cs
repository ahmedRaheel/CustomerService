namespace CustomerService.Domain.Entities;

public sealed class NotificationDelivery
{
    private NotificationDelivery() { }

    public Guid Id { get; private set; }
    public Guid RegistrationId { get; private set; }
    public Guid OtpChallengeId { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public string Destination { get; private set; } = string.Empty;
    public string TemplateCode { get; private set; } = string.Empty;
    public NotificationDeliveryStatus Status { get; private set; }
    public int AttemptCount { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime? SentUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }

    public static NotificationDelivery Create(
        Guid registrationId,
        Guid otpChallengeId,
        NotificationChannel channel,
        string destination,
        string templateCode)
        => new()
        {
            Id = Guid.NewGuid(),
            RegistrationId = registrationId,
            OtpChallengeId = otpChallengeId,
            Channel = channel,
            Destination = destination.Trim(),
            TemplateCode = templateCode,
            Status = NotificationDeliveryStatus.Pending,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };

    public void MarkSending()
    {
        AttemptCount++;
        Status = NotificationDeliveryStatus.Sending;
        FailureReason = null;
        UpdatedUtc = DateTime.UtcNow;
    }

    public void MarkSent(string? providerMessageId)
    {
        Status = NotificationDeliveryStatus.Sent;
        ProviderMessageId = providerMessageId;
        SentUtc = DateTime.UtcNow;
        UpdatedUtc = DateTime.UtcNow;
    }

    public void MarkFailed(string reason)
    {
        Status = NotificationDeliveryStatus.Failed;
        FailureReason = string.IsNullOrWhiteSpace(reason) ? "Notification provider failed." : reason[..Math.Min(reason.Length, 2000)];
        UpdatedUtc = DateTime.UtcNow;
    }
}

public enum NotificationDeliveryStatus
{
    Pending = 1,
    Sending = 2,
    Sent = 3,
    Failed = 4
}
