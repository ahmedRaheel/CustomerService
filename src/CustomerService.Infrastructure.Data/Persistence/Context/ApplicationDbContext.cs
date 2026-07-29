using CustomerService.Application.Abstractions.Persistence;
using CustomerService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Infrastructure.Data.Persistence.Context;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<RegistrationApplication> RegistrationApplications =>
        Set<RegistrationApplication>();

    public DbSet<OtpChallenge> OtpChallenges =>
        Set<OtpChallenge>();

    public DbSet<NotificationTemplate> NotificationTemplates =>
        Set<NotificationTemplate>();

    public DbSet<NotificationDelivery> NotificationDeliveries =>
        Set<NotificationDelivery>();

    public DbSet<OtpVerificationAttempt> OtpVerificationAttempts =>
        Set<OtpVerificationAttempt>();

    public DbSet<TermDocument> TermDocuments =>
        Set<TermDocument>();

    public DbSet<RegistrationConsent> RegistrationConsents =>
        Set<RegistrationConsent>();

    public DbSet<RegistrationStepHistory> RegistrationStepHistories =>
        Set<RegistrationStepHistory>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);

        base.OnModelCreating(builder);
    }
}
