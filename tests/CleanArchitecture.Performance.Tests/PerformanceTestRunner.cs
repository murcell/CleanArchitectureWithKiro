using BenchmarkDotNet.Running;
using CleanArchitecture.Performance.Tests.Database;
using CleanArchitecture.Performance.Tests.Caching;

namespace CleanArchitecture.Performance.Tests;

/// <summary>
/// Performance test runner for executing benchmark tests
/// </summary>
public class PerformanceTestRunner
{
    /// <summary>
    /// Run all performance benchmarks
    /// </summary>
    public static void RunAllBenchmarks()
    {
        Console.WriteLine("Starting Performance Benchmarks...");
        
        // Run database performance benchmarks
        Console.WriteLine("Running Database Performance Benchmarks...");
        BenchmarkRunner.Run<DatabasePerformanceTests>();
        
        // Run cache performance benchmarks
        Console.WriteLine("Running Cache Performance Benchmarks...");
        BenchmarkRunner.Run<CachePerformanceTests>();
        
        Console.WriteLine("Performance Benchmarks Completed!");
        Console.WriteLine("Note: Load tests are available as xUnit tests. Run 'dotnet test --filter Category=LoadTest' to execute them.");
    }

    /// <summary>
    /// Run database performance benchmarks only
    /// </summary>
    public static void RunDatabaseBenchmarks()
    {
        Console.WriteLine("Running Database Performance Benchmarks...");
        BenchmarkRunner.Run<DatabasePerformanceTests>();
    }

    /// <summary>
    /// Run cache performance benchmarks only
    /// </summary>
    public static void RunCacheBenchmarks()
    {
        Console.WriteLine("Running Cache Performance Benchmarks...");
        BenchmarkRunner.Run<CachePerformanceTests>();
    }

    /// <summary>
    /// Run load tests using xUnit test runner
    /// </summary>
    public static void RunLoadTests()
    {
        Console.WriteLine("Load tests are implemented as xUnit tests.");
        Console.WriteLine("To run load tests, use: dotnet test --filter Category=LoadTest");
        Console.WriteLine("Available load test classes:");
        Console.WriteLine("- ApiLoadTests: Basic API load testing");
        Console.WriteLine("- ComprehensiveLoadTests: Comprehensive load testing scenarios");
        Console.WriteLine("- SimpleLoadTests: Simple load testing scenarios");
    }
}

