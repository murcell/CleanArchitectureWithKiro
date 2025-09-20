using CleanArchitecture.Application.Common.Mappings;
using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Application.DTOs.Requests;

namespace CleanArchitecture.Application.Tests.Common.Mappings;

public class MappingProfileTests
{
    [Fact]
    public void MappingProfile_ShouldBeCreatable()
    {
        // Arrange & Act
        var profile = new MappingProfile();

        // Assert
        Assert.NotNull(profile);
    }

    [Fact]
    public void MappingProfile_ShouldHaveCorrectProfileName()
    {
        // Arrange
        var profile = new MappingProfile();

        // Act
        var profileName = profile.ProfileName;

        // Assert
        Assert.Contains("MappingProfile", profileName);
    }

    [Fact]
    public void CreateUserRequest_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var request = new CreateUserRequest
        {
            Name = "Test User",
            Email = "test@example.com"
        };

        // Assert
        Assert.NotNull(request);
        Assert.Equal("Test User", request.Name);
        Assert.Equal("test@example.com", request.Email);
    }

    [Fact]
    public void UpdateUserRequest_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var request = new UpdateUserRequest
        {
            Name = "Updated User",
            Email = "updated@example.com"
        };

        // Assert
        Assert.NotNull(request);
        Assert.Equal("Updated User", request.Name);
        Assert.Equal("updated@example.com", request.Email);
    }

    [Fact]
    public void CreateProductRequest_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var request = new CreateProductRequest
        {
            Name = "Test Product",
            Description = "Test Description",
            Price = 99.99m,
            Currency = "USD",
            Stock = 10,
            UserId = 1
        };

        // Assert
        Assert.NotNull(request);
        Assert.Equal("Test Product", request.Name);
        Assert.Equal("Test Description", request.Description);
        Assert.Equal(99.99m, request.Price);
        Assert.Equal("USD", request.Currency);
        Assert.Equal(10, request.Stock);
        Assert.Equal(1, request.UserId);
    }

    [Fact]
    public void UpdateProductRequest_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var request = new UpdateProductRequest
        {
            Name = "Updated Product",
            Description = "Updated Description",
            Price = 149.99m,
            Currency = "EUR",
            Stock = 5
        };

        // Assert
        Assert.NotNull(request);
        Assert.Equal("Updated Product", request.Name);
        Assert.Equal("Updated Description", request.Description);
        Assert.Equal(149.99m, request.Price);
        Assert.Equal("EUR", request.Currency);
        Assert.Equal(5, request.Stock);
    }

    [Fact]
    public void UserDto_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var dto = new UserDto
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            IsActive = true,
            LastLoginAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(1, dto.Id);
        Assert.Equal("Test User", dto.Name);
        Assert.Equal("test@example.com", dto.Email);
        Assert.True(dto.IsActive);
        Assert.NotNull(dto.LastLoginAt);
        Assert.NotNull(dto.Products);
    }

    [Fact]
    public void ProductDto_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var dto = new ProductDto
        {
            Id = 1,
            Name = "Test Product",
            Description = "Test Description",
            Price = 99.99m,
            Currency = "USD",
            Stock = 10,
            IsAvailable = true,
            UserId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(1, dto.Id);
        Assert.Equal("Test Product", dto.Name);
        Assert.Equal("Test Description", dto.Description);
        Assert.Equal(99.99m, dto.Price);
        Assert.Equal("USD", dto.Currency);
        Assert.Equal(10, dto.Stock);
        Assert.True(dto.IsAvailable);
        Assert.Equal(1, dto.UserId);
    }
}