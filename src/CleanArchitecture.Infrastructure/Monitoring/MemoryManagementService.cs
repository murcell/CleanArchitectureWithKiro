using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Infrastructure.Monitoring;

/// <summary>
/// Background service for memory management and monitoring
/// </summary>
public class MemoryManagementService : BackgroundService
{
    private readonly ILogger<MemoryManagementService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);
    private readonly long _memoryThresholdBytes = 500 * 1024 * 1024; // 500MB

    public MemoryManagementService(ILogger<MemoryManagementService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Memory Management Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MonitorMemoryUsage();
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in memory management service");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        _logger.LogInformation("Memory Management Service stopped");
    }

    private async Task MonitorMemoryUsage()
    {
        var beforeGC = GC.GetTotalMemory(false);
        var memoryInfo = GetMemoryInfo();

        _logger.LogDebug("Memory usage - Before GC: {BeforeGC:N0} bytes, Working Set: {WorkingSet:N0} bytes",
            beforeGC, memoryInfo.WorkingSet);

        // Force garbage collection if memory usage is high
        if (beforeGC > _memoryThresholdBytes)
        {
            _logger.LogWarning("High memory usage detected: {MemoryUsage:N0} bytes. Forcing garbage collection.",
                beforeGC);

            // Force full garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var afterGC = GC.GetTotalMemory(false);
            var freedMemory = beforeGC - afterGC;

            _logger.LogInformation("Garbage collection completed. Freed: {FreedMemory:N0} bytes, Current: {CurrentMemory:N0} bytes",
                freedMemory, afterGC);
        }

        // Log memory statistics
        LogMemoryStatistics();

        await Task.CompletedTask;
    }

    private void LogMemoryStatistics()
    {
        var memoryInfo = GetMemoryInfo();
        
        _logger.LogDebug("Memory Statistics - " +
            "Total Memory: {TotalMemory:N0} bytes, " +
            "Working Set: {WorkingSet:N0} bytes, " +
            "Private Memory: {PrivateMemory:N0} bytes, " +
            "Gen 0 Collections: {Gen0}, " +
            "Gen 1 Collections: {Gen1}, " +
            "Gen 2 Collections: {Gen2}",
            GC.GetTotalMemory(false),
            memoryInfo.WorkingSet,
            memoryInfo.PrivateMemory,
            GC.CollectionCount(0),
            GC.CollectionCount(1),
            GC.CollectionCount(2));
    }

    private static MemoryInfo GetMemoryInfo()
    {
        var process = System.Diagnostics.Process.GetCurrentProcess();
        
        return new MemoryInfo
        {
            WorkingSet = process.WorkingSet64,
            PrivateMemory = process.PrivateMemorySize64,
            VirtualMemory = process.VirtualMemorySize64
        };
    }

    private class MemoryInfo
    {
        public long WorkingSet { get; set; }
        public long PrivateMemory { get; set; }
        public long VirtualMemory { get; set; }
    }
}