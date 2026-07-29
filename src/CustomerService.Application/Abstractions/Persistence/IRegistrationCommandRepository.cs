using CustomerService.Domain.Entities;

namespace CustomerService.Application.Abstractions.Persistence;

public interface IRegistrationCommandRepository
{
    Task AddAsync(RegistrationApplication registration, CancellationToken cancellationToken);
    Task AddOtpAsync(OtpChallenge otpChallenge, CancellationToken cancellationToken);
    Task InvalidateActiveOtpsAsync(Guid registrationId, OtpChannel channel, CancellationToken cancellationToken);
    Task AddDeliveryAsync(NotificationDelivery notificationDelivery, CancellationToken cancellationToken);
    Task AddVerificationAttemptAsync(OtpVerificationAttempt verificationAttempt, CancellationToken cancellationToken);
    Task AddConsentAsync(RegistrationConsent consent, CancellationToken cancellationToken);
    Task AddStepAsync(Guid registrationId, RegistrationStep step, string status, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
