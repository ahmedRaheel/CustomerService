using CustomerService.Domain.Entities;
namespace CustomerService.Application.Abstractions.Persistence;
public interface IRegistrationRepository
{
 Task<RegistrationApplication?> GetAsync(Guid id,CancellationToken ct); Task AddAsync(RegistrationApplication entity,CancellationToken ct);
 Task<bool> HasActiveDuplicateAsync(string email,string mobile,string? nationalId,CancellationToken ct);
 Task<OtpChallenge?> GetLatestOtpAsync(Guid id,OtpChannel channel,CancellationToken ct); Task AddOtpAsync(OtpChallenge entity,CancellationToken ct);
 Task InvalidateActiveOtpsAsync(Guid id,OtpChannel channel,CancellationToken ct); Task<int> CountOtpsSinceAsync(Guid id,OtpChannel channel,DateTime sinceUtc,CancellationToken ct);
 Task<NotificationTemplate?> GetTemplateAsync(string code,NotificationChannel channel,CancellationToken ct); Task AddDeliveryAsync(NotificationDelivery entity,CancellationToken ct);
 Task<IReadOnlyList<NotificationDelivery>> GetDeliveriesAsync(Guid id,CancellationToken ct); Task AddVerificationAttemptAsync(OtpVerificationAttempt entity,CancellationToken ct);
 Task<IReadOnlyList<TermDocument>> GetActiveTermsAsync(CancellationToken ct); Task<TermDocument?> GetTermAsync(Guid id,CancellationToken ct);
 Task<bool> HasAcceptedRequiredTermsAsync(Guid id,CancellationToken ct); Task AddConsentAsync(RegistrationConsent entity,CancellationToken ct);
 Task AddStepAsync(Guid registrationId,RegistrationStep step,string status,CancellationToken ct); Task<CustomerAccount?> GetCustomerByLegacyIdAsync(string legacyId,CancellationToken ct);
 Task AddCustomerAsync(CustomerAccount customer,CancellationToken ct); Task<int> SaveChangesAsync(CancellationToken ct);
}
