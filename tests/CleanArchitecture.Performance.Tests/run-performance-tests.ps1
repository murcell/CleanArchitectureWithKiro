#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Runs performance tests for the Clean Architecture application
.DESCRIPTION
    This script runs various types of performance tests including load tests, 
    database performance tests, and cache performance tests.
.PARAMETER TestType
    The type of performance test to run: all, load, database, cache, benchmarks
.PARAMETER Configuration
    The build configuration to use: Debug or Release (default: Release)
.PARAMETER OutputPath
    The path where test results should be saved (default: ./test-results)
.EXAMPLE
    ./run-performance-tests.ps1 -TestType all
    ./run-performance-tests.ps1 -TestType load -Configuration Release
    ./run-performance-tests.ps1 -TestType benchmarks -OutputPath ./perf-results
#>

param(
    [Parameter(Mandatory = $false)]
    [ValidateSet("all", "load", "database", "cache", "benchmarks")]
    [string]$TestType = "all",
    
    [Parameter(Mandatory = $false)]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    
    [Parameter(Mandatory = $false)]
    [string]$OutputPath = "./test-results"
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Colors for output
$Green = "`e[32m"
$Yellow = "`e[33m"
$Red = "`e[31m"
$Reset = "`e[0m"

function Write-ColorOutput {
    param([string]$Message, [string]$Color = $Reset)
    Write-Host "$Color$Message$Reset"
}

function Test-Prerequisites {
    Write-ColorOutput "Checking prerequisites..." $Yellow
    
    # Check if .NET SDK is installed
    try {
        $dotnetVersion = dotnet --version
        Write-ColorOutput "✓ .NET SDK version: $dotnetVersion" $Green
    }
    catch {
        Write-ColorOutput "✗ .NET SDK not found. Please install .NET 9 SDK." $Red
        exit 1
    }
    
    # Check if Docker is running
    try {
        docker info | Out-Null
        Write-ColorOutput "✓ Docker is running" $Green
    }
    catch {
        Write-ColorOutput "✗ Docker is not running. Please start Docker." $Red
        exit 1
    }
    
    Write-ColorOutput "Prerequisites check completed." $Green
}

function Start-TestInfrastructure {
    Write-ColorOutput "Starting test infrastructure..." $Yellow
    
    # Start test containers using docker-compose
    $dockerComposePath = "../../docker/docker-compose.test.yml"
    if (Test-Path $dockerComposePath) {
        docker-compose -f $dockerComposePath up -d
        Write-ColorOutput "✓ Test infrastructure started" $Green
        
        # Wait for services to be ready
        Write-ColorOutput "Waiting for services to be ready..." $Yellow
        Start-Sleep -Seconds 10
    }
    else {
        Write-ColorOutput "⚠ Docker compose file not found, using existing infrastructure" $Yellow
    }
}

function Stop-TestInfrastructure {
    Write-ColorOutput "Stopping test infrastructure..." $Yellow
    
    $dockerComposePath = "../../docker/docker-compose.test.yml"
    if (Test-Path $dockerComposePath) {
        docker-compose -f $dockerComposePath down
        Write-ColorOutput "✓ Test infrastructure stopped" $Green
    }
}

function Run-LoadTests {
    Write-ColorOutput "Running Load Tests..." $Yellow
    
    $testFilter = "FullyQualifiedName~LoadTests"
    $resultsPath = Join-Path $OutputPath "load-tests"
    
    dotnet test --configuration $Configuration --filter $testFilter --logger "trx;LogFileName=load-tests.trx" --results-directory $resultsPath
    
    if ($LASTEXITCODE -eq 0) {
        Write-ColorOutput "✓ Load tests completed successfully" $Green
    }
    else {
        Write-ColorOutput "✗ Load tests failed" $Red
        return $false
    }
    return $true
}

function Run-DatabasePerformanceTests {
    Write-ColorOutput "Running Database Performance Tests..." $Yellow
    
    $testFilter = "FullyQualifiedName~Database"
    $resultsPath = Join-Path $OutputPath "database-performance"
    
    dotnet test --configuration $Configuration --filter $testFilter --logger "trx;LogFileName=database-performance.trx" --results-directory $resultsPath
    
    if ($LASTEXITCODE -eq 0) {
        Write-ColorOutput "✓ Database performance tests completed successfully" $Green
    }
    else {
        Write-ColorOutput "✗ Database performance tests failed" $Red
        return $false
    }
    return $true
}

function Run-CachePerformanceTests {
    Write-ColorOutput "Running Cache Performance Tests..." $Yellow
    
    $testFilter = "FullyQualifiedName~Caching"
    $resultsPath = Join-Path $OutputPath "cache-performance"
    
    dotnet test --configuration $Configuration --filter $testFilter --logger "trx;LogFileName=cache-performance.trx" --results-directory $resultsPath
    
    if ($LASTEXITCODE -eq 0) {
        Write-ColorOutput "✓ Cache performance tests completed successfully" $Green
    }
    else {
        Write-ColorOutput "✗ Cache performance tests failed" $Red
        return $false
    }
    return $true
}

function Run-Benchmarks {
    Write-ColorOutput "Running Benchmarks..." $Yellow
    
    # Build the project first
    dotnet build --configuration $Configuration
    
    if ($LASTEXITCODE -ne 0) {
        Write-ColorOutput "✗ Build failed" $Red
        return $false
    }
    
    # Run benchmarks
    dotnet run --project . --configuration $Configuration
    
    if ($LASTEXITCODE -eq 0) {
        Write-ColorOutput "✓ Benchmarks completed successfully" $Green
        Write-ColorOutput "Benchmark results saved to BenchmarkDotNet.Artifacts/" $Yellow
    }
    else {
        Write-ColorOutput "✗ Benchmarks failed" $Red
        return $false
    }
    return $true
}

function Run-AllTests {
    Write-ColorOutput "Running All Performance Tests..." $Yellow
    
    $resultsPath = Join-Path $OutputPath "all-performance-tests"
    
    dotnet test --configuration $Configuration --logger "trx;LogFileName=all-performance-tests.trx" --results-directory $resultsPath
    
    if ($LASTEXITCODE -eq 0) {
        Write-ColorOutput "✓ All performance tests completed successfully" $Green
    }
    else {
        Write-ColorOutput "✗ Some performance tests failed" $Red
        return $false
    }
    return $true
}

# Main execution
try {
    Write-ColorOutput "=== Clean Architecture Performance Tests ===" $Green
    Write-ColorOutput "Test Type: $TestType" $Yellow
    Write-ColorOutput "Configuration: $Configuration" $Yellow
    Write-ColorOutput "Output Path: $OutputPath" $Yellow
    Write-ColorOutput ""
    
    # Create output directory
    if (!(Test-Path $OutputPath)) {
        New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
    }
    
    # Check prerequisites
    Test-Prerequisites
    
    # Start test infrastructure
    Start-TestInfrastructure
    
    $success = $true
    
    # Run tests based on type
    switch ($TestType) {
        "load" {
            $success = Run-LoadTests
        }
        "database" {
            $success = Run-DatabasePerformanceTests
        }
        "cache" {
            $success = Run-CachePerformanceTests
        }
        "benchmarks" {
            $success = Run-Benchmarks
        }
        "all" {
            $success = Run-AllTests
            if ($success) {
                Write-ColorOutput "Running additional benchmarks..." $Yellow
                Run-Benchmarks | Out-Null
            }
        }
    }
    
    if ($success) {
        Write-ColorOutput "" 
        Write-ColorOutput "=== Performance Tests Completed Successfully ===" $Green
        Write-ColorOutput "Results saved to: $OutputPath" $Yellow
        
        if ($TestType -eq "benchmarks" -or $TestType -eq "all") {
            Write-ColorOutput "Benchmark results saved to: BenchmarkDotNet.Artifacts/" $Yellow
        }
    }
    else {
        Write-ColorOutput "" 
        Write-ColorOutput "=== Performance Tests Failed ===" $Red
        exit 1
    }
}
catch {
    Write-ColorOutput "Error occurred: $($_.Exception.Message)" $Red
    exit 1
}
finally {
    # Always try to stop test infrastructure
    Stop-TestInfrastructure
}