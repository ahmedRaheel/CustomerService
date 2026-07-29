namespace CustomerService.Domain.Constants;

public static class ResultMessages
{
    public const string RegistrationNotFound = "Registration not found.";
    public const string RegistrationStarted = "Registration started.";
    public const string RegistrationRetrieved = "Registration retrieved.";
    public const string RegistrationCompleted = "Registration completed.";
    public const string RegistrationCancelled = "Registration cancelled.";
    public const string ProfileUpdated = "Profile updated.";
    public const string PinConfigured = "PIN configured.";
    public const string TermsAccepted = "Terms accepted.";
    public const string TermsRetrieved = "Terms retrieved.";
    public const string TermRetrieved = "Term retrieved.";
    public const string TermDocumentNotFound = "Term document not found.";
    public const string DeliveriesRetrieved = "Deliveries retrieved.";
    public const string EmailOtpSent = "Email OTP sent.";
    public const string SmsOtpSent = "SMS OTP sent.";
    public const string EmailVerified = "Email verified.";
    public const string MobileVerified = "Mobile verified.";
    public const string OtpNotFound = "OTP not found.";
    public const string NotificationTemplateNotFound = "Notification template not found.";
    public const string OtpHourlyLimitReached = "OTP hourly limit reached.";
    public const string InvalidOrExpiredOtp = "Invalid or expired OTP.";
    public const string DuplicateRegistration = "An active or completed registration already exists for the supplied identity.";
    public const string RequiredTermsNotAccepted = "Required terms are not accepted.";
    public const string AllRequiredTermsMustBeAccepted = "All required terms must be accepted.";
    public const string InvalidOrInactiveTerm = "An invalid or inactive term was supplied.";
    public const string InvalidOtpAttempt = "Invalid, expired, used or locked OTP.";
}
