#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Test automation script for Clean Architecture .NET project
.DESCRIPTION
    This script runs all types of tests with coverage reporting and result analysis
.PARAMETER TestType
    Type of tests to run (Unit, Integration, Performance, All). Default is All
.PARAMETER Configuration
    Build configuration (Debug/Release). Default is Release
.PARAMETER Coverage
    Generate code coverage report
.PARAMETER OutputPath
    Output path for test results and coverage reports
#>

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Unit", "Integration", "Performance", "All")]
    [string]$TestType = "All",
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    
    [Parameter(Mandatory=$false)]
    [switch]$Coverage,
    
    [Parameter(Mandatory=$false)]
    [string]$OutputPath = "./test-results"
)

$ErrorActionPreference = "Stop"

# Colors for output
$Green = "`e[32m"
$Red = "`e[31m"
$Yellow = "`e[33m"
$Blue = "`e[34m"
$Reset = "`e[0m"

function Write-ColorOutput {
    param([string]$Message, [string]$Color = $Reset)
    Write-Host "$Color$Message$Reset"
}

function Test-Command {
    param([string]$Command)
    return Get-Command $Command -ErrorAction SilentlyContinue
}

# Check prerequisites
Write-ColorOutput "🔍 Checking test prerequisites..." $Blue

if (-not (Test-Command "dotnet")) {
    Write-ColorOutput "❌ .NET SDK not found." $Red
    exit 1
}

# Clean and create output directory
if (Test-Path $OutputPath) {
    Remove-Item -Path $OutputPath -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null

# Build solution first
Write-ColorOutput "🔨 Building solution for tests..." $Blue
dotnet build --configuration $Configuration --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    Write-ColorOutput "❌ Build failed" $Red
    exit 1
}

$testResults = @()
$totalTests = 0
$passedTests = 0
$failedTests = 0

function Run-TestProject {
    param(
        [string]$ProjectPath,
        [string]$TestName,
        [string]$LogFileName
    )
    
    Write-ColorOutput "🧪 Running $TestName tests..." $Yellow
    
    $coverageArgs = ""
    if ($Coverage) {
        $coverageArgs = "--collect:`"XPlat Code Coverage`" --settings:coverlet.runsettings"
    }
    
    $testCommand = "dotnet test `"$ProjectPath`" --configuration $Configuration --no-build --verbosity normal --logger `"trx;LogFileName=$LogFileName`" --results-directory `"$OutputPath`" $coverageArgs"
    
    Write-ColorOutput "Executing: $testCommand" $Blue
    
    try {
        Invoke-Expression $testCommand
        $exitCode = $LASTEXITCODE
        
        if ($exitCode -eq 0) {
            Write-ColorOutput "✅ $TestName tests passed" $Green
            return @{ Success = $true; ExitCode = $exitCode }
        } else {
            Write-ColorOutput "❌ $TestName tests failed (Exit code: $exitCode)" $Red
            return @{ Success = $false; ExitCode = $exitCode }
        }
    }
    catch {
        Write-ColorOutput "❌ Error running $TestName tests: $($_.Exception.Message)" $Red
        return @{ Success = $false; ExitCode = 1 }
    }
}

# Run tests based on type
$startTime = Get-Date

if ($TestType -eq "Unit" -or $TestType -eq "All") {
    Write-ColorOutput "🚀 Starting Unit Tests..." $Blue
    
    $projects = @(
        @{ Path = "tests/CleanArchitecture.Domain.Tests"; Name = "Domain Unit"; LogFile = "domain-unit-tests.trx" },
        @{ Path = "tests/CleanArchitecture.Application.Tests"; Name = "Application Unit"; LogFile = "application-unit-tests.trx" },
        @{ Path = "tests/CleanArchitecture.Infrastructure.Tests"; Name = "Infrastructure Unit"; LogFile = "infrastructure-unit-tests.trx" }
    )
    
    foreach ($project in $projects) {
        if (Test-Path $project.Path) {
            $result = Run-TestProject -ProjectPath $project.Path -TestName $project.Name -LogFileName $project.LogFile
            $testResults += @{ Project = $project.Name; Result = $result }
        }
    }
}

if ($TestType -eq "Integration" -or $TestType -eq "All") {
    Write-ColorOutput "🚀 Starting Integration Tests..." $Blue
    
    # Start test dependencies if needed
    Write-ColorOutput "🐳 Starting test dependencies..." $Yellow
    docker-compose -f docker/docker-compose.test.yml up -d --wait
    
    try {
        $result = Run-TestProject -ProjectPath "tests/CleanArchitecture.WebAPI.Tests" -TestName "Integration" -LogFileName "integration-tests.trx"
        $testResults += @{ Project = "Integration"; Result = $result }
    }
    finally {
        Write-ColorOutput "🐳 Stopping test dependencies..." $Yellow
        docker-compose -f docker/docker-compose.test.yml down
    }
}

if ($TestType -eq "Performance" -or $TestType -eq "All") {
    Write-ColorOutput "🚀 Starting Performance Tests..." $Blue
    
    if (Test-Path "tests/CleanArchitecture.Performance.Tests") {
        $result = Run-TestProject -ProjectPath "tests/CleanArchitecture.Performance.Tests" -TestName "Performance" -LogFileName "performance-tests.trx"
        $testResults += @{ Project = "Performance"; Result = $result }
    }
}

$endTime = Get-Date
$duration = $endTime - $startTime

# Generate test summary
Write-ColorOutput "📊 Test Summary" $Blue
Write-ColorOutput "=================" $Blue
Write-ColorOutput "Duration: $($duration.ToString('mm\:ss'))" $Blue

$allPassed = $true
foreach ($testResult in $testResults) {
    $status = if ($testResult.Result.Success) { "✅ PASSED" } else { "❌ FAILED"; $allPassed = $false }
    Write-ColorOutput "$($testResult.Project): $status" $(if ($testResult.Result.Success) { $Green } else { $Red })
}

# Generate coverage report if requested
if ($Coverage) {
    Write-ColorOutput "📈 Generating coverage report..." $Blue
    
    if (Test-Command "reportgenerator") {
        $coverageFiles = Get-ChildItem -Path $OutputPath -Filter "coverage.cobertura.xml" -Recurse
        if ($coverageFiles.Count -gt 0) {
            $coverageFilePaths = ($coverageFiles | ForEach-Object { $_.FullName }) -join ";"
            reportgenerator -reports:$coverageFilePaths -targetdir:"$OutputPath/coverage-report" -reporttypes:"Html;Cobertura;JsonSummary"
            Write-ColorOutput "✅ Coverage report generated in $OutputPath/coverage-report" $Green
        }
    } else {
        Write-ColorOutput "⚠️ ReportGenerator not found. Install with: dotnet tool install -g dotnet-reportgenerator-globaltool" $Yellow
    }
}

# Create test summary JSON
$summary = @{
    TestRun = @{
        StartTime = $startTime.ToString("yyyy-MM-dd HH:mm:ss UTC")
        EndTime = $endTime.ToString("yyyy-MM-dd HH:mm:ss UTC")
        Duration = $duration.TotalSeconds
        TestType = $TestType
        Configuration = $Configuration
    }
    Results = $testResults
    Success = $allPassed
} | ConvertTo-Json -Depth 3

$summary | Out-File -FilePath "$OutputPath/test-summary.json" -Encoding UTF8

if ($allPassed) {
    Write-ColorOutput "🎉 All tests completed successfully!" $Green
    exit 0
} else {
    Write-ColorOutput "❌ Some tests failed. Check the results above." $Red
    exit 1
}