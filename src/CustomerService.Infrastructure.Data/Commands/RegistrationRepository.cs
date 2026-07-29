using CustomerService.Application.Abstractions.Persistence;
using CustomerService.Domain.Entities;
using CustomerService.Infrastructure.Data.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Infrastructure.Data.Commands;

public sealed class RegistrationRepository(ApplicationDbContext db) : IRegistrationRepository
{
    public Task<RegistrationApplication?> GetAsync(Guid id, CancellationToken cancellationToken)
        => db.RegistrationApplications.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddAsync(RegistrationApplication registration, CancellationToken cancellationToken)
        => await db.RegistrationApplications.AddAsync(registration, cancellationToken);

    public Task<OtpChallenge?> GetLatestOtpAsync(Guid registrationId, OtpChannel channel, CancellationToken cancellationToken)
        => db.OtpChallenges
            .Where(x => x.RegistrationId == registrationId && x.Channel == channel)
            .OrderByDescending(x => x.CreatedUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddOtpAsync(OtpChallenge challenge, CancellationToken cancellationToken)
        => await db.OtpChallenges.AddAsync(challenge, cancellationToken);

    public Task<NotificationTemplate?> GetTemplateAsync(
        string code,
        NotificationChannel channel,
        CancellationToken cancellationToken)
        => db.NotificationTemplates
            .AsNoTracking()
            .Where(x => x.Code == code && x.Channel == channel && x.IsActive)
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddNotificationDeliveryAsync(NotificationDelivery delivery, CancellationToken cancellationToken)
        => await db.NotificationDeliveries.AddAsync(delivery, cancellationToken);

    public async Task<IReadOnlyList<NotificationDelivery>> GetNotificationDeliveriesAsync(
        Guid registrationId,
        CancellationToken cancellationToken)
        => await db.NotificationDeliveries
            .AsNoTracking()
            .Where(x => x.RegistrationId == registrationId)
            .OrderByDescending(x => x.CreatedUtc)
            .ToListAsync(cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => db.SaveChangesAsync(cancellationToken);
}
