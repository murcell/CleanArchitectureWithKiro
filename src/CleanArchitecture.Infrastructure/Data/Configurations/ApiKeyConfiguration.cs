using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CleanArchitecture.Domain.Entities;
using System.Text.Json;

namespace CleanArchitecture.Infrastructure.Data.Configurations;

public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("ApiKeys");

        // Primary Key
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.KeyHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.KeyPrefix)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(a => a.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(a => a.ExpiresAt)
            .IsRequired(false);

        builder.Property(a => a.LastUsedAt)
            .IsRequired(false);

        builder.Property(a => a.Description)
            .HasMaxLength(500);

        // Scopes as JSON
        builder.Property(a => a.Scopes)
            .IsRequired()
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<string[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<string>())
            .HasColumnType("nvarchar(max)");

        // Audit Properties
        builder.Property(a => a.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(a => a.CreatedBy)
            .HasMaxLength(100);

        builder.Property(a => a.UpdatedAt)
            .IsRequired(false);

        builder.Property(a => a.UpdatedBy)
            .HasMaxLength(100);

        // Indexes
        builder.HasIndex(a => a.KeyHash)
            .IsUnique()
            .HasDatabaseName("IX_ApiKeys_KeyHash");

        builder.HasIndex(a => a.KeyPrefix)
            .HasDatabaseName("IX_ApiKeys_KeyPrefix");

        builder.HasIndex(a => a.IsActive)
            .HasDatabaseName("IX_ApiKeys_IsActive");

        builder.HasIndex(a => a.ExpiresAt)
            .HasDatabaseName("IX_ApiKeys_ExpiresAt");

        builder.HasIndex(a => a.UserId)
            .HasDatabaseName("IX_ApiKeys_UserId");

        builder.HasIndex(a => a.CreatedAt)
            .HasDatabaseName("IX_ApiKeys_CreatedAt");

        // Relationships
        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Ignore Domain Events (they are not persisted)
        builder.Ignore(a => a.DomainEvents);
    }
}