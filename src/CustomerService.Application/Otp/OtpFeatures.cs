using CustomerService.Application.Abstractions.Notifications;
using CustomerService.Application.Abstractions.Persistence;
using CustomerService.Domain.Entities;
using CustomerService.Domain.Shared;
using FluentValidation;
using MediatR;

namespace CustomerService.Application.Otp;

public sealed record SendOtpCommand(Guid RegistrationId, OtpChannel Channel) : IRequest<Result>;

public sealed class SendOtpValidator : AbstractValidator<SendOtpCommand>
{
    public SendOtpValidator()
    {
        RuleFor(x => x.RegistrationId).NotEmpty();
        RuleFor(x => x.Channel).IsInEnum();
    }
}

public sealed class SendOtpHandler(
    IRegistrationQueryRepository queryRepository,
    IRegistrationCommandRepository commandRepository,
    IOtpService otpService,
    INotificationDeliveryService deliveryService)
    : IRequestHandler<SendOtpCommand, Result>
{
    public async Task<Result> Handle(SendOtpCommand request, CancellationToken cancellationToken)
    {
        var registration = await queryRepository.GetAsync(request.RegistrationId, cancellationToken);

        if (registration is null)
        {
            return Result.NotFound("Registration not found.");
        }

        var latest = await queryRepository.GetLatestOtpAsync(
            request.RegistrationId,
            request.Channel,
            cancellationToken);

        if (latest is not null && DateTime.UtcNow < latest.NextResendAllowedUtc)
        {
            return Result.Failure($"OTP can be resent after {latest.NextResendAllowedUtc:O}.");
        }

        var sentInLastHour = await queryRepository.CountOtpsSinceAsync(
            request.RegistrationId,
            request.Channel,
            DateTime.UtcNow.AddHours(-1),
            cancellationToken);

        if (sentInLastHour >= 5)
        {
            return Result.Failure("OTP hourly limit reached.");
        }

        await commandRepository.InvalidateActiveOtpsAsync(
            request.RegistrationId,
            request.Channel,
            cancellationToken);

        var code = otpService.GenerateCode();
        var hashedOtp = otpService.Hash(code);
        var challenge = OtpChallenge.Create(
            request.RegistrationId,
            request.Channel,
            hashedOtp.Hash,
            hashedOtp.Salt,
            10,
            5,
            60);

        await commandRepository.AddOtpAsync(challenge, cancellationToken);

        var notificationChannel = request.Channel == OtpChannel.Email
            ? NotificationChannel.Email
            : NotificationChannel.Sms;
        var templateCode = request.Channel == OtpChannel.Email
            ? "REGISTRATION_EMAIL_OTP"
            : "REGISTRATION_SMS_OTP";
        var template = await queryRepository.GetTemplateAsync(
            templateCode,
            notificationChannel,
            cancellationToken);

        if (template is null)
        {
            return Result.NotFound("Notification template not found.");
        }

        var delivery = await deliveryService.SendOtpAsync(
            registration,
            challenge,
            template,
            code,
            cancellationToken);
        var step = request.Channel == OtpChannel.Email
            ? RegistrationStep.EmailOtpSent
            : RegistrationStep.SmsOtpSent;

        await commandRepository.AddStepAsync(
            registration.Id,
            step,
            delivery.Status.ToString(),
            cancellationToken);
        await commandRepository.SaveChangesAsync(cancellationToken);

        return Result.Success(request.Channel == OtpChannel.Email
            ? "Email OTP sent."
            : "SMS OTP sent.");
    }
}

public sealed record VerifyOtpCommand(
    Guid RegistrationId,
    OtpChannel Channel,
    string Otp,
    string? IpAddress,
    string? UserAgent) : IRequest<Result>;

public sealed class VerifyOtpValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpValidator()
    {
        RuleFor(x => x.RegistrationId).NotEmpty();
        RuleFor(x => x.Channel).IsInEnum();
        RuleFor(x => x.Otp).Matches("^[0-9]{6}$");
    }
}

public sealed class VerifyOtpHandler(
    IRegistrationQueryRepository queryRepository,
    IRegistrationCommandRepository commandRepository,
    IOtpService otpService)
    : IRequestHandler<VerifyOtpCommand, Result>
{
    public async Task<Result> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        var registration = await queryRepository.GetAsync(request.RegistrationId, cancellationToken);

        if (registration is null)
        {
            return Result.NotFound("Registration not found.");
        }

        var challenge = await queryRepository.GetLatestOtpAsync(
            request.RegistrationId,
            request.Channel,
            cancellationToken);

        if (challenge is null)
        {
            return Result.NotFound("OTP not found.");
        }

        var verified = challenge.CanVerify
                       && otpService.Verify(request.Otp, challenge.CodeHash, challenge.Salt);

        var attempt = new OtpVerificationAttempt
        {
            Id = Guid.NewGuid(),
            OtpChallengeId = challenge.Id,
            WasSuccessful = verified,
            FailureReason = verified ? null : "Invalid, expired, used or locked OTP.",
            IpAddress = request.IpAddress,
            UserAgent = request.UserAgent,
            SubmittedUtc = DateTime.UtcNow
        };

        await commandRepository.AddVerificationAttemptAsync(attempt, cancellationToken);

        if (!verified)
        {
            challenge.RecordFailedAttempt();
            await commandRepository.SaveChangesAsync(cancellationToken);
            return Result.Failure("Invalid or expired OTP.");
        }

        challenge.MarkVerified();

        if (request.Channel == OtpChannel.Email)
        {
            registration.MarkEmailVerified();
        }
        else
        {
            registration.MarkSmsVerified();
        }

        var step = request.Channel == OtpChannel.Email
            ? RegistrationStep.EmailVerified
            : RegistrationStep.SmsVerified;

        await commandRepository.AddStepAsync(
            registration.Id,
            step,
            "Completed",
            cancellationToken);
        await commandRepository.SaveChangesAsync(cancellationToken);

        return Result.Success(request.Channel == OtpChannel.Email
            ? "Email verified."
            : "Mobile verified.");
    }
}
