namespace CustomerService.Domain.Entities;

public sealed class RegistrationApplication : BaseEntity
{
    private RegistrationApplication() { }
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

    public static RegistrationApplication Create(string email,string mobile,RegistrationType type,string? nationalId,string? legacyCustomerId)
    {
        var now=DateTime.UtcNow;
        return new RegistrationApplication { Id=Guid.NewGuid(),Email=email.Trim(),NormalizedEmail=email.Trim().ToUpperInvariant(),
            MobileNumber=mobile.Trim(),NormalizedMobileNumber=NormalizeMobile(mobile),Type=type,NationalId=nationalId?.Trim(),
            LegacyCustomerId=legacyCustomerId?.Trim(),Status=RegistrationStatus.InProgress,CurrentStep=RegistrationStep.Started,
            CreatedUtc=now,UpdatedUtc=now,ExpiresUtc=now.AddDays(7)};
    }
    public void MarkEmailVerified(){EnsureActive();EmailVerified=true;Advance(RegistrationStep.EmailVerified);}
    public void MarkSmsVerified(){EnsureActive();SmsVerified=true;Advance(RegistrationStep.SmsVerified);}
    public void UpdateProfile(string fullName,string? nationalId){EnsureActive();FullName=fullName.Trim();NationalId=nationalId?.Trim();Advance(RegistrationStep.ProfileCompleted);}
    public void SetPin(string hash,string salt){EnsureActive();if(!EmailVerified||!SmsVerified)throw new InvalidOperationException("Email and SMS must be verified before setting the PIN.");PinHash=hash;PinSalt=salt;PinSetUtc=DateTime.UtcNow;Advance(RegistrationStep.PinConfigured);}
    public void MarkTermsAccepted(){EnsureActive();Advance(RegistrationStep.TermsAccepted);}
    public void Complete(){EnsureActive();if(!EmailVerified||!SmsVerified)throw new InvalidOperationException("Both OTPs must be verified.");if(PinSetUtc is null)throw new InvalidOperationException("A six-digit PIN must be configured.");if(CurrentStep<RegistrationStep.TermsAccepted)throw new InvalidOperationException("Required terms must be accepted.");Status=RegistrationStatus.Completed;CurrentStep=RegistrationStep.Completed;UpdatedUtc=DateTime.UtcNow;}
    public void Cancel(string reason){if(Status==RegistrationStatus.Completed)throw new InvalidOperationException("A completed registration cannot be cancelled.");Status=RegistrationStatus.Cancelled;CancellationReason=reason.Trim();CancelledUtc=DateTime.UtcNow;UpdatedUtc=DateTime.UtcNow;}
    private void EnsureActive(){if(Status!=RegistrationStatus.InProgress)throw new InvalidOperationException("Registration is not active.");if(DateTime.UtcNow>ExpiresUtc){Status=RegistrationStatus.Expired;throw new InvalidOperationException("Registration has expired.");}}
    private void Advance(RegistrationStep step){if(step>CurrentStep)CurrentStep=step;UpdatedUtc=DateTime.UtcNow;}
    private static string NormalizeMobile(string value){var d=new string(value.Where(char.IsDigit).ToArray());return value.TrimStart().StartsWith('+')?"+"+d:d;}
}
public enum RegistrationType { NewCustomer=1, ExistingCustomerMigration=2 }
public enum RegistrationStatus { InProgress=1, Completed=2, Cancelled=3, Expired=4 }
public enum RegistrationStep { Started=1,EmailOtpSent=2,EmailVerified=3,SmsOtpSent=4,SmsVerified=5,ProfileCompleted=6,TermsAccepted=7,PinConfigured=8,Completed=9 }
