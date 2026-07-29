using CustomerService.Domain.Entities;

namespace CustomerService.Application.Abstractions.Persistence;

public interface IRegistrationRepository
{
    Task<RegistrationApplication?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(RegistrationApplication registration, CancellationToken cancellationToken);
    Task<OtpChallenge?> GetLatestOtpAsync(Guid registrationId, OtpChannel channel, CancellationToken cancellationToken);
    Task AddOtpAsync(OtpChallenge challenge, CancellationToken cancellationToken);
    Task<NotificationTemplate?> GetTemplateAsync(string code, NotificationChannel channel, CancellationToken cancellationToken);
    Task AddNotificationDeliveryAsync(NotificationDelivery delivery, CancellationToken cancellationToken);
    Task<IReadOnlyList<NotificationDelivery>> GetNotificationDeliveriesAsync(Guid registrationId, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
