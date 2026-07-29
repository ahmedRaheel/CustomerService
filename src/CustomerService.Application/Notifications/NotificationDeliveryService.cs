using CustomerService.Application.Abstractions.Notifications;
using CustomerService.Application.Abstractions.Persistence;
using CustomerService.Domain.Entities;

namespace CustomerService.Application.Notifications;

public sealed class NotificationDeliveryService(IRegistrationRepository repository, IEmailSender emailSender, ISmsSender smsSender) : INotificationDeliveryService
{
    public async Task<NotificationDelivery> SendOtpAsync(RegistrationApplication registration, OtpChallenge challenge, NotificationTemplate template, string code, CancellationToken cancellationToken)
    {
        var channel = challenge.Channel == OtpChannel.Email ? NotificationChannel.Email : NotificationChannel.Sms;
        var destination = challenge.Channel == OtpChannel.Email ? registration.Email : registration.MobileNumber;
        var delivery = NotificationDelivery.Create(registration.Id, challenge.Id, channel, destination, template.Code);
        await repository.AddDeliveryAsync(delivery, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        string Render(string value) => value.Replace("{{OtpCode}}", code).Replace("{{FullName}}", registration.FullName ?? "Customer").Replace("{{ExpiryMinutes}}", "10");
        try
        {
            var providerId = challenge.Channel == OtpChannel.Email
                ? await emailSender.SendAsync(destination, Render(template.SubjectTemplate ?? template.Name), Render(template.BodyTemplate), template.IsHtml, cancellationToken)
                : await smsSender.SendAsync(destination, Render(template.BodyTemplate), cancellationToken);
            delivery.MarkSent(providerId);
            return delivery;
        }
        catch (Exception exception)
        {
            delivery.MarkFailed(exception.Message);
            throw;
        }
        finally
        {
            await repository.SaveChangesAsync(cancellationToken);
        }
    }
}
