using System.Diagnostics;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using CleanArchitecture.Application.Common.Exceptions;

namespace CleanArchitecture.Application.Common.Behaviors;

/// <summary>
/// Enhanced validation behavior with performance monitoring and detailed metrics
/// </summary>
/// <typeparam name="TRequest">The request type</typeparam>
/// <typeparam name="TResponse">The response type</typeparam>
public class ValidationPerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationPerformanceBehavior<TRequest, TResponse>> _logger;

    public ValidationPerformanceBehavior(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidationPerformanceBehavior<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (!_validators.Any())
            {
                _logger.LogDebug("No validators found for request {RequestName}", requestName);
                return await next();
            }

            _logger.LogDebug("Starting validation for request {RequestName} with {ValidatorCount} validators", 
                requestName, _validators.Count());

            var context = new ValidationContext<TRequest>(request);
            var validationTasks = _validators.Select(async validator =>
            {
                var validatorStopwatch = Stopwatch.StartNew();
                try
                {
                    var result = await validator.ValidateAsync(context, cancellationToken);
                    validatorStopwatch.Stop();
                    
                    _logger.LogDebug("Validator {ValidatorName} completed in {ElapsedMs}ms with {ErrorCount} errors",
                        validator.GetType().Name, 
                        validatorStopwatch.ElapsedMilliseconds,
                        result.Errors.Count);
                    
                    return result;
                }
                catch (Exception ex)
                {
                    validatorStopwatch.Stop();
                    _logger.LogError(ex, "Validator {ValidatorName} failed after {ElapsedMs}ms",
                        validator.GetType().Name, 
                        validatorStopwatch.ElapsedMilliseconds);
                    throw;
                }
            });

            var validationResults = await Task.WhenAll(validationTasks);

            var failures = validationResults
                .Where(r => r.Errors.Any())
                .SelectMany(r => r.Errors)
                .ToList();

            stopwatch.Stop();

            if (failures.Any())
            {
                _logger.LogWarning("Validation failed for request {RequestName} in {ElapsedMs}ms with {ErrorCount} errors: {Errors}",
                    requestName, 
                    stopwatch.ElapsedMilliseconds,
                    failures.Count,
                    string.Join("; ", failures.Select(f => $"{f.PropertyName}: {f.ErrorMessage}")));

                throw new Common.Exceptions.ValidationException(failures);
            }

            _logger.LogDebug("Validation passed for request {RequestName} in {ElapsedMs}ms", 
                requestName, stopwatch.ElapsedMilliseconds);

            // Log performance warning if validation takes too long
            if (stopwatch.ElapsedMilliseconds > 1000) // 1 second threshold
            {
                _logger.LogWarning("Validation for request {RequestName} took {ElapsedMs}ms, which exceeds the recommended threshold",
                    requestName, stopwatch.ElapsedMilliseconds);
            }

            return await next();
        }
        catch (Exception ex) when (!(ex is Common.Exceptions.ValidationException))
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Unexpected error during validation for request {RequestName} after {ElapsedMs}ms",
                requestName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}