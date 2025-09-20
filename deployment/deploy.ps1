#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Deployment script for Clean Architecture .NET project
.DESCRIPTION
    This script handles deployment to different environments (staging, production)
.PARAMETER Environment
    Target environment (staging/production). Default is staging
.PARAMETER Tag
    Docker image tag to deploy. Default is latest
.PARAMETER ConfigFile
    Path to environment-specific configuration file
.PARAMETER DryRun
    Perform a dry run without actually deploying
#>

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("staging", "production")]
    [string]$Environment = "staging",
    
    [Parameter(Mandatory=$false)]
    [string]$Tag = "latest",
    
    [Parameter(Mandatory=$false)]
    [string]$ConfigFile = "",
    
    [Parameter(Mandatory=$false)]
    [switch]$DryRun
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
Write-ColorOutput "🔍 Checking deployment prerequisites..." $Blue

$requiredCommands = @("docker", "docker-compose")
foreach ($cmd in $requiredCommands) {
    if (-not (Test-Command $cmd)) {
        Write-ColorOutput "❌ $cmd not found. Please install Docker and Docker Compose." $Red
        exit 1
    }
}

Write-ColorOutput "✅ Prerequisites check passed" $Green

# Set configuration file if not provided
if ([string]::IsNullOrEmpty($ConfigFile)) {
    $ConfigFile = "deployment/.env.$Environment"
}

# Check if configuration file exists
if (-not (Test-Path $ConfigFile)) {
    Write-ColorOutput "❌ Configuration file not found: $ConfigFile" $Red
    Write-ColorOutput "Please create the environment configuration file with required variables." $Yellow
    exit 1
}

Write-ColorOutput "📋 Using configuration file: $ConfigFile" $Blue

# Load environment variables
Get-Content $ConfigFile | ForEach-Object {
    if ($_ -match '^([^#][^=]+)=(.*)$') {
        $name = $matches[1].Trim()
        $value = $matches[2].Trim()
        [Environment]::SetEnvironmentVariable($name, $value, "Process")
        Write-ColorOutput "  $name = $value" $Blue
    }
}

# Set the image tag
[Environment]::SetEnvironmentVariable("TAG", $Tag, "Process")
Write-ColorOutput "🏷️ Deploying with tag: $Tag" $Blue

# Determine compose file
$composeFile = switch ($Environment) {
    "staging" { "deployment/docker-compose.staging.yml" }
    "production" { "deployment/docker-compose.production.yml" }
    default { "deployment/docker-compose.staging.yml" }
}

if (-not (Test-Path $composeFile)) {
    Write-ColorOutput "❌ Compose file not found: $composeFile" $Red
    exit 1
}

Write-ColorOutput "🐳 Using compose file: $composeFile" $Blue

if ($DryRun) {
    Write-ColorOutput "🔍 DRY RUN MODE - No actual deployment will occur" $Yellow
    Write-ColorOutput "Would execute: docker-compose -f $composeFile --env-file $ConfigFile up -d" $Blue
    exit 0
}

# Pre-deployment checks
Write-ColorOutput "🔍 Running pre-deployment checks..." $Blue

# Check if services are already running
$runningServices = docker-compose -f $composeFile ps --services --filter "status=running" 2>$null
if ($runningServices) {
    Write-ColorOutput "⚠️ Some services are already running:" $Yellow
    $runningServices | ForEach-Object { Write-ColorOutput "  - $_" $Yellow }
    
    $response = Read-Host "Do you want to continue and update running services? (y/N)"
    if ($response -ne "y" -and $response -ne "Y") {
        Write-ColorOutput "❌ Deployment cancelled by user" $Red
        exit 1
    }
}

# Pull latest images
Write-ColorOutput "📥 Pulling latest images..." $Blue
docker-compose -f $composeFile --env-file $ConfigFile pull
if ($LASTEXITCODE -ne 0) {
    Write-ColorOutput "❌ Failed to pull images" $Red
    exit 1
}

# Create backup of current deployment (if exists)
$backupDir = "deployment/backups/$(Get-Date -Format 'yyyyMMdd-HHmmss')"
if (Test-Path "deployment/current") {
    Write-ColorOutput "💾 Creating backup..." $Blue
    New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
    Copy-Item -Path "deployment/current/*" -Destination $backupDir -Recurse -Force
    Write-ColorOutput "✅ Backup created at: $backupDir" $Green
}

# Deploy services
Write-ColorOutput "🚀 Starting deployment to $Environment..." $Blue

try {
    # Start services
    docker-compose -f $composeFile --env-file $ConfigFile up -d --remove-orphans
    if ($LASTEXITCODE -ne 0) {
        throw "Docker compose up failed"
    }
    
    Write-ColorOutput "⏳ Waiting for services to be healthy..." $Yellow
    
    # Wait for health checks
    $maxWaitTime = 300 # 5 minutes
    $waitTime = 0
    $interval = 10
    
    do {
        Start-Sleep -Seconds $interval
        $waitTime += $interval
        
        $healthyServices = docker-compose -f $composeFile ps --services --filter "status=running" | Measure-Object | Select-Object -ExpandProperty Count
        $totalServices = docker-compose -f $composeFile config --services | Measure-Object | Select-Object -ExpandProperty Count
        
        Write-ColorOutput "Health check: $healthyServices/$totalServices services running (${waitTime}s elapsed)" $Blue
        
        if ($healthyServices -eq $totalServices) {
            break
        }
        
        if ($waitTime -ge $maxWaitTime) {
            throw "Services did not become healthy within $maxWaitTime seconds"
        }
    } while ($true)
    
    # Verify deployment
    Write-ColorOutput "🔍 Verifying deployment..." $Blue
    
    # Check API health endpoint
    $apiHealthUrl = "http://localhost:8080/health"
    try {
        $response = Invoke-RestMethod -Uri $apiHealthUrl -TimeoutSec 30
        if ($response.status -eq "Healthy") {
            Write-ColorOutput "✅ API health check passed" $Green
        } else {
            Write-ColorOutput "⚠️ API health check returned: $($response.status)" $Yellow
        }
    }
    catch {
        Write-ColorOutput "⚠️ API health check failed: $($_.Exception.Message)" $Yellow
    }
    
    # Save current deployment info
    $deploymentInfo = @{
        Environment = $Environment
        Tag = $Tag
        DeploymentTime = Get-Date -Format "yyyy-MM-dd HH:mm:ss UTC"
        ConfigFile = $ConfigFile
        ComposeFile = $composeFile
        GitCommit = if (Test-Command "git") { git rev-parse HEAD } else { "unknown" }
        GitBranch = if (Test-Command "git") { git rev-parse --abbrev-ref HEAD } else { "unknown" }
    } | ConvertTo-Json -Depth 2
    
    New-Item -ItemType Directory -Path "deployment/current" -Force | Out-Null
    $deploymentInfo | Out-File -FilePath "deployment/current/deployment-info.json" -Encoding UTF8
    
    Write-ColorOutput "🎉 Deployment completed successfully!" $Green
    Write-ColorOutput "🌐 Application should be available at: http://localhost" $Blue
    Write-ColorOutput "📊 Management interfaces:" $Blue
    Write-ColorOutput "  - RabbitMQ: http://localhost:15672" $Blue
    Write-ColorOutput "  - API Health: http://localhost:8080/health" $Blue
}
catch {
    Write-ColorOutput "❌ Deployment failed: $($_.Exception.Message)" $Red
    
    Write-ColorOutput "🔄 Rolling back..." $Yellow
    docker-compose -f $composeFile --env-file $ConfigFile down
    
    if (Test-Path $backupDir) {
        Write-ColorOutput "💾 Backup available at: $backupDir" $Blue
        Write-ColorOutput "To restore manually, copy files from backup to deployment/current/" $Blue
    }
    
    exit 1
}

Write-ColorOutput "📋 Deployment Summary:" $Blue
Write-ColorOutput "  Environment: $Environment" $Blue
Write-ColorOutput "  Tag: $Tag" $Blue
Write-ColorOutput "  Config: $ConfigFile" $Blue
Write-ColorOutput "  Compose: $composeFile" $Blue