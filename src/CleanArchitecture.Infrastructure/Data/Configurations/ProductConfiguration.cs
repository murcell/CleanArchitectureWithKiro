using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.ValueObjects;

namespace CleanArchitecture.Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        // Primary Key
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .HasMaxLength(1000)
            .HasDefaultValue(string.Empty);

        // Money Value Object Configuration
        builder.OwnsOne(p => p.Price, price =>
        {
            price.Property(m => m.Amount)
                .HasColumnName("PriceAmount")
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            price.Property(m => m.Currency)
                .HasColumnName("PriceCurrency")
                .IsRequired()
                .HasMaxLength(3)
                .IsFixedLength();
        });

        builder.Property(p => p.Stock)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(p => p.IsAvailable)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(p => p.UserId)
            .IsRequired();

        // Audit Properties
        builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(p => p.CreatedBy)
            .HasMaxLength(100);

        builder.Property(p => p.UpdatedAt)
            .IsRequired(false);

        builder.Property(p => p.UpdatedBy)
            .HasMaxLength(100);

        // Indexes
        builder.HasIndex(p => p.Name)
            .HasDatabaseName("IX_Products_Name");

        builder.HasIndex(p => p.IsAvailable)
            .HasDatabaseName("IX_Products_IsAvailable");

        builder.HasIndex(p => p.UserId)
            .HasDatabaseName("IX_Products_UserId");

        builder.HasIndex(p => p.CreatedAt)
            .HasDatabaseName("IX_Products_CreatedAt");

        builder.HasIndex(p => new { p.UserId, p.IsAvailable })
            .HasDatabaseName("IX_Products_UserId_IsAvailable");

        // Relationships
        builder.HasOne(p => p.User)
            .WithMany(u => u.Products)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore Domain Events (they are not persisted)
        builder.Ignore(p => p.DomainEvents);
    }
}