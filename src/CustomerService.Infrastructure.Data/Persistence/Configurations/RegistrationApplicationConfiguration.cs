using CustomerService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerService.Infrastructure.Data.Persistence.Configurations;

public sealed class RegistrationApplicationConfiguration : IEntityTypeConfiguration<RegistrationApplication>
{
    public void Configure(EntityTypeBuilder<RegistrationApplication> builder)
    {
        builder.ToTable("RegistrationApplications", "reg"); builder.HasKey(x => x.Id);
        builder.Property(x => x.Email).HasMaxLength(320).IsRequired(); builder.Property(x => x.NormalizedEmail).HasMaxLength(320).IsRequired();
        builder.Property(x => x.MobileNumber).HasMaxLength(30).IsRequired(); builder.Property(x => x.NormalizedMobileNumber).HasMaxLength(30).IsRequired();
        builder.Property(x => x.PinHash).HasMaxLength(256); builder.Property(x => x.PinSalt).HasMaxLength(256); builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => x.NormalizedEmail); builder.HasIndex(x => x.NormalizedMobileNumber);
    }
}
