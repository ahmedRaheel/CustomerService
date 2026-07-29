using CustomerService.Application.Abstractions.Persistence;
using CustomerService.Domain.Entities;
using CustomerService.Infrastructure.Data.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Infrastructure.Data.Queries;

public sealed class RegistrationQueryRepository(ApplicationDbContext dbContext)
    : IRegistrationQueryRepository
{
    public Task<RegistrationApplication?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.RegistrationApplications
            .SingleOrDefaultAsync(registration => registration.Id == id, cancellationToken);
    }

    public Task<bool> HasActiveDuplicateAsync(
        string email,
        string mobileNumber,
        string? nationalId,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();
        var normalizedMobileNumber = new string(mobileNumber.Where(char.IsDigit).ToArray());
        var normalizedNationalId = string.IsNullOrWhiteSpace(nationalId) ? null : nationalId.Trim();

        return dbContext.RegistrationApplications.AnyAsync(
            registration =>
                (registration.NormalizedEmail == normalizedEmail
                 || registration.NormalizedMobileNumber == normalizedMobileNumber
                 || (normalizedNationalId != null && registration.NationalId == normalizedNationalId))
                && registration.Status != RegistrationStatus.Cancelled
                && registration.Status != RegistrationStatus.Expired,
            cancellationToken);
    }

    public Task<OtpChallenge?> GetLatestOtpAsync(
        Guid registrationId,
        OtpChannel channel,
        CancellationToken cancellationToken)
    {
        return dbContext.OtpChallenges
            .Where(otp => otp.RegistrationId == registrationId && otp.Channel == channel)
            .OrderByDescending(otp => otp.CreatedUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<int> CountOtpsSinceAsync(
        Guid registrationId,
        OtpChannel channel,
        DateTime sinceUtc,
        CancellationToken cancellationToken)
    {
        return dbContext.OtpChallenges.CountAsync(
            otp => otp.RegistrationId == registrationId
                   && otp.Channel == channel
                   && otp.CreatedUtc >= sinceUtc,
            cancellationToken);
    }

    public Task<NotificationTemplate?> GetTemplateAsync(
        string code,
        NotificationChannel channel,
        CancellationToken cancellationToken)
    {
        return dbContext.NotificationTemplates
            .AsNoTracking()
            .Where(template => template.Code == code
                               && template.Channel == channel
                               && template.IsActive)
            .OrderByDescending(template => template.Version)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationDelivery>> GetDeliveriesAsync(
        Guid registrationId,
        CancellationToken cancellationToken)
    {
        return await dbContext.NotificationDeliveries
            .AsNoTracking()
            .Where(delivery => delivery.RegistrationId == registrationId)
            .OrderByDescending(delivery => delivery.CreatedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TermDocument>> GetActiveTermsAsync(
        CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;

        return await dbContext.TermDocuments
            .AsNoTracking()
            .Where(term => term.IsActive
                           && term.EffectiveFromUtc <= utcNow
                           && (term.EffectiveToUtc == null || term.EffectiveToUtc > utcNow))
            .OrderBy(term => term.Code)
            .ToListAsync(cancellationToken);
    }

    public Task<TermDocument?> GetTermAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.TermDocuments
            .AsNoTracking()
            .SingleOrDefaultAsync(term => term.Id == id && term.IsActive, cancellationToken);
    }

    public async Task<bool> HasAcceptedRequiredTermsAsync(
        Guid registrationId,
        CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;

        var requiredTerms = await dbContext.TermDocuments
            .AsNoTracking()
            .Where(term => term.IsRequired
                           && term.IsActive
                           && term.EffectiveFromUtc <= utcNow
                           && (term.EffectiveToUtc == null || term.EffectiveToUtc > utcNow))
            .Select(term => new { TermDocumentId = term.Id, term.Version })
            .ToListAsync(cancellationToken);

        var acceptedTerms = await dbContext.RegistrationConsents
            .AsNoTracking()
            .Where(consent => consent.RegistrationId == registrationId && consent.Accepted)
            .Select(consent => new { consent.TermDocumentId, Version = consent.TermVersion })
            .ToListAsync(cancellationToken);

        return requiredTerms.All(required => acceptedTerms.Any(accepted =>
            accepted.TermDocumentId == required.TermDocumentId
            && accepted.Version == required.Version));
    }
}
