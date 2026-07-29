namespace CustomerService.Domain.Entities;

public sealed class RegistrationApplication
{
    private RegistrationApplication() { }

    public Guid Id { get; private set; }
    public RegistrationType Type { get; private set; }
    public RegistrationStatus Status { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string MobileNumber { get; private set; } = string.Empty;
    public string? NationalId { get; private set; }
    public string? FullName { get; private set; }
    public bool EmailVerified { get; private set; }
    public bool SmsVerified { get; private set; }
    public string? PinHash { get; private set; }
    public string? PinSalt { get; private set; }
    public DateTime? PinSetUtc { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public bool HasPin => !string.IsNullOrWhiteSpace(PinHash) && !string.IsNullOrWhiteSpace(PinSalt);

    public static RegistrationApplication Create(string email, string mobileNumber, RegistrationType type, string? nationalId)
        => new()
        {
            Id = Guid.NewGuid(),
            Email = email.Trim().ToLowerInvariant(),
            MobileNumber = mobileNumber.Trim(),
            Type = type,
            NationalId = nationalId?.Trim(),
            Status = RegistrationStatus.PendingVerification,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };

    public void MarkEmailVerified()
    {
        EmailVerified = true;
        RefreshVerificationStatus();
    }

    public void MarkSmsVerified()
    {
        SmsVerified = true;
        RefreshVerificationStatus();
    }

    public void UpdateProfile(string fullName, string? nationalId)
    {
        FullName = fullName.Trim();
        NationalId = nationalId?.Trim();
        UpdatedUtc = DateTime.UtcNow;
    }

    public void SetPin(string pinHash, string pinSalt)
    {
        if (!EmailVerified || !SmsVerified)
            throw new InvalidOperationException("Both email and SMS OTPs must be verified before setting the PIN.");

        PinHash = pinHash;
        PinSalt = pinSalt;
        PinSetUtc = DateTime.UtcNow;
        UpdatedUtc = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (!EmailVerified || !SmsVerified)
            throw new InvalidOperationException("Both email and SMS OTPs must be verified.");
        if (!HasPin)
            throw new InvalidOperationException("A six-digit PIN must be set before registration can be completed.");

        Status = RegistrationStatus.Completed;
        UpdatedUtc = DateTime.UtcNow;
    }

    private void RefreshVerificationStatus()
    {
        if (EmailVerified && SmsVerified && Status == RegistrationStatus.PendingVerification)
            Status = RegistrationStatus.Verified;

        UpdatedUtc = DateTime.UtcNow;
    }
}

public enum RegistrationType { NewCustomer = 1, ExistingCustomerMigration = 2 }
public enum RegistrationStatus { PendingVerification = 1, Verified = 2, Completed = 3, Cancelled = 4 }
