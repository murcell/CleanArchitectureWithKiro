using CleanArchitecture.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace CleanArchitecture.Infrastructure.Monitoring;

/// <summary>
/// Service for collecting application metrics using .NET Metrics API
/// </summary>
public class MetricsService : IMetricsService, IDisposable
{
    private readonly Meter _meter;
    private readonly ILogger<MetricsService> _logger;
    private readonly Dictionary<string, Counter<long>> _counters = new();
    private readonly Dictionary<string, Histogram<double>> _histograms = new();

    public MetricsService(ILogger<MetricsService> logger)
    {
        _logger = logger;
        _meter = new Meter("CleanArchitecture.Application", "1.0.0");
    }

    public void IncrementCounter(string name, Dictionary<string, string>? tags = null)
    {
        try
        {
            var counter = GetOrCreateCounter(name);
            var tagArray = ConvertTagsToArray(tags);
            counter.Add(1, tagArray);
            
            _logger.LogDebug("Counter {Name} incremented", name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing counter {Name}", name);
        }
    }

    public void RecordGauge(string name, double value, Dictionary<string, string>? tags = null)
    {
        try
        {
            // For gauges, we'll use histograms as .NET doesn't have built-in gauge support
            var histogram = GetOrCreateHistogram(name);
            var tagArray = ConvertTagsToArray(tags);
            histogram.Record(value, tagArray);
            
            _logger.LogDebug("Gauge {Name} recorded with value {Value}", name, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording gauge {Name}", name);
        }
    }

    public void RecordHistogram(string name, double value, Dictionary<string, string>? tags = null)
    {
        try
        {
            var histogram = GetOrCreateHistogram(name);
            var tagArray = ConvertTagsToArray(tags);
            histogram.Record(value, tagArray);
            
            _logger.LogDebug("Histogram {Name} recorded with value {Value}", name, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording histogram {Name}", name);
        }
    }

    public IDisposable StartTimer(string name, Dictionary<string, string>? tags = null)
    {
        return new MetricTimer(this, name, tags);
    }

    private Counter<long> GetOrCreateCounter(string name)
    {
        if (!_counters.TryGetValue(name, out var counter))
        {
            counter = _meter.CreateCounter<long>(name);
            _counters[name] = counter;
        }
        return counter;
    }

    private Histogram<double> GetOrCreateHistogram(string name)
    {
        if (!_histograms.TryGetValue(name, out var histogram))
        {
            histogram = _meter.CreateHistogram<double>(name);
            _histograms[name] = histogram;
        }
        return histogram;
    }

    private static KeyValuePair<string, object?>[] ConvertTagsToArray(Dictionary<string, string>? tags)
    {
        if (tags == null || tags.Count == 0)
            return Array.Empty<KeyValuePair<string, object?>>();

        return tags.Select(kvp => new KeyValuePair<string, object?>(kvp.Key, kvp.Value)).ToArray();
    }

    public void Dispose()
    {
        _meter?.Dispose();
    }

    private class MetricTimer : IDisposable
    {
        private readonly MetricsService _metricsService;
        private readonly string _name;
        private readonly Dictionary<string, string>? _tags;
        private readonly Stopwatch _stopwatch;

        public MetricTimer(MetricsService metricsService, string name, Dictionary<string, string>? tags)
        {
            _metricsService = metricsService;
            _name = name;
            _tags = tags;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _metricsService.RecordHistogram(_name, _stopwatch.Elapsed.TotalMilliseconds, _tags);
        }
    }
}