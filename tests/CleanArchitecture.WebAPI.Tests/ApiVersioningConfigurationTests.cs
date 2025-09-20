using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Asp.Versioning.ApiExplorer;
using CleanArchitecture.WebAPI.Configuration;

namespace CleanArchitecture.WebAPI.Tests;

/// <summary>
/// Tests for API versioning configuration
/// </summary>
public class ApiVersioningConfigurationTests
{
    [Fact]
    public void AddApiVersioningConfiguration_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddApiVersioningConfiguration();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var apiVersionDescriptionProvider = serviceProvider.GetService<IApiVersionDescriptionProvider>();
        
        Assert.NotNull(apiVersionDescriptionProvider);
    }

    [Fact]
    public void ApiVersionDescriptionProvider_ShouldProvideMultipleVersions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddApiVersioningConfiguration();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var provider = serviceProvider.GetRequiredService<IApiVersionDescriptionProvider>();
        var descriptions = provider.ApiVersionDescriptions.ToList();

        // Assert
        Assert.NotEmpty(descriptions);
        Assert.Contains(descriptions, d => d.ApiVersion.ToString() == "1.0");
        Assert.Contains(descriptions, d => d.ApiVersion.ToString() == "2.0");
    }

    [Fact]
    public void ApiVersionDescriptionProvider_ShouldHaveCorrectGroupNameFormat()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddApiVersioningConfiguration();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var provider = serviceProvider.GetRequiredService<IApiVersionDescriptionProvider>();
        var descriptions = provider.ApiVersionDescriptions.ToList();

        // Assert
        Assert.All(descriptions, description =>
        {
            Assert.StartsWith("v", description.GroupName);
        });
    }
}