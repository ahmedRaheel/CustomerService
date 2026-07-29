using CustomerService.Domain.Entities; 
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace CustomerService.Infrastructure.Data.Persistence.Configurations;
public sealed class OtpVerificationAttemptConfiguration : IEntityTypeConfiguration<OtpVerificationAttempt> 
{
    public void Configure(EntityTypeBuilder<OtpVerificationAttempt> builder)
    {
        builder.ToTable("OtpVerificationAttempts", "reg");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.OtpChallengeId, x.SubmittedUtc });
    }
}
