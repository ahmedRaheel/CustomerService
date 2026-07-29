using CustomerService.Domain.Entities; using Microsoft.EntityFrameworkCore; using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace CustomerService.Infrastructure.Data.Persistence.Configurations;
public sealed class NotificationDeliveryConfiguration : IEntityTypeConfiguration<NotificationDelivery>
{
    public void Configure(EntityTypeBuilder<NotificationDelivery> builder)
    {
        builder.ToTable("NotificationDeliveries", "notify");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Destination).HasMaxLength(320);
        builder.Property(x => x.FailureReason).HasMaxLength(2000);
        b.HasIndex(x => new { x.RegistrationId, x.CreatedUtc });
    }
}
