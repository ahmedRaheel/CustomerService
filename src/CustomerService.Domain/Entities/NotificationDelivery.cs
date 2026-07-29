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

    public static NotificationDelivery Create(
        Guid registrationId,
        Guid? otpChallengeId,
        NotificationChannel channel,
        string destination,
        string templateCode)
    {
        var utcNow = DateTime.UtcNow;

        return new NotificationDelivery
        {
            Id = Guid.NewGuid(),
            RegistrationId = registrationId,
            OtpChallengeId = otpChallengeId,
            Channel = channel,
            Destination = destination,
            TemplateCode = templateCode,
            Status = DeliveryStatus.Sending,
            AttemptCount = 1,
            CreatedUtc = utcNow,
            UpdatedUtc = utcNow
        };
    }

    public void MarkSent(string? providerMessageId)
    {
        var utcNow = DateTime.UtcNow;

        ProviderMessageId = providerMessageId;
        Status = DeliveryStatus.Sent;
        SentUtc = utcNow;
        UpdatedUtc = utcNow;
        FailureReason = null;
    }

    public void MarkFailed(string reason)
    {
        Status = DeliveryStatus.Failed;
        FailureReason = reason;
        UpdatedUtc = DateTime.UtcNow;
    }
}
