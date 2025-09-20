using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using CleanArchitecture.Application.Common.Exceptions;

namespace CleanArchitecture.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior for request validation with enhanced error handling and logging
/// </summary>
/// <typeparam name="TRequest">The request type</typeparam>
/// <typeparam name="TResponse">The response type</typeparam>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

    public ValidationBehavior(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    {
        _validators = validators;
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

        _logger.LogDebug("Validating request {RequestName} with {ValidatorCount} validators", 
            requestName, _validators.Count());

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .Where(r => r.Errors.Any())
            .SelectMany(r => r.Errors)
            .ToList();

        if (failures.Any())
        {
            _logger.LogWarning("Validation failed for request {RequestName} with {ErrorCount} errors: {Errors}",
                requestName, 
                failures.Count,
                string.Join("; ", failures.Select(f => $"{f.PropertyName}: {f.ErrorMessage}")));

            throw new Common.Exceptions.ValidationException(failures);
        }

        _logger.LogDebug("Validation passed for request {RequestName}", requestName);
        return await next();
    }
}