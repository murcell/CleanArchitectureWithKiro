using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CleanArchitecture.Infrastructure.Monitoring;

/// <summary>
/// Service for Application Insights integration and custom telemetry
/// </summary>
public class ApplicationInsightsService
{
    private readonly ILogger<ApplicationInsightsService> _logger;
    private static readonly ActivitySource ActivitySource = new("CleanArchitecture.Application");

    public ApplicationInsightsService(ILogger<ApplicationInsightsService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Tracks a custom event with properties and metrics
    /// </summary>
    public void TrackEvent(string eventName, Dictionary<string, string>? properties = null, Dictionary<string, double>? metrics = null)
    {
        using var activity = ActivitySource.StartActivity($"Event.{eventName}");
        
        if (properties != null)
        {
            foreach (var prop in properties)
            {
                activity?.SetTag(prop.Key, prop.Value);
            }
        }

        if (metrics != null)
        {
            foreach (var metric in metrics)
            {
                activity?.SetTag($"metric.{metric.Key}", metric.Value.ToString());
            }
        }

        _logger.LogInformation("Custom event tracked: {EventName}", eventName);
    }

    /// <summary>
    /// Tracks a business operation with timing
    /// </summary>
    public IDisposable TrackOperation(string operationName, Dictionary<string, string>? properties = null)
    {
        var activity = ActivitySource.StartActivity($"Operation.{operationName}");
        
        if (properties != null)
        {
            foreach (var prop in properties)
            {
                activity?.SetTag(prop.Key, prop.Value);
            }
        }

        _logger.LogDebug("Operation started: {OperationName}", operationName);
        
        return new OperationTracker(activity, operationName, _logger);
    }

    /// <summary>
    /// Tracks a dependency call (external service, database, etc.)
    /// </summary>
    public IDisposable TrackDependency(string dependencyType, string dependencyName, string? data = null)
    {
        var activity = ActivitySource.StartActivity($"Dependency.{dependencyType}.{dependencyName}");
        
        activity?.SetTag("dependency.type", dependencyType);
        activity?.SetTag("dependency.name", dependencyName);
        
        if (!string.IsNullOrEmpty(data))
        {
            activity?.SetTag("dependency.data", data);
        }

        _logger.LogDebug("Dependency call started: {DependencyType}.{DependencyName}", dependencyType, dependencyName);
        
        return new DependencyTracker(activity, dependencyType, dependencyName, _logger);
    }

    /// <summary>
    /// Tracks an exception with context
    /// </summary>
    public void TrackException(Exception exception, Dictionary<string, string>? properties = null)
    {
        using var activity = ActivitySource.StartActivity("Exception");
        
        activity?.SetTag("exception.type", exception.GetType().Name);
        activity?.SetTag("exception.message", exception.Message);
        activity?.SetTag("exception.stackTrace", exception.StackTrace);
        
        if (properties != null)
        {
            foreach (var prop in properties)
            {
                activity?.SetTag(prop.Key, prop.Value);
            }
        }

        activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
        
        _logger.LogError(exception, "Exception tracked with telemetry");
    }

    /// <summary>
    /// Tracks a page view (for web applications)
    /// </summary>
    public void TrackPageView(string pageName, string? url = null, TimeSpan? duration = null, Dictionary<string, string>? properties = null)
    {
        using var activity = ActivitySource.StartActivity($"PageView.{pageName}");
        
        activity?.SetTag("page.name", pageName);
        
        if (!string.IsNullOrEmpty(url))
        {
            activity?.SetTag("page.url", url);
        }
        
        if (duration.HasValue)
        {
            activity?.SetTag("page.duration", duration.Value.TotalMilliseconds.ToString());
        }
        
        if (properties != null)
        {
            foreach (var prop in properties)
            {
                activity?.SetTag(prop.Key, prop.Value);
            }
        }

        _logger.LogDebug("Page view tracked: {PageName}", pageName);
    }

    private class OperationTracker : IDisposable
    {
        private readonly Activity? _activity;
        private readonly string _operationName;
        private readonly ILogger _logger;
        private readonly Stopwatch _stopwatch;

        public OperationTracker(Activity? activity, string operationName, ILogger logger)
        {
            _activity = activity;
            _operationName = operationName;
            _logger = logger;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _activity?.SetTag("operation.duration", _stopwatch.ElapsedMilliseconds.ToString());
            _activity?.Dispose();
            
            _logger.LogDebug("Operation completed: {OperationName}, Duration: {Duration}ms", 
                _operationName, _stopwatch.ElapsedMilliseconds);
        }
    }

    private class DependencyTracker : IDisposable
    {
        private readonly Activity? _activity;
        private readonly string _dependencyType;
        private readonly string _dependencyName;
        private readonly ILogger _logger;
        private readonly Stopwatch _stopwatch;

        public DependencyTracker(Activity? activity, string dependencyType, string dependencyName, ILogger logger)
        {
            _activity = activity;
            _dependencyType = dependencyType;
            _dependencyName = dependencyName;
            _logger = logger;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _activity?.SetTag("dependency.duration", _stopwatch.ElapsedMilliseconds.ToString());
            _activity?.Dispose();
            
            _logger.LogDebug("Dependency call completed: {DependencyType}.{DependencyName}, Duration: {Duration}ms", 
                _dependencyType, _dependencyName, _stopwatch.ElapsedMilliseconds);
        }
    }
}