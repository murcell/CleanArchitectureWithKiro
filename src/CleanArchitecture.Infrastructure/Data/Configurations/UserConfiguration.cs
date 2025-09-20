using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.ValueObjects;

namespace CleanArchitecture.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        // Primary Key
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id)
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(u => u.Name)
            .IsRequired()
            .HasMaxLength(100);

        // Email Value Object Configuration
        builder.OwnsOne(u => u.Email, email =>
        {
            email.Property(e => e.Value)
                .HasColumnName("Email")
                .IsRequired()
                .HasMaxLength(255);
                
            email.HasIndex(e => e.Value)
                .IsUnique()
                .HasDatabaseName("IX_Users_Email");
        });

        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(u => u.LastLoginAt)
            .IsRequired(false);

        // Authentication Properties
        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(u => u.RefreshToken)
            .HasMaxLength(500);

        builder.Property(u => u.RefreshTokenExpiryTime)
            .IsRequired(false);

        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(u => u.IsEmailConfirmed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.EmailConfirmationToken)
            .HasMaxLength(500);

        builder.Property(u => u.EmailConfirmationTokenExpiryTime)
            .IsRequired(false);

        builder.Property(u => u.PasswordResetToken)
            .HasMaxLength(500);

        builder.Property(u => u.PasswordResetTokenExpiryTime)
            .IsRequired(false);

        builder.Property(u => u.FailedLoginAttempts)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(u => u.LockoutEnd)
            .IsRequired(false);

        // Audit Properties
        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(u => u.CreatedBy)
            .HasMaxLength(100);

        builder.Property(u => u.UpdatedAt)
            .IsRequired(false);

        builder.Property(u => u.UpdatedBy)
            .HasMaxLength(100);

        // Indexes

        builder.HasIndex(u => u.IsActive)
            .HasDatabaseName("IX_Users_IsActive");

        builder.HasIndex(u => u.CreatedAt)
            .HasDatabaseName("IX_Users_CreatedAt");

        builder.HasIndex(u => u.RefreshToken)
            .HasDatabaseName("IX_Users_RefreshToken");

        builder.HasIndex(u => u.EmailConfirmationToken)
            .HasDatabaseName("IX_Users_EmailConfirmationToken");

        builder.HasIndex(u => u.PasswordResetToken)
            .HasDatabaseName("IX_Users_PasswordResetToken");

        builder.HasIndex(u => u.Role)
            .HasDatabaseName("IX_Users_Role");

        // Relationships
        builder.HasMany(u => u.Products)
            .WithOne(p => p.User)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore Domain Events (they are not persisted)
        builder.Ignore(u => u.DomainEvents);
    }
}