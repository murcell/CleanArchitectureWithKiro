using Asp.Versioning.ApiExplorer;
using Microsoft.OpenApi.Models;
using System.Reflection;

namespace CleanArchitecture.WebAPI.Configuration;

/// <summary>
/// Configuration for Swagger/OpenAPI documentation
/// </summary>
public static class SwaggerConfiguration
{
    /// <summary>
    /// Adds Swagger services to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSwaggerGen(options =>
        {
            // Set the comments path for the Swagger JSON and UI
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            // Add security definition for JWT Bearer tokens
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Configure for API versioning
            options.DocInclusionPredicate((version, desc) => true);
            options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

            // Custom operation filters
            options.OperationFilter<SwaggerDefaultValues>();
        });

        return services;
    }

    /// <summary>
    /// Configures Swagger middleware in the application pipeline
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <param name="provider">API version description provider</param>
    /// <returns>Application builder for chaining</returns>
    public static IApplicationBuilder UseSwaggerDocumentation(this IApplicationBuilder app, IApiVersionDescriptionProvider provider)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            // Build a swagger endpoint for each discovered API version
            foreach (var description in provider.ApiVersionDescriptions)
            {
                options.SwaggerEndpoint(
                    $"/swagger/{description.GroupName}/swagger.json",
                    $"Clean Architecture API {description.GroupName.ToUpperInvariant()}");
            }

            options.RoutePrefix = "swagger";
            options.DocumentTitle = "Clean Architecture API Documentation";
            options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
            options.DefaultModelsExpandDepth(2);
            options.DefaultModelExpandDepth(2);
            options.DisplayRequestDuration();
            options.EnableDeepLinking();
            options.EnableFilter();
            options.ShowExtensions();
            options.EnableValidator();
        });

        return app;
    }
}

/// <summary>
/// Swagger operation filter to set default values for API versioning
/// </summary>
public class SwaggerDefaultValues : Swashbuckle.AspNetCore.SwaggerGen.IOperationFilter
{
    public void Apply(OpenApiOperation operation, Swashbuckle.AspNetCore.SwaggerGen.OperationFilterContext context)
    {
        var apiDescription = context.ApiDescription;

        // Check if the API version is deprecated
        if (context.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<Asp.Versioning.ApiVersionAttribute>()
            .Any(attr => attr.Deprecated))
        {
            operation.Deprecated = true;
        }

        foreach (var responseType in context.ApiDescription.SupportedResponseTypes)
        {
            var responseKey = responseType.IsDefaultResponse ? "default" : responseType.StatusCode.ToString();
            var response = operation.Responses[responseKey];

            foreach (var contentType in response.Content.Keys)
            {
                if (responseType.ApiResponseFormats.All(x => x.MediaType != contentType))
                {
                    response.Content.Remove(contentType);
                }
            }
        }

        if (operation.Parameters == null)
        {
            return;
        }

        foreach (var parameter in operation.Parameters)
        {
            var description = apiDescription.ParameterDescriptions.First(p => p.Name == parameter.Name);

            parameter.Description ??= description.ModelMetadata?.Description;

            if (parameter.Schema.Default == null && description.DefaultValue != null)
            {
                var jsonValue = System.Text.Json.JsonSerializer.Serialize(description.DefaultValue);
                parameter.Schema.Default = new Microsoft.OpenApi.Any.OpenApiString(jsonValue);
            }

            parameter.Required |= description.IsRequired;
        }
    }
}