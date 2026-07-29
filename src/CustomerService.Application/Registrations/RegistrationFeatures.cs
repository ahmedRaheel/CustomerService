using FluentValidation;
using MediatR;
using CustomerService.Application.Abstractions.Notifications;
using CustomerService.Application.Abstractions.Persistence;
using CustomerService.Domain.Dtos;
using CustomerService.Domain.Entities;
using CustomerService.Domain.Exceptions;

namespace CustomerService.Application.Registrations;

public sealed record StartRegistrationCommand(
    string Email,
    string MobileNumber,
    RegistrationType Type,
    string? NationalId) : IRequest<StartRegistrationResponse>;

public sealed class StartRegistrationValidator : AbstractValidator<StartRegistrationCommand>
{
    public StartRegistrationValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.MobileNumber).NotEmpty().MinimumLength(8).MaximumLength(30);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.NationalId).MaximumLength(100);
    }
}

public sealed class StartRegistrationHandler(IRegistrationRepository repository)
    : IRequestHandler<StartRegistrationCommand, StartRegistrationResponse>
{
    public async Task<StartRegistrationResponse> Handle(StartRegistrationCommand request, CancellationToken cancellationToken)
    {
        var registration = RegistrationApplication.Create(
            request.Email,
            request.MobileNumber,
            request.Type,
            request.NationalId);

        await repository.AddAsync(registration, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return new StartRegistrationResponse(registration.Id, registration.Status);
    }
}

public sealed record SendEmailOtpCommand(Guid RegistrationId) : IRequest;
public sealed record SendSmsOtpCommand(Guid RegistrationId) : IRequest;
public sealed record VerifyBothOtpsCommand(Guid RegistrationId, string EmailOtp, string SmsOtp) : IRequest;
public sealed record UpdateRegistrationProfileCommand(Guid RegistrationId, string FullName, string? NationalId) : IRequest;
public sealed record SetRegistrationPinCommand(Guid RegistrationId, string Pin, string ConfirmPin) : IRequest<SetPinResponse>;
public sealed record CompleteRegistrationCommand(Guid RegistrationId) : IRequest;
public sealed record GetRegistrationQuery(Guid RegistrationId) : IRequest<RegistrationDto>;
public sealed record GetNotificationDeliveriesQuery(Guid RegistrationId) : IRequest<IReadOnlyList<NotificationDeliveryDto>>;

public sealed class VerifyBothOtpsValidator : AbstractValidator<VerifyBothOtpsCommand>
{
    public VerifyBothOtpsValidator()
    {
        RuleFor(x => x.EmailOtp).Matches("^[0-9]{6}$");
        RuleFor(x => x.SmsOtp).Matches("^[0-9]{6}$");
    }
}

public sealed class UpdateRegistrationProfileValidator : AbstractValidator<UpdateRegistrationProfileCommand>
{
    public UpdateRegistrationProfileValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NationalId).MaximumLength(100);
    }
}

public sealed class SetRegistrationPinValidator : AbstractValidator<SetRegistrationPinCommand>
{
    public SetRegistrationPinValidator()
    {
        RuleFor(x => x.Pin)
            .NotEmpty()
            .Matches("^[0-9]{6}$")
            .WithMessage("PIN must contain exactly six digits.");

        RuleFor(x => x.ConfirmPin)
            .Equal(x => x.Pin)
            .WithMessage("PIN and confirmation PIN must match.");
    }
}

internal static class OtpCommandSupport
{
    private const int ExpiryMinutes = 10;
    private const int MaxVerificationAttempts = 5;

    public static async Task SendAsync(
        Guid registrationId,
        OtpChannel otpChannel,
        IRegistrationRepository repository,
        IOtpService otpService,
        IEmailSender emailSender,
        ISmsSender smsSender,
        CancellationToken cancellationToken)
    {
        var registration = await repository.GetAsync(registrationId, cancellationToken)
            ?? throw new NotFoundException($"Registration {registrationId} was not found.");

        var notificationChannel = otpChannel == OtpChannel.Email
            ? NotificationChannel.Email
            : NotificationChannel.Sms;
        var templateCode = otpChannel == OtpChannel.Email
            ? "REGISTRATION_EMAIL_OTP"
            : "REGISTRATION_SMS_OTP";
        var destination = otpChannel == OtpChannel.Email
            ? registration.Email
            : registration.MobileNumber;

        var template = await repository.GetTemplateAsync(templateCode, notificationChannel, cancellationToken)
            ?? throw new InvalidOperationException($"Active notification template '{templateCode}' was not found.");

        var code = otpService.GenerateCode();
        var securedCode = otpService.Hash(code);
        var challenge = OtpChallenge.Create(
            registrationId,
            otpChannel,
            securedCode.Hash,
            securedCode.Salt,
            ExpiryMinutes,
            MaxVerificationAttempts);

        var delivery = NotificationDelivery.Create(
            registrationId,
            challenge.Id,
            notificationChannel,
            destination,
            templateCode);

        await repository.AddOtpAsync(challenge, cancellationToken);
        await repository.AddNotificationDeliveryAsync(delivery, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["OtpCode"] = code,
            ["FullName"] = registration.FullName ?? "Customer",
            ["ExpiryMinutes"] = ExpiryMinutes.ToString()
        };

        var subject = TemplateRenderer.Render(template.SubjectTemplate ?? template.Name, variables);
        var body = TemplateRenderer.Render(template.BodyTemplate, variables);

        delivery.MarkSending();
        await repository.SaveChangesAsync(cancellationToken);

        try
        {
            var providerMessageId = notificationChannel == NotificationChannel.Email
                ? await emailSender.SendAsync(destination, subject, body, template.IsHtml, cancellationToken)
                : await smsSender.SendAsync(destination, body, cancellationToken);

            delivery.MarkSent(providerMessageId);
            await repository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            delivery.MarkFailed(exception.Message);
            await repository.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException($"Failed to send {notificationChannel} OTP.", exception);
        }
    }
}

internal static class TemplateRenderer
{
    public static string Render(string template, IReadOnlyDictionary<string, string> variables)
    {
        var rendered = template;
        foreach (var variable in variables)
            rendered = rendered.Replace($"{{{{{variable.Key}}}}}", variable.Value, StringComparison.OrdinalIgnoreCase);

        return rendered;
    }
}

public sealed class SendEmailOtpHandler(
    IRegistrationRepository repository,
    IOtpService otpService,
    IEmailSender emailSender,
    ISmsSender smsSender)
    : IRequestHandler<SendEmailOtpCommand>
{
    public Task Handle(SendEmailOtpCommand request, CancellationToken cancellationToken)
        => OtpCommandSupport.SendAsync(
            request.RegistrationId,
            OtpChannel.Email,
            repository,
            otpService,
            emailSender,
            smsSender,
            cancellationToken);
}

public sealed class SendSmsOtpHandler(
    IRegistrationRepository repository,
    IOtpService otpService,
    IEmailSender emailSender,
    ISmsSender smsSender)
    : IRequestHandler<SendSmsOtpCommand>
{
    public Task Handle(SendSmsOtpCommand request, CancellationToken cancellationToken)
        => OtpCommandSupport.SendAsync(
            request.RegistrationId,
            OtpChannel.Sms,
            repository,
            otpService,
            emailSender,
            smsSender,
            cancellationToken);
}

public sealed class VerifyBothOtpsHandler(IRegistrationRepository repository, IOtpService otpService)
    : IRequestHandler<VerifyBothOtpsCommand>
{
    public async Task Handle(VerifyBothOtpsCommand request, CancellationToken cancellationToken)
    {
        var registration = await repository.GetAsync(request.RegistrationId, cancellationToken)
            ?? throw new NotFoundException($"Registration {request.RegistrationId} was not found.");

        var emailOtp = await repository.GetLatestOtpAsync(request.RegistrationId, OtpChannel.Email, cancellationToken)
            ?? throw new InvalidOperationException("Email OTP was not found.");
        var smsOtp = await repository.GetLatestOtpAsync(request.RegistrationId, OtpChannel.Sms, cancellationToken)
            ?? throw new InvalidOperationException("SMS OTP was not found.");

        var emailIsValid = IsValid(emailOtp, request.EmailOtp, otpService);
        var smsIsValid = IsValid(smsOtp, request.SmsOtp, otpService);

        if (!emailIsValid)
            emailOtp.RecordFailedAttempt();
        if (!smsIsValid)
            smsOtp.RecordFailedAttempt();

        if (!emailIsValid || !smsIsValid)
        {
            await repository.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException(BuildOtpError(emailIsValid, smsIsValid));
        }

        emailOtp.MarkVerified();
        smsOtp.MarkVerified();
        registration.MarkEmailVerified();
        registration.MarkSmsVerified();

        await repository.SaveChangesAsync(cancellationToken);
    }

    private static bool IsValid(OtpChallenge challenge, string code, IOtpService otpService)
        => challenge.CanVerify && otpService.Verify(code, challenge.CodeHash, challenge.Salt);

    private static string BuildOtpError(bool emailIsValid, bool smsIsValid)
    {
        if (!emailIsValid && !smsIsValid)
            return "Invalid or expired email and SMS OTPs.";
        return emailIsValid ? "Invalid or expired SMS OTP." : "Invalid or expired email OTP.";
    }
}

public sealed class UpdateRegistrationProfileHandler(IRegistrationRepository repository)
    : IRequestHandler<UpdateRegistrationProfileCommand>
{
    public async Task Handle(UpdateRegistrationProfileCommand request, CancellationToken cancellationToken)
    {
        var registration = await repository.GetAsync(request.RegistrationId, cancellationToken)
            ?? throw new NotFoundException($"Registration {request.RegistrationId} was not found.");

        registration.UpdateProfile(request.FullName, request.NationalId);
        await repository.SaveChangesAsync(cancellationToken);
    }
}

public sealed class SetRegistrationPinHandler(IRegistrationRepository repository, IPinHasher pinHasher)
    : IRequestHandler<SetRegistrationPinCommand, SetPinResponse>
{
    public async Task<SetPinResponse> Handle(SetRegistrationPinCommand request, CancellationToken cancellationToken)
    {
        var registration = await repository.GetAsync(request.RegistrationId, cancellationToken)
            ?? throw new NotFoundException($"Registration {request.RegistrationId} was not found.");

        var securedPin = pinHasher.Hash(request.Pin);
        registration.SetPin(securedPin.Hash, securedPin.Salt);
        await repository.SaveChangesAsync(cancellationToken);

        return new SetPinResponse(registration.Id, registration.HasPin, registration.PinSetUtc!.Value);
    }
}

public sealed class CompleteRegistrationHandler(IRegistrationRepository repository)
    : IRequestHandler<CompleteRegistrationCommand>
{
    public async Task Handle(CompleteRegistrationCommand request, CancellationToken cancellationToken)
    {
        var registration = await repository.GetAsync(request.RegistrationId, cancellationToken)
            ?? throw new NotFoundException($"Registration {request.RegistrationId} was not found.");

        registration.Complete();
        await repository.SaveChangesAsync(cancellationToken);
    }
}

public sealed class GetRegistrationHandler(IRegistrationRepository repository)
    : IRequestHandler<GetRegistrationQuery, RegistrationDto>
{
    public async Task<RegistrationDto> Handle(GetRegistrationQuery request, CancellationToken cancellationToken)
    {
        var registration = await repository.GetAsync(request.RegistrationId, cancellationToken)
            ?? throw new NotFoundException($"Registration {request.RegistrationId} was not found.");

        return new RegistrationDto(
            registration.Id,
            registration.Type,
            registration.Status,
            registration.Email,
            registration.MobileNumber,
            registration.NationalId,
            registration.FullName,
            registration.EmailVerified,
            registration.SmsVerified,
            registration.HasPin,
            registration.PinSetUtc,
            registration.CreatedUtc,
            registration.UpdatedUtc);
    }
}

public sealed class GetNotificationDeliveriesHandler(IRegistrationRepository repository)
    : IRequestHandler<GetNotificationDeliveriesQuery, IReadOnlyList<NotificationDeliveryDto>>
{
    public async Task<IReadOnlyList<NotificationDeliveryDto>> Handle(
        GetNotificationDeliveriesQuery request,
        CancellationToken cancellationToken)
    {
        _ = await repository.GetAsync(request.RegistrationId, cancellationToken)
            ?? throw new NotFoundException($"Registration {request.RegistrationId} was not found.");

        var deliveries = await repository.GetNotificationDeliveriesAsync(request.RegistrationId, cancellationToken);
        return deliveries.Select(x => new NotificationDeliveryDto(
            x.Id,
            x.Channel,
            x.Destination,
            x.TemplateCode,
            x.Status,
            x.AttemptCount,
            x.ProviderMessageId,
            x.FailureReason,
            x.CreatedUtc,
            x.SentUtc)).ToList();
    }
}
