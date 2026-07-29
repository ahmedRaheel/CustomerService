namespace CustomerService.Domain.Enums;

public enum RegistrationStep
{
    Started = 1,
    EmailOtpSent = 2,
    EmailVerified = 3,
    SmsOtpSent = 4,
    SmsVerified = 5,
    ProfileCompleted = 6,
    TermsAccepted = 7,
    PinConfigured = 8,
    Completed = 9
}
