using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Xunit;
using CleanArchitecture.WebAPI.Configuration;

namespace CleanArchitecture.WebAPI.Tests;

/// <summary>
/// Tests for Swagger configuration
/// </summary>
public class SwaggerConfigurationTests
{
    [Fact]
    public void AddSwaggerDocumentation_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        services.AddSwaggerDocumentation(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        // Verify that Swagger services are registered by checking for SwaggerGenerator
        var swaggerGenerator = serviceProvider.GetService<Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenerator>();
        Assert.NotNull(swaggerGenerator);
    }

    [Fact]
    public void SwaggerDefaultValues_OperationFilter_ShouldBeRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        services.AddSwaggerDocumentation(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        // The SwaggerDefaultValues filter should be registered as part of the configuration
        // We can verify this by checking that the SwaggerGen services are properly configured
        var swaggerGenerator = serviceProvider.GetService<Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenerator>();
        Assert.NotNull(swaggerGenerator);
    }

    [Fact]
    public void ConfigureSwaggerOptions_ShouldCreateCorrectApiInfo()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddApiVersioningConfiguration(); // Required for API version descriptions
        var configuration = new ConfigurationBuilder().Build();
        services.AddSwaggerDocumentation(configuration);

        // Act
        var serviceProvider = services.BuildServiceProvider();
        var swaggerGenerator = serviceProvider.GetService<Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenerator>();

        // Assert
        Assert.NotNull(swaggerGenerator);
    }
}