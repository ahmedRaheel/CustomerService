using CustomerService.Application.Abstractions.Persistence;
using CustomerService.Domain.Entities;
using CustomerService.Infrastructure.Data.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Infrastructure.Data.Commands;

public sealed class RegistrationCommandRepository(ApplicationDbContext dbContext)
    : IRegistrationCommandRepository
{
    public async Task AddAsync(RegistrationApplication registration, CancellationToken cancellationToken)
    {
        await dbContext.RegistrationApplications.AddAsync(registration, cancellationToken);
    }

    public async Task AddOtpAsync(OtpChallenge otpChallenge, CancellationToken cancellationToken)
    {
        await dbContext.OtpChallenges.AddAsync(otpChallenge, cancellationToken);
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

    public async Task AddDeliveryAsync(
        NotificationDelivery notificationDelivery,
        CancellationToken cancellationToken)
    {
        await dbContext.NotificationDeliveries.AddAsync(notificationDelivery, cancellationToken);
    }

    public async Task AddVerificationAttemptAsync(
        OtpVerificationAttempt verificationAttempt,
        CancellationToken cancellationToken)
    {
        await dbContext.OtpVerificationAttempts.AddAsync(verificationAttempt, cancellationToken);
    }

    public async Task AddConsentAsync(
        RegistrationConsent consent,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.RegistrationConsents.AnyAsync(
            existing =>
                existing.RegistrationId == consent.RegistrationId
                && existing.TermDocumentId == consent.TermDocumentId
                && existing.TermVersion == consent.TermVersion,
            cancellationToken);

        if (exists)
        {
            return;
        }

        await dbContext.RegistrationConsents.AddAsync(consent, cancellationToken);
    }

    public async Task AddStepAsync(
        Guid registrationId,
        RegistrationStep step,
        string status,
        CancellationToken cancellationToken)
    {
        var history = new RegistrationStepHistory
        {
            Id = Guid.NewGuid(),
            RegistrationId = registrationId,
            Step = step,
            Status = status,
            OccurredUtc = DateTime.UtcNow
        };

        await dbContext.RegistrationStepHistories.AddAsync(history, cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
