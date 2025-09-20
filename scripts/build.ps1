#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Build script for Clean Architecture .NET project
.DESCRIPTION
    This script builds the entire solution, runs tests, and prepares artifacts for deployment
.PARAMETER Configuration
    Build configuration (Debug/Release). Default is Release
.PARAMETER SkipTests
    Skip running tests during build
.PARAMETER OutputPath
    Output path for build artifacts
#>

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipTests,
    
    [Parameter(Mandatory=$false)]
    [string]$OutputPath = "./artifacts"
)

# Set error action preference
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
Write-ColorOutput "🔍 Checking prerequisites..." $Blue

if (-not (Test-Command "dotnet")) {
    Write-ColorOutput "❌ .NET SDK not found. Please install .NET 9 SDK." $Red
    exit 1
}

$dotnetVersion = dotnet --version
Write-ColorOutput "✅ .NET SDK version: $dotnetVersion" $Green

# Clean previous builds
Write-ColorOutput "🧹 Cleaning previous builds..." $Blue
if (Test-Path $OutputPath) {
    Remove-Item -Path $OutputPath -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null

dotnet clean --configuration $Configuration --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    Write-ColorOutput "❌ Clean failed" $Red
    exit 1
}

# Restore packages
Write-ColorOutput "📦 Restoring NuGet packages..." $Blue
dotnet restore --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    Write-ColorOutput "❌ Package restore failed" $Red
    exit 1
}

# Build solution
Write-ColorOutput "🔨 Building solution..." $Blue
dotnet build --configuration $Configuration --no-restore --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    Write-ColorOutput "❌ Build failed" $Red
    exit 1
}

Write-ColorOutput "✅ Build completed successfully" $Green

# Run tests if not skipped
if (-not $SkipTests) {
    Write-ColorOutput "🧪 Running tests..." $Blue
    
    # Run unit tests
    Write-ColorOutput "Running unit tests..." $Yellow
    dotnet test --configuration $Configuration --no-build --verbosity minimal --logger "trx;LogFileName=unit-tests.trx" --results-directory "$OutputPath/test-results"
    if ($LASTEXITCODE -ne 0) {
        Write-ColorOutput "❌ Unit tests failed" $Red
        exit 1
    }
    
    # Run integration tests
    Write-ColorOutput "Running integration tests..." $Yellow
    dotnet test tests/CleanArchitecture.WebAPI.Tests --configuration $Configuration --no-build --verbosity minimal --logger "trx;LogFileName=integration-tests.trx" --results-directory "$OutputPath/test-results"
    if ($LASTEXITCODE -ne 0) {
        Write-ColorOutput "❌ Integration tests failed" $Red
        exit 1
    }
    
    Write-ColorOutput "✅ All tests passed" $Green
}

# Publish applications
Write-ColorOutput "📦 Publishing applications..." $Blue

# Publish Web API
dotnet publish src/CleanArchitecture.WebAPI --configuration $Configuration --no-build --output "$OutputPath/webapi" --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    Write-ColorOutput "❌ Web API publish failed" $Red
    exit 1
}

Write-ColorOutput "✅ Applications published successfully" $Green

# Generate build info
$buildInfo = @{
    BuildTime = Get-Date -Format "yyyy-MM-dd HH:mm:ss UTC"
    Configuration = $Configuration
    DotNetVersion = $dotnetVersion
    GitCommit = if (Test-Command "git") { git rev-parse HEAD } else { "unknown" }
    GitBranch = if (Test-Command "git") { git rev-parse --abbrev-ref HEAD } else { "unknown" }
} | ConvertTo-Json -Depth 2

$buildInfo | Out-File -FilePath "$OutputPath/build-info.json" -Encoding UTF8

Write-ColorOutput "📋 Build information saved to $OutputPath/build-info.json" $Blue
Write-ColorOutput "🎉 Build process completed successfully!" $Green
Write-ColorOutput "📁 Artifacts available in: $OutputPath" $Blue