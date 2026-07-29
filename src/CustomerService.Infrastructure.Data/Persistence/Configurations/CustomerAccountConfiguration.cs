using CustomerService.Domain.Entities; using Microsoft.EntityFrameworkCore; using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace CustomerService.Infrastructure.Data.Persistence.Configurations;
public sealed class CustomerAccountConfiguration : IEntityTypeConfiguration<CustomerAccount> 
{
    public void Configure(EntityTypeBuilder<CustomerAccount> builder) 
    {
        builder.ToTable("CustomerAccounts", "crm");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.RegistrationId).IsUnique(); 
        builder.HasIndex(x => x.LegacyCustomerId);
    }
}
