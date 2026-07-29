namespace CustomerService.Domain.Entities;

public sealed class OtpChallenge
{
    private OtpChallenge() { }
    public Guid Id { get; private set; }
    public Guid RegistrationId { get; private set; }
    public OtpChannel Channel { get; private set; }
    public string CodeHash { get; private set; } = string.Empty;
    public string Salt { get; private set; } = string.Empty;
    public DateTime ExpiresUtc { get; private set; }
    public int AttemptCount { get; private set; }
    public int MaxAttempts { get; private set; }
    public DateTime? VerifiedUtc { get; private set; }
    public DateTime CreatedUtc { get; private set; }

    public static OtpChallenge Create(Guid registrationId, OtpChannel channel, string codeHash, string salt, int expiryMinutes, int maxAttempts)
        => new() { Id=Guid.NewGuid(), RegistrationId=registrationId, Channel=channel, CodeHash=codeHash, Salt=salt,
            ExpiresUtc=DateTime.UtcNow.AddMinutes(expiryMinutes), MaxAttempts=maxAttempts, CreatedUtc=DateTime.UtcNow };

    public bool CanVerify => VerifiedUtc is null && DateTime.UtcNow <= ExpiresUtc && AttemptCount < MaxAttempts;
    public void RecordFailedAttempt() => AttemptCount++;
    public void MarkVerified() => VerifiedUtc = DateTime.UtcNow;
}
public enum OtpChannel { Email = 1, Sms = 2 }
