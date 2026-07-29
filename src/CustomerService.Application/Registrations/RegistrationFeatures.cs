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

public sealed class StartRegistrationHandler(
    IRegistrationQueryRepository queryRepository,
    IRegistrationCommandRepository commandRepository)
    : IRequestHandler<StartRegistrationCommand, Result<StartRegistrationResponse>>
{
    public async Task<Result<StartRegistrationResponse>> Handle(
        StartRegistrationCommand r,
        CancellationToken ct)
    {
        var duplicate = await queryRepository.HasActiveDuplicateAsync(
            r.Email,
            r.MobileNumber,
            r.NationalId,
            ct);

        if (duplicate)
            return Result.Failure<StartRegistrationResponse>(
                "An active or completed registration already exists for the supplied identity.");


        var registration = RegistrationApplication.Create(
            r.Email,
            r.MobileNumber,
            r.Type,
            r.NationalId,
            r.LegacyCustomerId);

        await commandRepository.AddAsync(registration, ct);
        await commandRepository.AddStepAsync(registration.Id, registration.CurrentStep, "Completed", ct);
        await commandRepository.SaveChangesAsync(ct);

        var response = new StartRegistrationResponse(
            registration.Id,
            registration.Status,
            registration.CurrentStep);

        return Result.Created(response, "Registration started.");
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

public sealed class UpdateRegistrationProfileHandler(
    IRegistrationQueryRepository queryRepository,
    IRegistrationCommandRepository commandRepository)
    : IRequestHandler<UpdateRegistrationProfileCommand, Result>
{
    public async Task<Result> Handle(
        UpdateRegistrationProfileCommand r,
        CancellationToken ct)
    {
        var registration = await queryRepository.GetAsync(r.Id, ct);

        if (registration is null)
            return Result.NotFound("Registration not found.");

        registration.UpdateProfile(r.FullName, r.NationalId);
        await commandRepository.AddStepAsync(registration.Id, RegistrationStep.ProfileCompleted, "Completed", ct);
        await commandRepository.SaveChangesAsync(ct);

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

public sealed class AcceptTermsHandler(
    IRegistrationQueryRepository queryRepository,
    IRegistrationCommandRepository commandRepository)
    : IRequestHandler<AcceptTermsCommand, Result>
{
    public async Task<Result> Handle(AcceptTermsCommand r, CancellationToken ct)
    {
        var registration = await queryRepository.GetAsync(r.Id, ct);

        if (registration is null)
            return Result.NotFound("Registration not found.");

        var activeTerms = await queryRepository.GetActiveTermsAsync(ct);
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

            await commandRepository.AddConsentAsync(new RegistrationConsent
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
        await commandRepository.AddStepAsync(registration.Id, RegistrationStep.TermsAccepted, "Completed", ct);
        await commandRepository.SaveChangesAsync(ct);

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
    IRegistrationQueryRepository queryRepository,
    IRegistrationCommandRepository commandRepository,
    IPinService pinService) : IRequestHandler<SetPinCommand, Result>
{
    public async Task<Result> Handle(SetPinCommand r, CancellationToken ct)
    {
        var registration = await queryRepository.GetAsync(r.Id, ct);

        if (registration is null)
            return Result.NotFound("Registration not found.");

        var hashedPin = pinService.Hash(r.Pin);

        registration.SetPin(hashedPin.Hash, hashedPin.Salt);
        await commandRepository.AddStepAsync(registration.Id, RegistrationStep.PinConfigured, "Completed", ct);
        await commandRepository.SaveChangesAsync(ct);

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

public sealed class CompleteRegistrationHandler(
    IRegistrationQueryRepository queryRepository,
    IRegistrationCommandRepository commandRepository)
    : IRequestHandler<CompleteRegistrationCommand, Result>
{
    public async Task<Result> Handle(
        CompleteRegistrationCommand r,
        CancellationToken ct)
    {
        var registration = await queryRepository.GetAsync(r.Id, ct);

        if (registration is null)
            return Result.NotFound("Registration not found.");

        var acceptedRequiredTerms = await queryRepository.HasAcceptedRequiredTermsAsync(
            registration.Id,
            ct);

        if (!acceptedRequiredTerms)
            return Result.Failure("Required terms are not accepted.");

        registration.Complete();

        await commandRepository.AddStepAsync(registration.Id, RegistrationStep.Completed, "Completed", ct);
        await commandRepository.SaveChangesAsync(ct);

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

public sealed class CancelRegistrationHandler(
    IRegistrationQueryRepository queryRepository,
    IRegistrationCommandRepository commandRepository)
    : IRequestHandler<CancelRegistrationCommand, Result>
{
    public async Task<Result> Handle(
        CancelRegistrationCommand r,
        CancellationToken ct)
    {
        var registration = await queryRepository.GetAsync(r.Id, ct);

        if (registration is null)
            return Result.NotFound("Registration not found.");

        registration.Cancel(r.Reason);
        await commandRepository.SaveChangesAsync(ct);

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

public sealed class GetRegistrationHandler(IRegistrationQueryRepository queryRepository)
    : IRequestHandler<GetRegistrationQuery, Result<RegistrationDto>>
{
    public async Task<Result<RegistrationDto>> Handle(
        GetRegistrationQuery r,
        CancellationToken ct)
    {
        var registration = await queryRepository.GetAsync(r.Id, ct);

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
