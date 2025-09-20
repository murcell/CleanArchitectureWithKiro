using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using CleanArchitecture.Domain.Interfaces;
using CleanArchitecture.Infrastructure.Data;
using CleanArchitecture.Infrastructure.Data.Repositories;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Infrastructure.Caching;
using CleanArchitecture.Infrastructure.MessageQueue;
using CleanArchitecture.Infrastructure.Configuration;
using CleanArchitecture.Infrastructure.Logging;
using CleanArchitecture.Infrastructure.HealthChecks;
using CleanArchitecture.Infrastructure.Security;
using StackExchange.Redis;

namespace CleanArchitecture.Infrastructure;

/// <summary>
/// Extension methods for configuring Infrastructure layer services
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Infrastructure layer services to the DI container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure options
        services.AddInfrastructureOptions(configuration);

        // Add core infrastructure services
        services.AddDatabaseServices(configuration);
        services.AddRepositoryServices();
        services.AddSecurityServices(configuration);
        services.AddCachingServices(configuration);
        services.AddMessageQueueServices(configuration);
        services.AddLoggingServices();
        services.AddHealthCheckServices(configuration);

        return services;
    }

    /// <summary>
    /// Adds Infrastructure layer options configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddInfrastructureOptions(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure and validate Database options
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.AddSingleton<IValidateOptions<DatabaseOptions>, DatabaseOptionsValidator>();

        // Configure and validate Cache options
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));
        services.AddSingleton<IValidateOptions<CacheOptions>, CacheOptionsValidator>();

        // Configure and validate MessageQueue options
        services.Configure<MessageQueueOptions>(configuration.GetSection(MessageQueueOptions.SectionName));
        services.AddSingleton<IValidateOptions<MessageQueueOptions>, MessageQueueOptionsValidator>();

        return services;
    }

    /// <summary>
    /// Adds database services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Entity Framework with proper configuration
        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            var databaseOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                sqlOptions.CommandTimeout(databaseOptions.CommandTimeout);
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: databaseOptions.MaxRetryCount,
                    maxRetryDelay: TimeSpan.FromSeconds(databaseOptions.MaxRetryDelay),
                    errorNumbersToAdd: null);
            });

            // Configure based on environment
            if (databaseOptions.EnableSensitiveDataLogging)
            {
                options.EnableSensitiveDataLogging();
            }

            if (databaseOptions.EnableDetailedErrors)
            {
                options.EnableDetailedErrors();
            }
        }, ServiceLifetime.Scoped);

        return services;
    }

    /// <summary>
    /// Adds repository services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddRepositoryServices(this IServiceCollection services)
    {
        // Register UnitOfWork with scoped lifetime
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<ApplicationDbContext>());

        // Register generic repository with scoped lifetime
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Register specific repositories with scoped lifetime
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();

        return services;
    }

    /// <summary>
    /// Adds security services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSecurityServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register security services with appropriate lifetimes
        services.AddScoped<IPasswordHasher, Security.PasswordHasher>();
        services.AddScoped<ITokenGenerator, Security.TokenGenerator>();

        return services;
    }

    /// <summary>
    /// Adds caching services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCachingServices(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis");
        
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            // Add Redis distributed caching
            services.AddSingleton<IConnectionMultiplexer>(provider =>
            {
                var cacheOptions = provider.GetRequiredService<IOptions<CacheOptions>>().Value;
                return ConnectionMultiplexer.Connect(redisConnectionString);
            });

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "CleanArchitecture";
            });

            // Register Redis cache services with appropriate lifetimes
            services.AddScoped<ICacheService, RedisCacheService>();
        }
        else
        {
            // Fallback to in-memory cache if Redis is not configured
            services.AddMemoryCache();
            services.AddScoped<ICacheService, MemoryCacheService>();
        }

        // Register cache support services with appropriate lifetimes
        services.AddScoped<ICacheKeyService, CacheKeyService>();
        services.AddScoped<CacheInvalidationService>();

        return services;
    }

    /// <summary>
    /// Adds message queue services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddMessageQueueServices(this IServiceCollection services, IConfiguration configuration)
    {
        // TODO: Fix RabbitMQ version compatibility issues
        // Register RabbitMQ services with appropriate lifetimes
        // services.AddSingleton<IMessageQueueService, RabbitMQService>();
        // services.AddScoped<IMessagePublisher, MessagePublisher>();
        // services.AddScoped<IMessageConsumer, MessageConsumer>();
        
        // Register message handlers with scoped lifetime
        // services.AddScoped<MessageQueue.Handlers.UserEventHandler>();
        // services.AddScoped<MessageQueue.Handlers.EmailNotificationHandler>();
        
        // Register hosted services for automatic startup/shutdown
        // services.AddHostedService<MessageConsumer>();
        // services.AddHostedService<MessageQueue.Services.MessageQueueBackgroundService>();

        return services;
    }

    /// <summary>
    /// Adds logging services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddLoggingServices(this IServiceCollection services)
    {
        // Register application logger with scoped lifetime
        services.AddScoped(typeof(IApplicationLogger<>), typeof(ApplicationLogger<>));

        return services;
    }

    /// <summary>
    /// Adds health check services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddHealthCheckServices(this IServiceCollection services, IConfiguration configuration)
    {
        var healthChecksBuilder = services.AddHealthChecks();

        // Add application health check
        healthChecksBuilder.AddCheck<ApplicationHealthCheck>("application", tags: new[] { "application" });

        // Add database health check
        healthChecksBuilder.AddCheck<DatabaseHealthCheck>("database", tags: new[] { "database", "infrastructure" });

        // Add Redis health check if Redis is configured
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            healthChecksBuilder.AddCheck<RedisHealthCheck>("redis", tags: new[] { "redis", "cache", "infrastructure" });
        }

        // Add RabbitMQ health check
        // healthChecksBuilder.AddCheck<RabbitMQHealthCheck>("rabbitmq", tags: new[] { "rabbitmq", "messaging", "infrastructure" });

        return services;
    }
}