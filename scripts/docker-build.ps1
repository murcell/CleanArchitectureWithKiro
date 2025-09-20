#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Docker build script for Clean Architecture .NET project
.DESCRIPTION
    This script builds Docker images for the application with proper tagging and optimization
.PARAMETER Tag
    Docker image tag. Default is latest
.PARAMETER Registry
    Docker registry URL. Default is local
.PARAMETER Push
    Push image to registry after build
.PARAMETER Platform
    Target platform(s) for multi-arch builds. Default is linux/amd64
#>

param(
    [Parameter(Mandatory=$false)]
    [string]$Tag = "latest",
    
    [Parameter(Mandatory=$false)]
    [string]$Registry = "",
    
    [Parameter(Mandatory=$false)]
    [switch]$Push,
    
    [Parameter(Mandatory=$false)]
    [string]$Platform = "linux/amd64"
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
Write-ColorOutput "🔍 Checking Docker prerequisites..." $Blue

if (-not (Test-Command "docker")) {
    Write-ColorOutput "❌ Docker not found. Please install Docker." $Red
    exit 1
}

# Check if Docker is running
try {
    docker version | Out-Null
    Write-ColorOutput "✅ Docker is running" $Green
}
catch {
    Write-ColorOutput "❌ Docker is not running. Please start Docker." $Red
    exit 1
}

# Determine image name
$imageName = "cleanarchitecture/webapi"
if (-not [string]::IsNullOrEmpty($Registry)) {
    $imageName = "$Registry/$imageName"
}

$fullImageName = "${imageName}:${Tag}"

Write-ColorOutput "🐳 Building Docker image: $fullImageName" $Blue
Write-ColorOutput "📋 Build configuration:" $Blue
Write-ColorOutput "  Image: $fullImageName" $Blue
Write-ColorOutput "  Platform: $Platform" $Blue
Write-ColorOutput "  Push: $Push" $Blue

# Build arguments
$buildArgs = @(
    "--file", "docker/Dockerfile",
    "--tag", $fullImageName,
    "--platform", $Platform,
    "--build-arg", "BUILDKIT_INLINE_CACHE=1"
)

# Add cache arguments for better build performance
if (-not [string]::IsNullOrEmpty($Registry)) {
    $buildArgs += @(
        "--cache-from", "${imageName}:cache",
        "--cache-to", "${imageName}:cache,mode=max"
    )
}

# Add labels
$gitCommit = if (Test-Command "git") { git rev-parse HEAD } else { "unknown" }
$gitBranch = if (Test-Command "git") { git rev-parse --abbrev-ref HEAD } else { "unknown" }
$buildTime = Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ"

$buildArgs += @(
    "--label", "org.opencontainers.image.created=$buildTime",
    "--label", "org.opencontainers.image.revision=$gitCommit",
    "--label", "org.opencontainers.image.source=https://github.com/yourusername/cleanarchitecture",
    "--label", "org.opencontainers.image.version=$Tag",
    "--label", "git.branch=$gitBranch",
    "--label", "git.commit=$gitCommit"
)

# Build the image
Write-ColorOutput "🔨 Starting Docker build..." $Blue

try {
    $buildCommand = "docker build $($buildArgs -join ' ') ."
    Write-ColorOutput "Executing: $buildCommand" $Blue
    
    Invoke-Expression $buildCommand
    
    if ($LASTEXITCODE -ne 0) {
        throw "Docker build failed with exit code $LASTEXITCODE"
    }
    
    Write-ColorOutput "✅ Docker image built successfully" $Green
}
catch {
    Write-ColorOutput "❌ Docker build failed: $($_.Exception.Message)" $Red
    exit 1
}

# Verify the image
Write-ColorOutput "🔍 Verifying Docker image..." $Blue
$imageInfo = docker inspect $fullImageName | ConvertFrom-Json
$imageSize = [math]::Round($imageInfo[0].Size / 1MB, 2)
Write-ColorOutput "✅ Image size: ${imageSize} MB" $Green

# Security scan (if available)
if (Test-Command "docker") {
    Write-ColorOutput "🔒 Running security scan..." $Blue
    try {
        docker run --rm -v /var/run/docker.sock:/var/run/docker.sock aquasec/trivy:latest image --exit-code 0 --severity HIGH,CRITICAL $fullImageName
        Write-ColorOutput "✅ Security scan completed" $Green
    }
    catch {
        Write-ColorOutput "⚠️ Security scan failed or not available" $Yellow
    }
}

# Push to registry if requested
if ($Push) {
    if ([string]::IsNullOrEmpty($Registry)) {
        Write-ColorOutput "⚠️ No registry specified, skipping push" $Yellow
    }
    else {
        Write-ColorOutput "📤 Pushing image to registry..." $Blue
        
        try {
            docker push $fullImageName
            if ($LASTEXITCODE -ne 0) {
                throw "Docker push failed with exit code $LASTEXITCODE"
            }
            Write-ColorOutput "✅ Image pushed successfully" $Green
        }
        catch {
            Write-ColorOutput "❌ Docker push failed: $($_.Exception.Message)" $Red
            exit 1
        }
    }
}

# Generate image manifest
$manifest = @{
    ImageName = $fullImageName
    Tag = $Tag
    Platform = $Platform
    BuildTime = $buildTime
    GitCommit = $gitCommit
    GitBranch = $gitBranch
    SizeMB = $imageSize
    Registry = $Registry
    Pushed = $Push.IsPresent
} | ConvertTo-Json -Depth 2

$manifest | Out-File -FilePath "docker-manifest.json" -Encoding UTF8

Write-ColorOutput "🎉 Docker build completed successfully!" $Green
Write-ColorOutput "📋 Image details:" $Blue
Write-ColorOutput "  Name: $fullImageName" $Blue
Write-ColorOutput "  Size: ${imageSize} MB" $Blue
Write-ColorOutput "  Platform: $Platform" $Blue
Write-ColorOutput "📄 Manifest saved to: docker-manifest.json" $Blue