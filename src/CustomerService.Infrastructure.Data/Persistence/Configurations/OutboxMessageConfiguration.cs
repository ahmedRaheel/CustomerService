using CustomerService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace CustomerService.Infrastructure.Data.Persistence.Configurations;

public sealed class OutboxMessageConfiguration:IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage>b)
    {
        b.ToTable("OutboxMessages","integration");
        b.HasKey(x=>x.Id);
        b.Property(x=>x.Destination).HasMaxLength(320);
        b.Property(x=>x.TemplateCode).HasMaxLength(100);
        b.Property(x=>x.ProviderMessageId).HasMaxLength(200);
        b.HasIndex(x=>new{x.Status,x.NextAttemptUtc,x.CreatedUtc});
    }
}
