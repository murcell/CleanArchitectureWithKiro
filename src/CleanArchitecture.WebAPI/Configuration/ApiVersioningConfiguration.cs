using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.OpenApi.Models;

namespace CleanArchitecture.WebAPI.Configuration;

/// <summary>
/// Configuration for API versioning
/// </summary>
public static class ApiVersioningConfiguration
{
    /// <summary>
    /// Adds API versioning services to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddApiVersioningConfiguration(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            // Default API version
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;

            // API version reading strategies
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),           // /api/v1/users
                new QueryStringApiVersionReader("version"), // ?version=1.0
                new HeaderApiVersionReader("X-Version"),    // X-Version: 1.0
                new MediaTypeApiVersionReader("ver")        // Accept: application/json;ver=1.0
            );

            // Reporting API versions
            options.ReportApiVersions = true;
        })
        .AddMvc()
        .AddApiExplorer(options =>
        {
            // Add the versioned API explorer, which also adds IApiVersionDescriptionProvider service
            // Note: the specified format code will format the version as "'v'major[.minor][-status]"
            options.GroupNameFormat = "'v'VVV";

            // Automatically substitute version in controller names
            options.SubstituteApiVersionInUrl = true;
        });

        // Configure Swagger to support multiple API versions
        services.ConfigureOptions<ConfigureSwaggerOptions>();

        return services;
    }
}

/// <summary>
/// Configures Swagger options for API versioning
/// </summary>
public class ConfigureSwaggerOptions : Microsoft.Extensions.Options.IConfigureOptions<Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;

    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
    {
        _provider = provider;
    }

    public void Configure(Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions options)
    {
        // Add a swagger document for each discovered API version
        foreach (var description in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, CreateInfoForApiVersion(description));
        }
    }

    private static OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
    {
        var info = new OpenApiInfo
        {
            Title = "Clean Architecture API",
            Version = description.ApiVersion.ToString(),
            Description = "A comprehensive Clean Architecture implementation with .NET 9",
            Contact = new OpenApiContact
            {
                Name = "Clean Architecture Team",
                Email = "support@cleanarchitecture.com",
                Url = new Uri("https://github.com/cleanarchitecture/api")
            },
            License = new OpenApiLicense
            {
                Name = "MIT License",
                Url = new Uri("https://opensource.org/licenses/MIT")
            }
        };

        if (description.IsDeprecated)
        {
            info.Description += " (This API version has been deprecated)";
        }

        return info;
    }
}