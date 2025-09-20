using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Validators;

namespace CleanArchitecture.Application.Common.Behaviors;

/// <summary>
/// Validation behavior with caching support for expensive validations
/// </summary>
/// <typeparam name="TRequest">The request type</typeparam>
/// <typeparam name="TResponse">The response type</typeparam>
public class CachedValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly IValidationCacheService _cacheService;
    private readonly ILogger<CachedValidationBehavior<TRequest, TResponse>> _logger;

    public CachedValidationBehavior(
        IEnumerable<IValidator<TRequest>> validators,
        IValidationCacheService cacheService,
        ILogger<CachedValidationBehavior<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        if (!_validators.Any())
        {
            _logger.LogDebug("No validators found for request {RequestName}", requestName);
            return await next();
        }

        _logger.LogDebug("Starting cached validation for request {RequestName} with {ValidatorCount} validators", 
            requestName, _validators.Count());

        var context = new ValidationContext<TRequest>(request);
        var allFailures = new List<FluentValidation.Results.ValidationFailure>();
        var cacheHits = 0;
        var cacheMisses = 0;

        foreach (var validator in _validators)
        {
            var validatorName = validator.GetType().Name;
            
            // Try to get cached result first
            var cachedResult = await _cacheService.GetCachedResultAsync(request, validatorName, cancellationToken);
            
            if (cachedResult != null)
            {
                _logger.LogDebug("Cache hit for validator {ValidatorName}", validatorName);
                cacheHits++;
                allFailures.AddRange(cachedResult.Errors);
            }
            else
            {
                _logger.LogDebug("Cache miss for validator {ValidatorName}, executing validation", validatorName);
                cacheMisses++;
                
                var validationResult = await validator.ValidateAsync(context, cancellationToken);
                allFailures.AddRange(validationResult.Errors);
                
                // Cache the result for future use
                // Only cache if validation is expensive (has async rules or complex logic)
                if (ShouldCacheValidationResult(validator))
                {
                    await _cacheService.SetCachedResultAsync(request, validatorName, validationResult, 
                        TimeSpan.FromMinutes(5), cancellationToken);
                    _logger.LogDebug("Cached validation result for validator {ValidatorName}", validatorName);
                }
            }
        }

        _logger.LogDebug("Validation completed for request {RequestName}: {CacheHits} cache hits, {CacheMisses} cache misses", 
            requestName, cacheHits, cacheMisses);

        if (allFailures.Any())
        {
            _logger.LogWarning("Validation failed for request {RequestName} with {ErrorCount} errors: {Errors}",
                requestName, 
                allFailures.Count,
                string.Join("; ", allFailures.Select(f => $"{f.PropertyName}: {f.ErrorMessage}")));

            throw new Common.Exceptions.ValidationException(allFailures);
        }

        _logger.LogDebug("Validation passed for request {RequestName}", requestName);
        return await next();
    }

    /// <summary>
    /// Determines if validation result should be cached based on validator characteristics
    /// </summary>
    private static bool ShouldCacheValidationResult(IValidator<TRequest> validator)
    {
        // Cache validation results for validators that likely perform expensive operations
        // This is a simple heuristic - in practice, you might use attributes or interfaces
        var validatorType = validator.GetType();
        var validatorName = validatorType.Name;
        
        // Cache async validators (likely doing database calls)
        if (validatorName.Contains("Async"))
            return true;
            
        // Cache context-aware validators (might be doing complex business logic)
        if (validatorName.Contains("ContextAware"))
            return true;
            
        // Don't cache simple validators
        return false;
    }
}