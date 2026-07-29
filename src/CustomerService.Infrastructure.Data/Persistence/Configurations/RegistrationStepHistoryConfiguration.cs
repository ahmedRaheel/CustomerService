using CustomerService.Domain.Entities; using Microsoft.EntityFrameworkCore; using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace CustomerService.Infrastructure.Data.Persistence.Configurations;
public sealed class RegistrationStepHistoryConfiguration : IEntityTypeConfiguration<RegistrationStepHistory> 
{
    public void Configure(EntityTypeBuilder<RegistrationStepHistory> builder)
    { 
        builder.ToTable("RegistrationStepHistory", "reg");
        builder.HasKey(x => x.Id); 
        builder.HasIndex(x => new { x.RegistrationId, x.OccurredUtc });
    }
}
