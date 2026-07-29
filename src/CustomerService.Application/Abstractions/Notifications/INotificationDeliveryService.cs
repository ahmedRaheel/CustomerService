using CustomerService.Domain.Entities;

namespace CustomerService.Application.Abstractions.Notifications;

public interface INotificationDeliveryService
{
    Task<NotificationDelivery> SendOtpAsync(RegistrationApplication registration, OtpChallenge challenge, NotificationTemplate template, string code, CancellationToken cancellationToken);
}
