using CustomerService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace CustomerService.Infrastructure.Data.Persistence.Configurations;

public sealed class RegistrationConfiguration : IEntityTypeConfiguration<RegistrationApplication>
{
    public void Configure(EntityTypeBuilder<RegistrationApplication> b)
    {
        b.ToTable("RegistrationApplications", "reg");
        b.HasKey(x => x.Id);
        b.Property(x => x.Email).HasMaxLength(320).IsRequired();
        b.Property(x => x.MobileNumber).HasMaxLength(30).IsRequired();
        b.Property(x => x.NationalId).HasMaxLength(100);
        b.Property(x => x.FullName).HasMaxLength(200);
        b.Property(x => x.PinHash).HasMaxLength(128);
        b.Property(x => x.PinSalt).HasMaxLength(128);
        b.Ignore(x => x.HasPin);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => x.Email);
        b.HasIndex(x => x.MobileNumber);
    }
}
