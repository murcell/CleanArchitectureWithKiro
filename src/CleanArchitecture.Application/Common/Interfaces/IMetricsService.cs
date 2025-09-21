namespace CleanArchitecture.Application.Common.Interfaces;

/// <summary>
/// Service for collecting application metrics
/// </summary>
public interface IMetricsService
{
    /// <summary>
    /// Increments a counter metric
    /// </summary>
    /// <param name="name">Metric name</param>
    /// <param name="tags">Optional tags</param>
    void IncrementCounter(string name, Dictionary<string, string>? tags = null);
    
    /// <summary>
    /// Records a gauge metric
    /// </summary>
    /// <param name="name">Metric name</param>
    /// <param name="value">Metric value</param>
    /// <param name="tags">Optional tags</param>
    void RecordGauge(string name, double value, Dictionary<string, string>? tags = null);
    
    /// <summary>
    /// Records a histogram metric (for timing/duration)
    /// </summary>
    /// <param name="name">Metric name</param>
    /// <param name="value">Metric value</param>
    /// <param name="tags">Optional tags</param>
    void RecordHistogram(string name, double value, Dictionary<string, string>? tags = null);
    
    /// <summary>
    /// Starts a timer for measuring duration
    /// </summary>
    /// <param name="name">Metric name</param>
    /// <param name="tags">Optional tags</param>
    /// <returns>Disposable timer</returns>
    IDisposable StartTimer(string name, Dictionary<string, string>? tags = null);
}