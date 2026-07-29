namespace CustomerService.Domain.Entities;

public sealed class OtpVerificationAttempt : BaseEntity
{
    public Guid OtpChallengeId { get; set; }

    public bool WasSuccessful { get; set; }

    public string? FailureReason { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTime SubmittedUtc { get; set; }
}
