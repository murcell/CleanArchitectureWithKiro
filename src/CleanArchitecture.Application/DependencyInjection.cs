using System.Reflection;
using CleanArchitecture.Application.Common.Behaviors;
using CleanArchitecture.Application.Common.Mappings;
using CleanArchitecture.Application.Common.Validators;
using CleanArchitecture.Application.Configuration;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CleanArchitecture.Application;

/// <summary>
/// Extension methods for configuring Application layer services
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Application layer services to the DI container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure options
        services.AddApplicationOptions(configuration);

        // Add core services
        services.AddMediatRServices();
        services.AddValidationServices();
        services.AddMappingServices();

        return services;
    }

    /// <summary>
    /// Adds Application layer options configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddApplicationOptions(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure and validate Application options
        services.Configure<ApplicationOptions>(configuration.GetSection(ApplicationOptions.SectionName));
        services.AddSingleton<IValidateOptions<ApplicationOptions>, ApplicationOptionsValidator>();

        return services;
    }

    /// <summary>
    /// Adds MediatR services and pipeline behaviors
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddMediatRServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
        });

        // Register MediatR pipeline behaviors with proper order
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        
        return services;
    }

    /// <summary>
    /// Adds validation services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddValidationServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register FluentValidation
        services.AddValidatorsFromAssembly(assembly, ServiceLifetime.Scoped);

        // Register validation services with appropriate lifetimes
        services.AddScoped<IValidationContextService, ValidationContextService>();
        services.AddSingleton<IValidationCacheService, InMemoryValidationCacheService>();

        return services;
    }

    /// <summary>
    /// Adds mapping services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddMappingServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register AutoMapper with proper configuration
        services.AddAutoMapper(typeof(MappingProfile));

        return services;
    }

    /// <summary>
    /// Adds enhanced validation behaviors with performance monitoring and caching
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddEnhancedValidation(this IServiceCollection services)
    {
        // Add enhanced validation behaviors
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationPerformanceBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachedValidationBehavior<,>));

        return services;
    }
}