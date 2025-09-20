using Microsoft.Extensions.Options;
using CleanArchitecture.WebAPI.Middleware;

namespace CleanArchitecture.WebAPI.Configuration;

/// <summary>
/// Extension methods for configuring WebAPI layer services
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds WebAPI layer services to the DI container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddWebAPI(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure options
        services.AddWebAPIOptions(configuration);

        // Add core web services
        services.AddWebServices();
        services.AddAuthenticationServices(configuration);
        services.AddMiddlewareServices();
        services.AddDocumentationServices(configuration);

        return services;
    }

    /// <summary>
    /// Adds WebAPI layer options configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddWebAPIOptions(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure and validate API options
        services.Configure<ApiOptions>(configuration.GetSection(ApiOptions.SectionName));
        services.AddSingleton<IValidateOptions<ApiOptions>, ApiOptionsValidator>();

        // Configure and validate Logging options
        services.Configure<LoggingOptions>(configuration.GetSection(LoggingOptions.SectionName));
        services.AddSingleton<IValidateOptions<LoggingOptions>, LoggingOptionsValidator>();

        return services;
    }

    /// <summary>
    /// Adds core web services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddWebServices(this IServiceCollection services)
    {
        // Add controllers with proper configuration
        services.AddControllers(options =>
        {
            // Add custom filters if needed
        });

        // Add HTTP context accessor for correlation ID and other services
        services.AddHttpContextAccessor();

        return services;
    }

    /// <summary>
    /// Adds authentication and authorization services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add JWT authentication
        services.AddJwtAuthentication(configuration);
        
        // Add authorization policies
        services.AddAuthorizationPolicies();

        return services;
    }

    /// <summary>
    /// Adds middleware services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddMiddlewareServices(this IServiceCollection services)
    {
        // Register middleware services with appropriate lifetimes
        services.AddScoped<ICorrelationIdService, CorrelationIdService>();

        // Register configuration helper
        services.AddSingleton<IConfigurationHelper, ConfigurationHelper>();

        // Register logging enricher
        services.AddScoped<WebAPI.Logging.ApplicationEnricher>();

        return services;
    }

    /// <summary>
    /// Adds documentation services (Swagger, API versioning)
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddDocumentationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var apiOptions = configuration.GetSection(ApiOptions.SectionName).Get<ApiOptions>() ?? new ApiOptions();

        if (apiOptions.EnableVersioning)
        {
            services.AddApiVersioningConfiguration();
        }

        if (apiOptions.EnableSwagger)
        {
            services.AddSwaggerDocumentation(configuration);
        }

        // Add OpenAPI
        services.AddOpenApi();

        return services;
    }

    /// <summary>
    /// Adds CORS services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCorsServices(this IServiceCollection services, IConfiguration configuration)
    {
        var apiOptions = configuration.GetSection(ApiOptions.SectionName).Get<ApiOptions>() ?? new ApiOptions();

        if (apiOptions.EnableCors)
        {
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    if (apiOptions.AllowedOrigins?.Any() == true)
                    {
                        builder.WithOrigins(apiOptions.AllowedOrigins);
                    }
                    else
                    {
                        builder.AllowAnyOrigin();
                    }

                    builder.AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });
        }

        return services;
    }

    /// <summary>
    /// Adds configuration validation services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddConfigurationValidation(this IServiceCollection services)
    {
        // Add configuration validation as a hosted service
        services.AddHostedService<ConfigurationValidationService>();

        return services;
    }
}