namespace CustomerService.Domain.Entities;

public sealed class NotificationDelivery : BaseEntity
{
    public Guid RegistrationId { get; set; }
    public Guid? OtpChallengeId { get; set; }
    public NotificationChannel Channel { get; set; }
    public string Destination { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = string.Empty;
    public DeliveryStatus Status { get; private set; }
    public int AttemptCount { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime? SentUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }

    public static NotificationDelivery Create(Guid registrationId, Guid? otpChallengeId, NotificationChannel channel, string destination, string templateCode)
    {
        var now = DateTime.UtcNow;
        return new NotificationDelivery
        {
            Id = Guid.NewGuid(), RegistrationId = registrationId, OtpChallengeId = otpChallengeId, Channel = channel,
            Destination = destination, TemplateCode = templateCode, Status = DeliveryStatus.Sending, AttemptCount = 1,
            CreatedUtc = now, UpdatedUtc = now
        };
    }

    public void MarkSent(string? providerMessageId)
    {
        ProviderMessageId = providerMessageId; Status = DeliveryStatus.Sent; SentUtc = DateTime.UtcNow; UpdatedUtc = DateTime.UtcNow; FailureReason = null;
    }

    public void MarkFailed(string reason)
    {
        Status = DeliveryStatus.Failed; FailureReason = reason; UpdatedUtc = DateTime.UtcNow;
    }
}

public enum DeliveryStatus { Pending = 1, Sending = 2, Sent = 3, Failed = 4, Delivered = 5, Undelivered = 6 }
