using CustomerService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace CustomerService.Infrastructure.Data.Persistence.Configurations;

public sealed class OtpChallengeConfiguration : IEntityTypeConfiguration<OtpChallenge>
{ 
    public void Configure(EntityTypeBuilder<OtpChallenge>b)
    {
        b.ToTable("OtpChallenges","reg");
        b.HasKey(x=>x.Id);b.Property(x=>x.CodeHash).HasMaxLength(128);
        b.Property(x=>x.Salt).HasMaxLength(128);
        b.HasIndex(x=>new{x.RegistrationId,x.Channel,x.CreatedUtc});
    }
}
