using CustomerService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace CustomerService.Infrastructure.Data.Persistence.Configurations;

public sealed class NotificationTemplateConfiguration:IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("NotificationTemplates","notify");
        builder.HasKey(x=>x.Id);
        builder.Property(x=>x.Code).HasMaxLength(100);
        builder.Property(x=>x.Name).HasMaxLength(200);
        builder.Property(x=>x.SubjectTemplate).HasMaxLength(500);
        builder.HasIndex(x=>new{x.Code,x.Channel,x.IsActive}).IsUnique();
    }
}
