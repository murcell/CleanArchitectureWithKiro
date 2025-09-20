using Xunit;
using CleanArchitecture.WebAPI.Configuration;
using Microsoft.OpenApi.Models;

namespace CleanArchitecture.WebAPI.Tests;

/// <summary>
/// Manual verification tests for API documentation and versioning
/// These tests verify the configuration classes work correctly
/// </summary>
public class ManualApiDocumentationVerification
{
    [Fact]
    public void ConfigureSwaggerOptions_ShouldCreateCorrectApiInfo()
    {
        // Arrange
        var mockProvider = new MockApiVersionDescriptionProvider();
        var configureOptions = new ConfigureSwaggerOptions(mockProvider);
        var swaggerGenOptions = new Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions();

        // Act
        configureOptions.Configure(swaggerGenOptions);

        // Assert
        Assert.NotEmpty(swaggerGenOptions.SwaggerGeneratorOptions.SwaggerDocs);
        Assert.True(swaggerGenOptions.SwaggerGeneratorOptions.SwaggerDocs.ContainsKey("v1"));
        Assert.True(swaggerGenOptions.SwaggerGeneratorOptions.SwaggerDocs.ContainsKey("v2"));

        var v1Doc = swaggerGenOptions.SwaggerGeneratorOptions.SwaggerDocs["v1"];
        Assert.Equal("Clean Architecture API", v1Doc.Title);
        Assert.Equal("1.0", v1Doc.Version);

        var v2Doc = swaggerGenOptions.SwaggerGeneratorOptions.SwaggerDocs["v2"];
        Assert.Equal("Clean Architecture API", v2Doc.Title);
        Assert.Equal("2.0", v2Doc.Version);
    }

    [Fact]
    public void SwaggerDefaultValues_OperationFilter_ShouldExist()
    {
        // Arrange & Act
        var filter = new SwaggerDefaultValues();

        // Assert
        Assert.NotNull(filter);
        Assert.True(filter is Swashbuckle.AspNetCore.SwaggerGen.IOperationFilter);
    }
}

/// <summary>
/// Mock implementation of IApiVersionDescriptionProvider for testing
/// </summary>
public class MockApiVersionDescriptionProvider : Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider
{
    public IReadOnlyList<Asp.Versioning.ApiExplorer.ApiVersionDescription> ApiVersionDescriptions { get; }

    public MockApiVersionDescriptionProvider()
    {
        ApiVersionDescriptions = new List<Asp.Versioning.ApiExplorer.ApiVersionDescription>
        {
            new Asp.Versioning.ApiExplorer.ApiVersionDescription(new Asp.Versioning.ApiVersion(1, 0), "v1", false),
            new Asp.Versioning.ApiExplorer.ApiVersionDescription(new Asp.Versioning.ApiVersion(2, 0), "v2", false)
        };
    }
}