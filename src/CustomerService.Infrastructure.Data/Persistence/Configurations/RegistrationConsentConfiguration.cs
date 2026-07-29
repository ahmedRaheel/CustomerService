using CustomerService.Domain.Entities; using Microsoft.EntityFrameworkCore; using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace CustomerService.Infrastructure.Data.Persistence.Configurations;
public sealed class RegistrationConsentConfiguration : IEntityTypeConfiguration<RegistrationConsent> 
{
    public void Configure(EntityTypeBuilder<RegistrationConsent> builder)
    {
        builder.ToTable("RegistrationConsents", "reg");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.RegistrationId, x.TermDocumentId, x.TermVersion }).IsUnique();
    }
}
