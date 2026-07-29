using CustomerService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace CustomerService.Infrastructure.Data.Persistence.Configurations;

public sealed class NotificationTemplateConfiguration:IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate>b)
    {
        b.ToTable("NotificationTemplates","notify");
        b.HasKey(x=>x.Id);
        b.Property(x=>x.Code).HasMaxLength(100);
        b.Property(x=>x.Name).HasMaxLength(200);
        b.Property(x=>x.SubjectTemplate).HasMaxLength(500);
        b.HasIndex(x=>new{x.Code,x.Channel,x.IsActive}).IsUnique();
    }
}
