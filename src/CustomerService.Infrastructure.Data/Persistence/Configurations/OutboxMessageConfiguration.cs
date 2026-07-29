using CustomerService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace CustomerService.Infrastructure.Data.Persistence.Configurations;

public sealed class OutboxMessageConfiguration:IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages","integration");
        builder.HasKey(x=>x.Id);
        builder.Property(x=>x.Destination).HasMaxLength(320);
        builder.Property(x=>x.TemplateCode).HasMaxLength(100);
        builder.Property(x=>x.ProviderMessageId).HasMaxLength(200);
        builder.HasIndex(x=>new{x.Status,x.NextAttemptUtc,x.CreatedUtc});
    }
}
