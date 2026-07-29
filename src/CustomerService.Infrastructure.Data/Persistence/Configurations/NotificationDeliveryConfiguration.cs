using CustomerService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerService.Infrastructure.Data.Persistence.Configurations;

public sealed class NotificationDeliveryConfiguration : IEntityTypeConfiguration<NotificationDelivery>
{
    public void Configure(EntityTypeBuilder<NotificationDelivery> builder)
    {
        builder.ToTable("NotificationDeliveries", "notify");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Destination).HasMaxLength(320).IsRequired();
        builder.Property(x => x.TemplateCode).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ProviderMessageId).HasMaxLength(200);
        builder.Property(x => x.FailureReason).HasMaxLength(2000);
        builder.HasIndex(x => new { x.RegistrationId, x.Channel, x.CreatedUtc });
        builder.HasIndex(x => x.Status);
    }
}
