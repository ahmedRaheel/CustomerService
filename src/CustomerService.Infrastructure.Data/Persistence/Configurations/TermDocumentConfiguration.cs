using CustomerService.Domain.Entities; using Microsoft.EntityFrameworkCore; using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace CustomerService.Infrastructure.Data.Persistence.Configurations;
public sealed class TermDocumentConfiguration : IEntityTypeConfiguration<TermDocument> 
{ 
    public void Configure(EntityTypeBuilder<TermDocument> builder)
    { 
        builder.ToTable("TermDocuments", "reg");
        builder.HasKey(x => x.Id); 
        builder.Property(x => x.Code).HasMaxLength(100);
        builder.Property(x => x.Version).HasMaxLength(50);
        builder.Property(x => x.Content).HasColumnType("nvarchar(max)");
        builder.HasIndex(x => new { x.Code, x.Version }).IsUnique();
    }
}
