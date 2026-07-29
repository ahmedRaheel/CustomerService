using CustomerService.Application.Abstractions.Notifications;
using CustomerService.Application.Abstractions.Persistence;
using CustomerService.Domain.Entities;

namespace CustomerService.Application.Notifications;

public sealed class NotificationDeliveryService(
    IRegistrationCommandRepository commandRepository,
    IEmailSender emailSender,
    ISmsSender smsSender) : INotificationDeliveryService
{
    public async Task<NotificationDelivery> SendOtpAsync(
        RegistrationApplication registration,
        OtpChallenge challenge,
        NotificationTemplate template,
        string code,
        CancellationToken ct)
    {
        var channel = challenge.Channel == OtpChannel.Email
            ? NotificationChannel.Email
            : NotificationChannel.Sms;
        var destination = challenge.Channel == OtpChannel.Email
            ? registration.Email
            : registration.MobileNumber;
        var delivery = NotificationDelivery.Create(
            registration.Id,
            challenge.Id,
            channel,
            destination,
            template.Code);

        await commandRepository.AddDeliveryAsync(delivery, ct);
        await commandRepository.SaveChangesAsync(ct);

        try
        {
            var providerId = challenge.Channel == OtpChannel.Email
                ? await emailSender.SendAsync(
                    destination,
                    Render(template.SubjectTemplate ?? template.Name, registration, code),
                    Render(template.BodyTemplate, registration, code),
                    template.IsHtml,
                    ct)
                : await smsSender.SendAsync(
                    destination,
                    Render(template.BodyTemplate, registration, code),
                    ct);

            delivery.MarkSent(providerId);
            return delivery;
        }
        catch (Exception ex)
        {
            delivery.MarkFailed(ex.Message);
            throw;
        }
        finally
        {
            await commandRepository.SaveChangesAsync(ct);
        }
    }

    private static string Render(
        string template,
        RegistrationApplication registration,
        string code)
    {
        return template
            .Replace("{{OtpCode}}", code)
            .Replace("{{FullName}}", registration.FullName ?? "Customer")
            .Replace("{{ExpiryMinutes}}", "10");
    }
}
