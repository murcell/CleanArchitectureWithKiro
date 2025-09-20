using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using CleanArchitecture.WebAPI.Tests.Common;

namespace CleanArchitecture.Performance.Tests.Common;

[Config(typeof(PerformanceConfig))]
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public abstract class PerformanceTestBase : IDisposable
{
    protected TestWebApplicationFactory Factory { get; private set; } = null!;
    protected HttpClient Client { get; private set; } = null!;
    protected IServiceScope Scope { get; private set; } = null!;

    [GlobalSetup]
    public virtual async Task GlobalSetup()
    {
        Factory = new TestWebApplicationFactory();
        Client = Factory.CreateClient();
        Scope = Factory.Services.CreateScope();
        
        await InitializeAsync();
    }

    [GlobalCleanup]
    public virtual async Task GlobalCleanup()
    {
        await CleanupAsync();
        
        Scope?.Dispose();
        Client?.Dispose();
        Factory?.Dispose();
    }

    protected virtual Task InitializeAsync() => Task.CompletedTask;
    protected virtual Task CleanupAsync() => Task.CompletedTask;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Scope?.Dispose();
            Client?.Dispose();
            Factory?.Dispose();
        }
    }
}

public class PerformanceConfig : ManualConfig
{
    public PerformanceConfig()
    {
        AddJob(Job.Default
            .WithToolchain(InProcessEmitToolchain.Instance)
            .WithWarmupCount(3)
            .WithIterationCount(10)
            .WithInvocationCount(100));
    }
}