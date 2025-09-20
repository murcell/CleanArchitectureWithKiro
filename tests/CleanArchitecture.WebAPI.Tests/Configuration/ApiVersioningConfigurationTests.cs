using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using CleanArchitecture.WebAPI.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CleanArchitecture.WebAPI.Tests.Configuration;

/// <summary>
/// Unit tests for API versioning configuration
/// </summary>
public class ApiVersioningConfigurationTests
{
    [Fact]
    public void AddApiVersioningConfiguration_Should_Register_Required_Services()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddApiVersioningConfiguration();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        // Verify API versioning services are registered
        Assert.NotNull(serviceProvider.GetService<IApiVersionDescriptionProvider>());
        
        // Verify that the service collection contains the expected services
        Assert.Contains(services, s => s.ServiceType == typeof(IApiVersionDescriptionProvider));
    }

    [Fact]
    public void AddApiVersioningConfiguration_Should_Configure_Default_Version()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddApiVersioningConfiguration();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<ApiVersioningOptions>>();
        
        Assert.NotNull(options);
        Assert.Equal(new ApiVersion(1, 0), options.Value.DefaultApiVersion);
        Assert.True(options.Value.AssumeDefaultVersionWhenUnspecified);
    }

    [Fact]
    public void AddApiVersioningConfiguration_Should_Configure_Version_Readers()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddApiVersioningConfiguration();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<ApiVersioningOptions>>();
        
        Assert.NotNull(options);
        Assert.NotNull(options.Value.ApiVersionReader);
        Assert.NotNull(options.Value.ApiVersionReader);
    }

    [Fact]
    public void AddApiVersioningConfiguration_Should_Enable_Version_Reporting()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddApiVersioningConfiguration();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<ApiVersioningOptions>>();
        
        Assert.NotNull(options);
        Assert.True(options.Value.ReportApiVersions);
    }
}

/// <summary>
/// Unit tests for ConfigureSwaggerOptions
/// </summary>
public class ConfigureSwaggerOptionsTests
{
    [Fact]
    public void Configure_Should_Add_Swagger_Doc_For_Each_Version()
    {
        // Arrange
        var mockProvider = new MockApiVersionDescriptionProvider();
        var configureOptions = new ConfigureSwaggerOptions(mockProvider);
        var swaggerOptions = new Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions();

        // Act
        configureOptions.Configure(swaggerOptions);

        // Assert
        Assert.Equal(2, swaggerOptions.SwaggerGeneratorOptions.SwaggerDocs.Count);
        Assert.True(swaggerOptions.SwaggerGeneratorOptions.SwaggerDocs.ContainsKey("v1"));
        Assert.True(swaggerOptions.SwaggerGeneratorOptions.SwaggerDocs.ContainsKey("v2"));
    }

    [Fact]
    public void Configure_Should_Set_Correct_Title_And_Version()
    {
        // Arrange
        var mockProvider = new MockApiVersionDescriptionProvider();
        var configureOptions = new ConfigureSwaggerOptions(mockProvider);
        var swaggerOptions = new Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions();

        // Act
        configureOptions.Configure(swaggerOptions);

        // Assert
        var v1Doc = swaggerOptions.SwaggerGeneratorOptions.SwaggerDocs["v1"];
        var v2Doc = swaggerOptions.SwaggerGeneratorOptions.SwaggerDocs["v2"];

        Assert.Equal("Clean Architecture API", v1Doc.Title);
        Assert.Equal("1.0", v1Doc.Version);
        Assert.Equal("Clean Architecture API", v2Doc.Title);
        Assert.Equal("2.0", v2Doc.Version);
    }

    [Fact]
    public void Configure_Should_Mark_Deprecated_Versions()
    {
        // Arrange
        var mockProvider = new MockApiVersionDescriptionProvider(includeDeprecated: true);
        var configureOptions = new ConfigureSwaggerOptions(mockProvider);
        var swaggerOptions = new Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions();

        // Act
        configureOptions.Configure(swaggerOptions);

        // Assert
        var deprecatedDoc = swaggerOptions.SwaggerGeneratorOptions.SwaggerDocs["v1"];
        Assert.Contains("deprecated", deprecatedDoc.Description, StringComparison.OrdinalIgnoreCase);
    }

    // Mock implementation for testing
    private class MockApiVersionDescriptionProvider : IApiVersionDescriptionProvider
    {
        private readonly bool _includeDeprecated;

        public MockApiVersionDescriptionProvider(bool includeDeprecated = false)
        {
            _includeDeprecated = includeDeprecated;
        }

        public IReadOnlyList<ApiVersionDescription> ApiVersionDescriptions => new List<ApiVersionDescription>
        {
            new(new ApiVersion(1, 0), "v1", _includeDeprecated),
            new(new ApiVersion(2, 0), "v2", false)
        };
    }
}