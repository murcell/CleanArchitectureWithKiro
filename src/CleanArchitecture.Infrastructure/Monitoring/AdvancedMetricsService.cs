using CleanArchitecture.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace CleanArchitecture.Infrastructure.Monitoring;

/// <summary>
/// Advanced metrics service with business-specific metrics
/// </summary>
public class AdvancedMetricsService : IDisposable
{
    private readonly Meter _meter;
    private readonly ILogger<AdvancedMetricsService> _logger;
    private readonly ConcurrentDictionary<string, Counter<long>> _counters = new();
    private readonly ConcurrentDictionary<string, Histogram<double>> _histograms = new();
    private readonly ConcurrentDictionary<string, ObservableGauge<double>> _gauges = new();

    // Business metrics
    private long _totalUsers = 0;
    private long _totalProducts = 0;
    private long _totalApiKeys = 0;
    private long _activeUsers = 0;
    private long _failedLogins = 0;
    private long _successfulLogins = 0;

    public AdvancedMetricsService(ILogger<AdvancedMetricsService> logger)
    {
        _logger = logger;
        _meter = new Meter("CleanArchitecture.Business", "1.0.0");
        
        InitializeBusinessMetrics();
    }

    private void InitializeBusinessMetrics()
    {
        // User metrics
        _meter.CreateObservableGauge("users_total", () => _totalUsers, description: "Total number of users");
        _meter.CreateObservableGauge("users_active", () => _activeUsers, description: "Number of active users");
        
        // Product metrics
        _meter.CreateObservableGauge("products_total", () => _totalProducts, description: "Total number of products");
        
        // API Key metrics
        _meter.CreateObservableGauge("api_keys_total", () => _totalApiKeys, description: "Total number of API keys");
        
        // Authentication metrics
        _meter.CreateObservableGauge("login_success_rate", () => 
            _successfulLogins + _failedLogins > 0 
                ? (double)_successfulLogins / (_successfulLogins + _failedLogins) * 100 
                : 0, 
            description: "Login success rate percentage");

        // System metrics
        _meter.CreateObservableGauge("memory_usage_mb", () => 
            GC.GetTotalMemory(false) / 1024.0 / 1024.0, 
            description: "Memory usage in MB");
            
        _meter.CreateObservableGauge("gc_collections_gen0", () => 
            GC.CollectionCount(0), 
            description: "Generation 0 garbage collections");
            
        _meter.CreateObservableGauge("gc_collections_gen1", () => 
            GC.CollectionCount(1), 
            description: "Generation 1 garbage collections");
            
        _meter.CreateObservableGauge("gc_collections_gen2", () => 
            GC.CollectionCount(2), 
            description: "Generation 2 garbage collections");
    }

    // Business metric methods
    public void RecordUserRegistration()
    {
        Interlocked.Increment(ref _totalUsers);
        GetOrCreateCounter("user_registrations_total").Add(1);
        _logger.LogDebug("User registration recorded");
    }

    public void RecordUserLogin(bool successful)
    {
        if (successful)
        {
            Interlocked.Increment(ref _successfulLogins);
            GetOrCreateCounter("login_successful_total").Add(1);
        }
        else
        {
            Interlocked.Increment(ref _failedLogins);
            GetOrCreateCounter("login_failed_total").Add(1);
        }
        
        _logger.LogDebug("User login recorded: {Successful}", successful);
    }

    public void RecordProductCreation()
    {
        Interlocked.Increment(ref _totalProducts);
        GetOrCreateCounter("product_creations_total").Add(1);
        _logger.LogDebug("Product creation recorded");
    }

    public void RecordApiKeyCreation()
    {
        Interlocked.Increment(ref _totalApiKeys);
        GetOrCreateCounter("api_key_creations_total").Add(1);
        _logger.LogDebug("API key creation recorded");
    }

    public void RecordDatabaseQuery(string operation, double durationMs)
    {
        var tags = new KeyValuePair<string, object?>[] 
        {
            new("operation", operation)
        };
        
        GetOrCreateHistogram("database_query_duration_ms").Record(durationMs, tags);
        GetOrCreateCounter("database_queries_total").Add(1, tags);
        
        _logger.LogDebug("Database query recorded: {Operation}, Duration: {Duration}ms", operation, durationMs);
    }

    public void RecordCacheOperation(string operation, bool hit)
    {
        var tags = new KeyValuePair<string, object?>[] 
        {
            new("operation", operation),
            new("hit", hit)
        };
        
        GetOrCreateCounter("cache_operations_total").Add(1, tags);
        
        if (hit)
        {
            GetOrCreateCounter("cache_hits_total").Add(1, new KeyValuePair<string, object?>[] 
            {
                new("operation", operation)
            });
        }
        else
        {
            GetOrCreateCounter("cache_misses_total").Add(1, new KeyValuePair<string, object?>[] 
            {
                new("operation", operation)
            });
        }
        
        _logger.LogDebug("Cache operation recorded: {Operation}, Hit: {Hit}", operation, hit);
    }

    public void RecordApiCall(string endpoint, string method, int statusCode, double durationMs)
    {
        var tags = new KeyValuePair<string, object?>[] 
        {
            new("endpoint", endpoint),
            new("method", method),
            new("status_code", statusCode)
        };
        
        GetOrCreateCounter("api_requests_total").Add(1, tags);
        GetOrCreateHistogram("api_request_duration_ms").Record(durationMs, tags);
        
        if (statusCode >= 400)
        {
            GetOrCreateCounter("api_errors_total").Add(1, tags);
        }
        
        _logger.LogDebug("API call recorded: {Method} {Endpoint}, Status: {StatusCode}, Duration: {Duration}ms", 
            method, endpoint, statusCode, durationMs);
    }

    public void UpdateActiveUsers(long count)
    {
        Interlocked.Exchange(ref _activeUsers, count);
        _logger.LogDebug("Active users updated: {Count}", count);
    }

    // Helper methods
    private Counter<long> GetOrCreateCounter(string name)
    {
        return _counters.GetOrAdd(name, _ => _meter.CreateCounter<long>(name));
    }

    private Histogram<double> GetOrCreateHistogram(string name)
    {
        return _histograms.GetOrAdd(name, _ => _meter.CreateHistogram<double>(name));
    }

    public void Dispose()
    {
        _meter?.Dispose();
    }
}