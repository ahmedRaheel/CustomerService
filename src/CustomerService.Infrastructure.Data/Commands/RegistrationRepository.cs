using CustomerService.Application.Abstractions.Persistence;
using CustomerService.Domain.Entities;
using CustomerService.Infrastructure.Data.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Infrastructure.Data.Commands;

public sealed class RegistrationRepository(
    ApplicationDbContext dbContext)
    : IRegistrationRepository
{
    public Task<RegistrationApplication?> GetAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return dbContext.RegistrationApplications
            .SingleOrDefaultAsync(
                registration => registration.Id == id,
                cancellationToken);
    }

    public async Task AddAsync(
        RegistrationApplication registration,
        CancellationToken cancellationToken)
    {
        await dbContext.RegistrationApplications.AddAsync(
            registration,
            cancellationToken);
    }

    public Task<bool> HasActiveDuplicateAsync(
        string email,
        string mobileNumber,
        string? nationalId,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(email);
        var normalizedMobileNumber = NormalizeMobileNumber(mobileNumber);
        var normalizedNationalId = NormalizeOptionalValue(nationalId);

        return dbContext.RegistrationApplications
            .AnyAsync(
                registration =>
                    (
                        registration.NormalizedEmail == normalizedEmail
                        || registration.NormalizedMobileNumber == normalizedMobileNumber
                        || (
                            normalizedNationalId != null
                            && registration.NationalId == normalizedNationalId
                        )
                    )
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
            .Where(otp =>
                otp.RegistrationId == registrationId
                && otp.Channel == channel)
            .OrderByDescending(otp => otp.CreatedUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddOtpAsync(
        OtpChallenge otpChallenge,
        CancellationToken cancellationToken)
    {
        await dbContext.OtpChallenges.AddAsync(
            otpChallenge,
            cancellationToken);
    }

    public async Task InvalidateActiveOtpsAsync(
        Guid registrationId,
        OtpChannel channel,
        CancellationToken cancellationToken)
    {
        var activeOtpChallenges = await dbContext.OtpChallenges
            .Where(otp =>
                otp.RegistrationId == registrationId
                && otp.Channel == channel
                && otp.VerifiedUtc == null
                && otp.InvalidatedUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var otpChallenge in activeOtpChallenges)
        {
            otpChallenge.Invalidate();
        }
    }

    public Task<int> CountOtpsSinceAsync(
        Guid registrationId,
        OtpChannel channel,
        DateTime sinceUtc,
        CancellationToken cancellationToken)
    {
        return dbContext.OtpChallenges
            .CountAsync(
                otp =>
                    otp.RegistrationId == registrationId
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
            .Where(template =>
                template.Code == code
                && template.Channel == channel
                && template.IsActive)
            .OrderByDescending(template => template.Version)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddDeliveryAsync(
        NotificationDelivery notificationDelivery,
        CancellationToken cancellationToken)
    {
        await dbContext.NotificationDeliveries.AddAsync(
            notificationDelivery,
            cancellationToken);
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

    public async Task AddVerificationAttemptAsync(
        OtpVerificationAttempt verificationAttempt,
        CancellationToken cancellationToken)
    {
        await dbContext.OtpVerificationAttempts.AddAsync(
            verificationAttempt,
            cancellationToken);
    }

    public async Task<IReadOnlyList<TermDocument>> GetActiveTermsAsync(
        CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;

        return await dbContext.TermDocuments
            .AsNoTracking()
            .Where(term =>
                term.IsActive
                && term.EffectiveFromUtc <= utcNow
                && (
                    term.EffectiveToUtc == null
                    || term.EffectiveToUtc > utcNow
                ))
            .OrderBy(term => term.Code)
            .ToListAsync(cancellationToken);
    }

    public Task<TermDocument?> GetTermAsync(
        Guid termDocumentId,
        CancellationToken cancellationToken)
    {
        return dbContext.TermDocuments
            .AsNoTracking()
            .SingleOrDefaultAsync(
                term =>
                    term.Id == termDocumentId
                    && term.IsActive,
                cancellationToken);
    }

    public async Task<bool> HasAcceptedRequiredTermsAsync(
        Guid registrationId,
        CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;

        var requiredTerms = await dbContext.TermDocuments
            .AsNoTracking()
            .Where(term =>
                term.IsRequired
                && term.IsActive
                && term.EffectiveFromUtc <= utcNow
                && (
                    term.EffectiveToUtc == null
                    || term.EffectiveToUtc > utcNow
                ))
            .Select(term => new
            {
                TermDocumentId = term.Id,
                term.Version
            })
            .ToListAsync(cancellationToken);

        if (requiredTerms.Count == 0)
        {
            return true;
        }

        var acceptedTerms = await dbContext.RegistrationConsents
            .AsNoTracking()
            .Where(consent =>
                consent.RegistrationId == registrationId
                && consent.Accepted)
            .Select(consent => new
            {
                consent.TermDocumentId,
                Version = consent.TermVersion
            })
            .ToListAsync(cancellationToken);

        return requiredTerms.All(requiredTerm =>
            acceptedTerms.Any(acceptedTerm =>
                acceptedTerm.TermDocumentId == requiredTerm.TermDocumentId
                && acceptedTerm.Version == requiredTerm.Version));
    }

    public async Task AddConsentAsync(
        RegistrationConsent consent,
        CancellationToken cancellationToken)
    {
        var consentExists = await dbContext.RegistrationConsents
            .AnyAsync(
                existingConsent =>
                    existingConsent.RegistrationId == consent.RegistrationId
                    && existingConsent.TermDocumentId == consent.TermDocumentId
                    && existingConsent.TermVersion == consent.TermVersion,
                cancellationToken);

        if (consentExists)
        {
            return;
        }

        await dbContext.RegistrationConsents.AddAsync(
            consent,
            cancellationToken);
    }

    public async Task AddStepAsync(
        Guid registrationId,
        RegistrationStep step,
        string status,
        CancellationToken cancellationToken)
    {
        var stepHistory = new RegistrationStepHistory
        {
            Id = Guid.NewGuid(),
            RegistrationId = registrationId,
            Step = step,
            Status = status,
            OccurredUtc = DateTime.UtcNow
        };

        await dbContext.RegistrationStepHistories.AddAsync(
            stepHistory,
            cancellationToken);
    }

    public Task<CustomerAccount?> GetCustomerByLegacyIdAsync(
        string legacyCustomerId,
        CancellationToken cancellationToken)
    {
        var normalizedLegacyCustomerId = legacyCustomerId.Trim();

        return dbContext.CustomerAccounts
            .SingleOrDefaultAsync(
                customer =>
                    customer.LegacyCustomerId == normalizedLegacyCustomerId,
                cancellationToken);
    }

    public async Task AddCustomerAsync(
        CustomerAccount customerAccount,
        CancellationToken cancellationToken)
    {
        var customerExists = await dbContext.CustomerAccounts
            .AnyAsync(
                customer =>
                    customer.RegistrationId == customerAccount.RegistrationId,
                cancellationToken);

        if (customerExists)
        {
            return;
        }

        await dbContext.CustomerAccounts.AddAsync(
            customerAccount,
            cancellationToken);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToUpperInvariant();
    }

    private static string NormalizeMobileNumber(string mobileNumber)
    {
        return new string(
            mobileNumber
                .Where(char.IsDigit)
                .ToArray());
    }

    private static string? NormalizeOptionalValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}