using CustomerService.Application.Abstractions.Notifications;
using CustomerService.Application.Abstractions.Persistence;
using CustomerService.Domain.Dtos;
using CustomerService.Domain.Entities;
using CustomerService.Domain.Shared;
using FluentValidation;
using MediatR;

namespace CustomerService.Application.Registrations;

public sealed record StartRegistrationCommand(
    string Email,
    string MobileNumber,
    RegistrationType Type,
    string? NationalId,
    string? LegacyCustomerId) : IRequest<Result<StartRegistrationResponse>>;

public sealed class StartRegistrationValidator : AbstractValidator<StartRegistrationCommand>
{
    public StartRegistrationValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.MobileNumber).NotEmpty().MinimumLength(8);
        When(x => x.Type == RegistrationType.ExistingCustomerMigration,
            () => RuleFor(x => x.LegacyCustomerId).NotEmpty());
    }
}

public sealed class StartRegistrationHandler(IRegistrationRepository repo)
    : IRequestHandler<StartRegistrationCommand, Result<StartRegistrationResponse>>
{
    public async Task<Result<StartRegistrationResponse>> Handle(
        StartRegistrationCommand r,
        CancellationToken ct)
    {
        var duplicate = await repo.HasActiveDuplicateAsync(
            r.Email,
            r.MobileNumber,
            r.NationalId,
            ct);

        if (duplicate)
            return Result.Failure<StartRegistrationResponse>(
                "An active or completed registration already exists for the supplied identity.");

        if (r.Type == RegistrationType.ExistingCustomerMigration)
        {
            var customer = await repo.GetCustomerByLegacyIdAsync(r.LegacyCustomerId!, ct);

            if (customer is null)
                return Result.NotFound<StartRegistrationResponse>("Legacy customer was not found.");
        }

        var registration = RegistrationApplication.Create(
            r.Email,
            r.MobileNumber,
            r.Type,
            r.NationalId,
            r.LegacyCustomerId);

        await repo.AddAsync(registration, ct);
        await repo.AddStepAsync(registration.Id, registration.CurrentStep, "Completed", ct);
        await repo.SaveChangesAsync(ct);

        var response = new StartRegistrationResponse(
            registration.Id,
            registration.Status,
            registration.CurrentStep);

        return Result.Created(response, "Registration started.");
    }
}

public sealed record SendOtpCommand(
    Guid RegistrationId,
    OtpChannel Channel) : IRequest<Result>;

public sealed class SendOtpValidator : AbstractValidator<SendOtpCommand>
{
    public SendOtpValidator()
    {
        RuleFor(x => x.RegistrationId).NotEmpty();
        RuleFor(x => x.Channel).IsInEnum();
    }
}

public sealed class SendOtpHandler(
    IRegistrationRepository repo,
    IOtpService otp,
    INotificationDeliveryService deliveryService)
    : IRequestHandler<SendOtpCommand, Result>
{
    public async Task<Result> Handle(SendOtpCommand r, CancellationToken ct)
    {
        var registration = await repo.GetAsync(r.RegistrationId, ct);

        if (registration is null)
            return Result.NotFound("Registration not found.");

        var latest = await repo.GetLatestOtpAsync(r.RegistrationId, r.Channel, ct);

        if (latest is not null && DateTime.UtcNow < latest.NextResendAllowedUtc)
            return Result.Failure($"OTP can be resent after {latest.NextResendAllowedUtc:O}.");

        var sentInLastHour = await repo.CountOtpsSinceAsync(
            r.RegistrationId,
            r.Channel,
            DateTime.UtcNow.AddHours(-1),
            ct);

        if (sentInLastHour >= 5)
            return Result.Failure("OTP hourly limit reached.");

        await repo.InvalidateActiveOtpsAsync(r.RegistrationId, r.Channel, ct);

        var code = otp.GenerateCode();
        var hashedOtp = otp.Hash(code);
        var challenge = OtpChallenge.Create(
            r.RegistrationId,
            r.Channel,
            hashedOtp.Hash,
            hashedOtp.Salt,
            10,
            5,
            60);

        await repo.AddOtpAsync(challenge, ct);

        var notificationChannel = r.Channel == OtpChannel.Email
            ? NotificationChannel.Email
            : NotificationChannel.Sms;
        var templateCode = r.Channel == OtpChannel.Email
            ? "REGISTRATION_EMAIL_OTP"
            : "REGISTRATION_SMS_OTP";
        var template = await repo.GetTemplateAsync(templateCode, notificationChannel, ct);

        if (template is null)
            return Result.NotFound("Notification template not found.");

        var delivery = await deliveryService.SendOtpAsync(
            registration,
            challenge,
            template,
            code,
            ct);
        var step = r.Channel == OtpChannel.Email
            ? RegistrationStep.EmailOtpSent
            : RegistrationStep.SmsOtpSent;

        await repo.AddStepAsync(registration.Id, step, delivery.Status.ToString(), ct);
        await repo.SaveChangesAsync(ct);

        return Result.Success(r.Channel == OtpChannel.Email
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
    IRegistrationRepository repo,
    IOtpService otp) : IRequestHandler<VerifyOtpCommand, Result>
{
    public async Task<Result> Handle(VerifyOtpCommand r, CancellationToken ct)
    {
        var registration = await repo.GetAsync(r.RegistrationId, ct);

        if (registration is null)
            return Result.NotFound("Registration not found.");

        var challenge = await repo.GetLatestOtpAsync(r.RegistrationId, r.Channel, ct);

        if (challenge is null)
            return Result.NotFound("OTP not found.");

        var verified = challenge.CanVerify &&
                       otp.Verify(r.Otp, challenge.CodeHash, challenge.Salt);

        await repo.AddVerificationAttemptAsync(new OtpVerificationAttempt
        {
            Id = Guid.NewGuid(),
            OtpChallengeId = challenge.Id,
            WasSuccessful = verified,
            FailureReason = verified
                ? null
                : "Invalid, expired, used or locked OTP.",
            IpAddress = r.IpAddress,
            UserAgent = r.UserAgent,
            SubmittedUtc = DateTime.UtcNow
        }, ct);

        if (!verified)
        {
            challenge.RecordFailedAttempt();
            await repo.SaveChangesAsync(ct);
            return Result.Failure("Invalid or expired OTP.");
        }

        challenge.MarkVerified();

        if (r.Channel == OtpChannel.Email)
            registration.MarkEmailVerified();
        else
            registration.MarkSmsVerified();

        var step = r.Channel == OtpChannel.Email
            ? RegistrationStep.EmailVerified
            : RegistrationStep.SmsVerified;

        await repo.AddStepAsync(registration.Id, step, "Completed", ct);
        await repo.SaveChangesAsync(ct);

        return Result.Success(r.Channel == OtpChannel.Email
            ? "Email verified."
            : "Mobile verified.");
    }
}

public sealed record UpdateRegistrationProfileCommand(
    Guid Id,
    string FullName,
    string? NationalId) : IRequest<Result>;

public sealed class UpdateRegistrationProfileValidator
    : AbstractValidator<UpdateRegistrationProfileCommand>
{
    public UpdateRegistrationProfileValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NationalId).MaximumLength(50);
    }
}

public sealed class UpdateRegistrationProfileHandler(IRegistrationRepository repo)
    : IRequestHandler<UpdateRegistrationProfileCommand, Result>
{
    public async Task<Result> Handle(
        UpdateRegistrationProfileCommand r,
        CancellationToken ct)
    {
        var registration = await repo.GetAsync(r.Id, ct);

        if (registration is null)
            return Result.NotFound("Registration not found.");

        registration.UpdateProfile(r.FullName, r.NationalId);
        await repo.AddStepAsync(registration.Id, RegistrationStep.ProfileCompleted, "Completed", ct);
        await repo.SaveChangesAsync(ct);

        return Result.Success("Profile updated.");
    }
}

public sealed record AcceptTermsCommand(
    Guid Id,
    IReadOnlyCollection<Guid> TermIds,
    string? Ip,
    string? UserAgent) : IRequest<Result>;

public sealed class AcceptTermsValidator : AbstractValidator<AcceptTermsCommand>
{
    public AcceptTermsValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.TermIds).NotEmpty();
    }
}

public sealed class AcceptTermsHandler(IRegistrationRepository repo)
    : IRequestHandler<AcceptTermsCommand, Result>
{
    public async Task<Result> Handle(AcceptTermsCommand r, CancellationToken ct)
    {
        var registration = await repo.GetAsync(r.Id, ct);

        if (registration is null)
            return Result.NotFound("Registration not found.");

        var activeTerms = await repo.GetActiveTermsAsync(ct);
        var requiredTermIds = activeTerms
            .Where(x => x.IsRequired)
            .Select(x => x.Id)
            .ToHashSet();
        var acceptedTermIds = r.TermIds.ToHashSet();

        if (!requiredTermIds.IsSubsetOf(acceptedTermIds))
            return Result.Failure("All required terms must be accepted.");

        foreach (var termId in r.TermIds.Distinct())
        {
            var term = activeTerms.SingleOrDefault(x => x.Id == termId);

            if (term is null)
                return Result.Failure("An invalid or inactive term was supplied.");

            await repo.AddConsentAsync(new RegistrationConsent
            {
                Id = Guid.NewGuid(),
                RegistrationId = registration.Id,
                TermDocumentId = term.Id,
                TermVersion = term.Version,
                Accepted = true,
                AcceptedUtc = DateTime.UtcNow,
                IpAddress = r.Ip,
                UserAgent = r.UserAgent
            }, ct);
        }

        registration.MarkTermsAccepted();
        await repo.AddStepAsync(registration.Id, RegistrationStep.TermsAccepted, "Completed", ct);
        await repo.SaveChangesAsync(ct);

        return Result.Success("Terms accepted.");
    }
}

public sealed record SetPinCommand(
    Guid Id,
    string Pin,
    string ConfirmPin) : IRequest<Result>;

public sealed class SetPinValidator : AbstractValidator<SetPinCommand>
{
    public SetPinValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Pin).Matches("^[0-9]{6}$");
        RuleFor(x => x.ConfirmPin).Equal(x => x.Pin);
    }
}

public sealed class SetPinHandler(
    IRegistrationRepository repo,
    IPinService pinService) : IRequestHandler<SetPinCommand, Result>
{
    public async Task<Result> Handle(SetPinCommand r, CancellationToken ct)
    {
        var registration = await repo.GetAsync(r.Id, ct);

        if (registration is null)
            return Result.NotFound("Registration not found.");

        var hashedPin = pinService.Hash(r.Pin);

        registration.SetPin(hashedPin.Hash, hashedPin.Salt);
        await repo.AddStepAsync(registration.Id, RegistrationStep.PinConfigured, "Completed", ct);
        await repo.SaveChangesAsync(ct);

        return Result.Success("PIN configured.");
    }
}

public sealed record CompleteRegistrationCommand(Guid Id) : IRequest<Result>;

public sealed class CompleteRegistrationValidator
    : AbstractValidator<CompleteRegistrationCommand>
{
    public CompleteRegistrationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class CompleteRegistrationHandler(IRegistrationRepository repo)
    : IRequestHandler<CompleteRegistrationCommand, Result>
{
    public async Task<Result> Handle(
        CompleteRegistrationCommand r,
        CancellationToken ct)
    {
        var registration = await repo.GetAsync(r.Id, ct);

        if (registration is null)
            return Result.NotFound("Registration not found.");

        var acceptedRequiredTerms = await repo.HasAcceptedRequiredTermsAsync(
            registration.Id,
            ct);

        if (!acceptedRequiredTerms)
            return Result.Failure("Required terms are not accepted.");

        registration.Complete();

        await repo.AddCustomerAsync(new CustomerAccount
        {
            Id = Guid.NewGuid(),
            RegistrationId = registration.Id,
            Email = registration.Email,
            MobileNumber = registration.MobileNumber,
            NationalId = registration.NationalId,
            FullName = registration.FullName,
            LegacyCustomerId = registration.LegacyCustomerId,
            IsMigrated = registration.Type == RegistrationType.ExistingCustomerMigration,
            CreatedUtc = DateTime.UtcNow
        }, ct);
        await repo.AddStepAsync(registration.Id, RegistrationStep.Completed, "Completed", ct);
        await repo.SaveChangesAsync(ct);

        return Result.Success("Registration completed.");
    }
}

public sealed record CancelRegistrationCommand(
    Guid Id,
    string Reason) : IRequest<Result>;

public sealed class CancelRegistrationValidator
    : AbstractValidator<CancelRegistrationCommand>
{
    public CancelRegistrationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public sealed class CancelRegistrationHandler(IRegistrationRepository repo)
    : IRequestHandler<CancelRegistrationCommand, Result>
{
    public async Task<Result> Handle(
        CancelRegistrationCommand r,
        CancellationToken ct)
    {
        var registration = await repo.GetAsync(r.Id, ct);

        if (registration is null)
            return Result.NotFound("Registration not found.");

        registration.Cancel(r.Reason);
        await repo.SaveChangesAsync(ct);

        return Result.Success("Registration cancelled.");
    }
}

public sealed record GetRegistrationQuery(Guid Id)
    : IRequest<Result<RegistrationDto>>;

public sealed class GetRegistrationValidator : AbstractValidator<GetRegistrationQuery>
{
    public GetRegistrationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class GetRegistrationHandler(IRegistrationRepository repo)
    : IRequestHandler<GetRegistrationQuery, Result<RegistrationDto>>
{
    public async Task<Result<RegistrationDto>> Handle(
        GetRegistrationQuery r,
        CancellationToken ct)
    {
        var registration = await repo.GetAsync(r.Id, ct);

        if (registration is null)
            return Result.NotFound<RegistrationDto>("Registration not found.");

        var response = new RegistrationDto(
            registration.Id,
            registration.Type,
            registration.Status,
            registration.CurrentStep,
            registration.Email,
            registration.MobileNumber,
            registration.NationalId,
            registration.FullName,
            registration.LegacyCustomerId,
            registration.EmailVerified,
            registration.SmsVerified,
            registration.PinSetUtc is not null,
            registration.ExpiresUtc,
            registration.CreatedUtc,
            registration.UpdatedUtc);

        return Result.Success(response, "Registration retrieved.");
    }
}

public sealed record GetDeliveriesQuery(Guid Id)
    : IRequest<Result<IReadOnlyList<NotificationDeliveryDto>>>;

public sealed class GetDeliveriesValidator : AbstractValidator<GetDeliveriesQuery>
{
    public GetDeliveriesValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class GetDeliveriesHandler(IRegistrationRepository repo)
    : IRequestHandler<GetDeliveriesQuery, Result<IReadOnlyList<NotificationDeliveryDto>>>
{
    public async Task<Result<IReadOnlyList<NotificationDeliveryDto>>> Handle(
        GetDeliveriesQuery r,
        CancellationToken ct)
    {
        var registration = await repo.GetAsync(r.Id, ct);

        if (registration is null)
            return Result.NotFound<IReadOnlyList<NotificationDeliveryDto>>(
                "Registration not found.");

        var deliveries = await repo.GetDeliveriesAsync(r.Id, ct);
        var response = deliveries
            .Select(x => new NotificationDeliveryDto(
                x.Id,
                x.Channel,
                x.Destination,
                x.TemplateCode,
                x.Status,
                x.AttemptCount,
                x.ProviderMessageId,
                x.FailureReason,
                x.CreatedUtc,
                x.SentUtc))
            .ToList();

        return Result.Success<IReadOnlyList<NotificationDeliveryDto>>(
            response,
            "Deliveries retrieved.");
    }
}
