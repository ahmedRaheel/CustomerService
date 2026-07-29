namespace CustomerService.Domain.Entities;

public sealed class OtpChallenge : BaseEntity
{
    private OtpChallenge()
    {
    }

    public Guid RegistrationId { get; private set; }

    public OtpChannel Channel { get; private set; }

    public string CodeHash { get; private set; } = string.Empty;

    public string Salt { get; private set; } = string.Empty;

    public DateTime ExpiresUtc { get; private set; }

    public int AttemptCount { get; private set; }

    public int MaxAttempts { get; private set; }

    public DateTime? VerifiedUtc { get; private set; }

    public DateTime? InvalidatedUtc { get; private set; }

    public DateTime CreatedUtc { get; private set; }

    public DateTime NextResendAllowedUtc { get; private set; }

    public bool CanVerify =>
        VerifiedUtc is null
        && InvalidatedUtc is null
        && DateTime.UtcNow <= ExpiresUtc
        && AttemptCount < MaxAttempts;

    public static OtpChallenge Create(
        Guid registrationId,
        OtpChannel channel,
        string hash,
        string salt,
        int expiryMinutes,
        int maxAttempts,
        int cooldownSeconds)
    {
        var utcNow = DateTime.UtcNow;

        return new OtpChallenge
        {
            Id = Guid.NewGuid(),
            RegistrationId = registrationId,
            Channel = channel,
            CodeHash = hash,
            Salt = salt,
            ExpiresUtc = utcNow.AddMinutes(expiryMinutes),
            MaxAttempts = maxAttempts,
            CreatedUtc = utcNow,
            NextResendAllowedUtc = utcNow.AddSeconds(cooldownSeconds)
        };
    }

    public void RecordFailedAttempt()
    {
        AttemptCount++;
    }

    public void MarkVerified()
    {
        VerifiedUtc = DateTime.UtcNow;
    }

    public void Invalidate()
    {
        InvalidatedUtc = DateTime.UtcNow;
    }
}
