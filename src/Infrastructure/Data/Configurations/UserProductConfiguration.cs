using AiAdvisor.Domain.Entities;
using AiAdvisor.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiAdvisor.Infrastructure.Data.Configurations;

public class UserProductConfiguration : IEntityTypeConfiguration<UserProduct>
{
    public void Configure(EntityTypeBuilder<UserProduct> builder)
    {
        builder.HasKey(up => up.Id);

        builder.Property(up => up.ProductId)
            .IsRequired();

        builder.Property(up => up.UserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(up => up.AvailableBalance)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(up => up.IsActive)
            .IsRequired();

        builder.Property(up => up.CardNumber)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(up => up.AccountNumber)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.HasOne(up => up.Product)
            .WithMany()
            .HasForeignKey(up => up.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(up => up.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
