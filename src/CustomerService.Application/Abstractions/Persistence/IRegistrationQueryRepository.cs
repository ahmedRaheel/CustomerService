using CustomerService.Domain.Entities;

namespace CustomerService.Application.Abstractions.Persistence;

public interface IRegistrationQueryRepository
{
    Task<RegistrationApplication?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> HasActiveDuplicateAsync(string email, string mobileNumber, string? nationalId, CancellationToken cancellationToken);
    Task<OtpChallenge?> GetLatestOtpAsync(Guid registrationId, OtpChannel channel, CancellationToken cancellationToken);
    Task<int> CountOtpsSinceAsync(Guid registrationId, OtpChannel channel, DateTime sinceUtc, CancellationToken cancellationToken);
    Task<NotificationTemplate?> GetTemplateAsync(string code, NotificationChannel channel, CancellationToken cancellationToken);
    Task<IReadOnlyList<NotificationDelivery>> GetDeliveriesAsync(Guid registrationId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TermDocument>> GetActiveTermsAsync(CancellationToken cancellationToken);
    Task<TermDocument?> GetTermAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> HasAcceptedRequiredTermsAsync(Guid registrationId, CancellationToken cancellationToken);
}
