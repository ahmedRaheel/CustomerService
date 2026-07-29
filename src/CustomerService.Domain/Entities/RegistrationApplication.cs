namespace CustomerService.Domain.Entities;

public sealed class RegistrationApplication : BaseEntity
{
    private RegistrationApplication()
    {
    }

    public RegistrationType Type { get; private set; }

    public RegistrationStatus Status { get; private set; }

    public RegistrationStep CurrentStep { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public string NormalizedEmail { get; private set; } = string.Empty;

    public string MobileNumber { get; private set; } = string.Empty;

    public string NormalizedMobileNumber { get; private set; } = string.Empty;

    public string? NationalId { get; private set; }

    public string? FullName { get; private set; }

    public string? LegacyCustomerId { get; private set; }

    public bool EmailVerified { get; private set; }

    public bool SmsVerified { get; private set; }

    public string? PinHash { get; private set; }

    public string? PinSalt { get; private set; }

    public DateTime? PinSetUtc { get; private set; }

    public int FailedPinAttempts { get; private set; }

    public DateTime? PinLockedUntilUtc { get; private set; }

    public DateTime ExpiresUtc { get; private set; }

    public DateTime? CancelledUtc { get; private set; }

    public string? CancellationReason { get; private set; }

    public DateTime CreatedUtc { get; private set; }

    public DateTime UpdatedUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public static RegistrationApplication Create(
        string email,
        string mobileNumber,
        RegistrationType type,
        string? nationalId,
        string? legacyCustomerId)
    {
        var utcNow = DateTime.UtcNow;

        return new RegistrationApplication
        {
            Id = Guid.NewGuid(),
            Email = email.Trim(),
            NormalizedEmail = email.Trim().ToUpperInvariant(),
            MobileNumber = mobileNumber.Trim(),
            NormalizedMobileNumber = NormalizeMobileNumber(mobileNumber),
            Type = type,
            NationalId = nationalId?.Trim(),
            LegacyCustomerId = legacyCustomerId?.Trim(),
            Status = RegistrationStatus.InProgress,
            CurrentStep = RegistrationStep.Started,
            CreatedUtc = utcNow,
            UpdatedUtc = utcNow,
            ExpiresUtc = utcNow.AddDays(DomainConstants.RegistrationExpiryDays)
        };
    }

    public void MarkEmailVerified()
    {
        EnsureActive();
        EmailVerified = true;
        Advance(RegistrationStep.EmailVerified);
    }

    public void MarkSmsVerified()
    {
        EnsureActive();
        SmsVerified = true;
        Advance(RegistrationStep.SmsVerified);
    }

    public void UpdateProfile(string fullName, string? nationalId)
    {
        EnsureActive();
        FullName = fullName.Trim();
        NationalId = nationalId?.Trim();
        Advance(RegistrationStep.ProfileCompleted);
    }

    public void SetPin(string hash, string salt)
    {
        EnsureActive();

        if (!EmailVerified || !SmsVerified)
        {
            throw new InvalidOperationException(
                DomainExceptionMessages.EmailAndSmsVerificationRequired);
        }

        PinHash = hash;
        PinSalt = salt;
        PinSetUtc = DateTime.UtcNow;
        Advance(RegistrationStep.PinConfigured);
    }

    public void MarkTermsAccepted()
    {
        EnsureActive();
        Advance(RegistrationStep.TermsAccepted);
    }

    public void Complete()
    {
        EnsureActive();

        if (!EmailVerified || !SmsVerified)
        {
            throw new InvalidOperationException(
                DomainExceptionMessages.BothOtpsVerificationRequired);
        }

        if (PinSetUtc is null)
        {
            throw new InvalidOperationException(
                DomainExceptionMessages.PinConfigurationRequired);
        }

        if (CurrentStep < RegistrationStep.TermsAccepted)
        {
            throw new InvalidOperationException(
                DomainExceptionMessages.RequiredTermsAcceptanceRequired);
        }

        Status = RegistrationStatus.Completed;
        CurrentStep = RegistrationStep.Completed;
        UpdatedUtc = DateTime.UtcNow;
    }

    public void Cancel(string reason)
    {
        if (Status == RegistrationStatus.Completed)
        {
            throw new InvalidOperationException(
                DomainExceptionMessages.CompletedRegistrationCannotBeCancelled);
        }

        Status = RegistrationStatus.Cancelled;
        CancellationReason = reason.Trim();
        CancelledUtc = DateTime.UtcNow;
        UpdatedUtc = DateTime.UtcNow;
    }

    private void EnsureActive()
    {
        if (Status != RegistrationStatus.InProgress)
        {
            throw new InvalidOperationException(
                DomainExceptionMessages.RegistrationNotActive);
        }

        if (DateTime.UtcNow <= ExpiresUtc)
        {
            return;
        }

        Status = RegistrationStatus.Expired;

        throw new InvalidOperationException(
            DomainExceptionMessages.RegistrationExpired);
    }

    private void Advance(RegistrationStep step)
    {
        if (step > CurrentStep)
        {
            CurrentStep = step;
        }

        UpdatedUtc = DateTime.UtcNow;
    }

    private static string NormalizeMobileNumber(string value)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());

        return value.TrimStart().StartsWith('+')
            ? $"+{digits}"
            : digits;
    }
}
