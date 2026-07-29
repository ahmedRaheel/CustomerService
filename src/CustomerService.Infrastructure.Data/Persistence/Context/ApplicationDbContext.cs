using Microsoft.EntityFrameworkCore;
using CustomerService.Application.Abstractions.Persistence;
using CustomerService.Domain.Entities;
namespace CustomerService.Infrastructure.Data.Persistence.Context;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options), IApplicationDbContext
{

    public DbSet<RegistrationApplication> RegistrationApplications => Set<RegistrationApplication>();
    public DbSet<OtpChallenge> OtpChallenges => Set<OtpChallenge>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
