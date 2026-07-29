using CustomerService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace CustomerService.Infrastructure.Data.Persistence.Configurations;
public sealed class OtpChallengeConfiguration:IEntityTypeConfiguration<OtpChallenge>
{
    public void Configure(EntityTypeBuilder<OtpChallenge> builder)
    {
        builder.ToTable("OtpChallenges","reg");
        builder.HasKey(x=>x.Id);
        builder.Property(x=>x.CodeHash).HasMaxLength(128);
        builder.Property(x=>x.Salt).HasMaxLength(128);
        builder.HasIndex(x=>new{x.RegistrationId,x.Channel,x.CreatedUtc});
    }
}