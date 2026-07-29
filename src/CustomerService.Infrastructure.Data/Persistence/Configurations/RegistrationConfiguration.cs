using CustomerService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace CustomerService.Infrastructure.Data.Persistence.Configurations;

public sealed class RegistrationConfiguration : IEntityTypeConfiguration<RegistrationApplication>
{
    public void Configure(EntityTypeBuilder<RegistrationApplication> builder)
    {
        builder.ToTable("RegistrationApplications", "reg");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Email).HasMaxLength(320).IsRequired();
        builder.Property(x => x.MobileNumber).HasMaxLength(30).IsRequired();
        builder.Property(x => x.NationalId).HasMaxLength(100);
        builder.Property(x => x.FullName).HasMaxLength(200);
        builder.Property(x => x.PinHash).HasMaxLength(128);
        builder.Property(x => x.PinSalt).HasMaxLength(128);
        //builder.Ignore(x => x.HasPin);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => x.Email);
        builder.HasIndex(x => x.MobileNumber);
    }
}
