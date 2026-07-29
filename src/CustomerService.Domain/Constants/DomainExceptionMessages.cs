namespace CustomerService.Domain.Constants;

public static class DomainExceptionMessages
{
    public const string EmailAndSmsVerificationRequired = "Email and SMS must be verified before setting the PIN.";
    public const string BothOtpsVerificationRequired = "Both OTPs must be verified.";
    public const string PinConfigurationRequired = "A six-digit PIN must be configured.";
    public const string RequiredTermsAcceptanceRequired = "Required terms must be accepted.";
    public const string CompletedRegistrationCannotBeCancelled = "A completed registration cannot be cancelled.";
    public const string RegistrationNotActive = "Registration is not active.";
    public const string RegistrationExpired = "Registration has expired.";
}
