using AiAdvisor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiAdvisor.Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.ProductId);

        builder.Ignore(p => p.Id);

        builder.Property(p => p.ProductId)
            .ValueGeneratedOnAdd();

        builder.Property(p => p.AutoId)
            .ValueGeneratedOnAdd()
            .UseIdentityColumn();

        builder.Property(p => p.ProductName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.ProductDescription)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(p => p.ProductPrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired(false);

        builder.Property(p => p.ProductType)
            .HasConversion<string>()
            .HasMaxLength(100)
            .IsRequired();
    }
}
