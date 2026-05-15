using AiAdvisor.Domain.Entities;
using AiAdvisor.Domain.Enums;
using AiAdvisor.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiAdvisor.Infrastructure.Data.Configurations;

public class UserTransactionConfiguration : IEntityTypeConfiguration<UserTransaction>
{
    public void Configure(EntityTypeBuilder<UserTransaction> builder)
    {
        builder.HasKey(t => t.TransactionId);

        builder.Ignore(t => t.Id);

        builder.Property(t => t.TransactionId)
            .ValueGeneratedOnAdd();

        builder.Property(t => t.UserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(t => t.ProductId)
            .IsRequired();

        builder.Property(t => t.From)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(t => t.To)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(t => t.TransactionType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.TransactionCategory)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Product)
            .WithMany()
            .HasForeignKey(t => t.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}