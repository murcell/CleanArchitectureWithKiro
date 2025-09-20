# Configuration Management

This document describes the configuration management system implemented in the Clean Architecture project.

## Configuration Structure

The application uses a hierarchical configuration system with environment-specific overrides:

1. **appsettings.json** - Base configuration
2. **appsettings.{Environment}.json** - Environment-specific overrides
3. **Environment variables** - Runtime overrides
4. **User secrets** - Development secrets (not in source control)

## Configuration Sections

### Application Options (`Application`)
Controls application-level behavior and performance settings.

```json
{
  "Application": {
    "EnablePerformanceMonitoring": true,
    "PerformanceThresholdMs": 500,
    "EnableValidationCaching": true,
    "ValidationCacheExpirationMinutes": 30,
    "EnableDetailedValidationErrors": true,
    "MaxFileUploadSizeMB": 10,
    "AllowedFileExtensions": [".jpg", ".jpeg", ".png", ".pdf"],
    "DefaultPageSize": 20,
    "MaxPageSize": 100
  }
}
```

### Database Options (`Database`)
Controls Entity Framework and database connection settings.

```json
{
  "Database": {
    "EnableAutomaticMigrations": false,
    "EnableSensitiveDataLogging": false,
    "EnableDetailedErrors": false,
    "CommandTimeout": 30,
    "MaxRetryCount": 3,
    "MaxRetryDelay": 30
  }
}
```

### Cache Options (`Cache`)
Controls Redis caching behavior and settings.

```json
{
  "Cache": {
    "DefaultExpiration": "00:30:00",
    "KeyPrefix": "CleanArch",
    "EnableCompression": true,
    "InstanceName": "CleanArchitecture",
    "EnableDistributedCache": true,
    "SlidingExpiration": "00:15:00",
    "MaxCacheSizeMB": 100,
    "EnableStatistics": true,
    "KeySeparator": ":"
  }
}
```

### Message Queue Options (`RabbitMQ`)
Controls RabbitMQ message queue settings.

```json
{
  "RabbitMQ": {
    "HostName": "localhost",
    "UserName": "guest",
    "Password": "guest",
    "Port": 5672,
    "VirtualHost": "/",
    "MaxRetryAttempts": 3,
    "RetryDelay": "00:00:05",
    "EnableDeadLetterQueue": true,
    "ConnectionTimeoutSeconds": 30,
    "EnableAutomaticRecovery": true,
    "NetworkRecoveryIntervalSeconds": 10,
    "PrefetchCount": 10,
    "EnablePublisherConfirms": true
  }
}
```

### API Options (`Api`)
Controls Web API behavior, documentation, and security settings.

```json
{
  "Api": {
    "Title": "Clean Architecture API",
    "Description": "A comprehensive Clean Architecture implementation",
    "EnableVersioning": true,
    "DefaultVersion": "1.0",
    "EnableSwagger": true,
    "EnableCors": true,
    "AllowedOrigins": [],
    "EnableRateLimiting": false,
    "RateLimitRequestsPerMinute": 100
  }
}
```

### Logging Options (`Logging`)
Controls application logging behavior.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

## Environment-Specific Configurations

### Development Environment
- Enables detailed error messages and sensitive data logging
- Uses development database with automatic migrations
- Enables Swagger documentation
- Reduced cache expiration times for faster development

### Staging Environment
- Production-like settings with enhanced logging
- Uses staging database and external services
- Enables Swagger for testing
- Moderate performance thresholds

### Production Environment
- Optimized for performance and security
- Disables sensitive data logging and detailed errors
- Disables Swagger documentation
- Enables rate limiting and security features

### Testing Environment
- Minimal configuration for unit and integration tests
- Uses in-memory or test databases
- Disables external services where possible
- Fast timeouts and reduced retry attempts

## Configuration Validation

The application validates configuration at two levels:

### Startup Validation
- Validates required connection strings exist
- Validates required configuration sections exist
- Validates environment-specific requirements
- Throws exceptions for critical configuration errors

### Runtime Validation
- Uses `IValidateOptions<T>` for each configuration section
- Validates data types, ranges, and business rules
- Provides detailed error messages for configuration issues
- Logs configuration summary at startup

## Security Considerations

### Sensitive Data
- Never store passwords or secrets in appsettings.json
- Use User Secrets for development
- Use environment variables or Azure Key Vault for production
- Validate that sensitive logging is disabled in production

### Connection Strings
- Use integrated security where possible
- Store connection strings in secure configuration providers
- Validate connection string format and accessibility

## Best Practices

### Configuration Organization
1. Group related settings in logical sections
2. Use consistent naming conventions (PascalCase)
3. Provide sensible defaults in base configuration
4. Override only necessary values in environment-specific files

### Validation
1. Validate all configuration values at startup
2. Provide clear error messages for invalid configuration
3. Use strongly-typed configuration classes
4. Implement custom validators for complex business rules

### Environment Management
1. Use clear environment names (Development, Staging, Production, Testing)
2. Validate environment-specific requirements
3. Log configuration summary at startup
4. Use configuration helpers for runtime access

## Troubleshooting

### Common Issues
1. **Missing Configuration Section**: Ensure all required sections exist in appsettings.json
2. **Invalid Connection String**: Validate database connectivity and format
3. **Environment Mismatch**: Check ASPNETCORE_ENVIRONMENT variable
4. **Validation Errors**: Review validator error messages for specific issues

### Debugging Configuration
1. Enable detailed logging for configuration loading
2. Use the configuration helper to inspect runtime values
3. Check environment variable overrides
4. Validate JSON syntax in configuration files