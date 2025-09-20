# Docker Setup Script for Clean Architecture Project
# This script helps set up and manage Docker containers for different environments

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("dev", "test", "prod")]
    [string]$Environment = "dev",
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("up", "down", "build", "logs", "clean", "health")]
    [string]$Action = "up",
    
    [Parameter(Mandatory=$false)]
    [switch]$Detached = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$Build = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$Verbose = $false
)

# Configuration
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$DockerPath = Join-Path $ProjectRoot "docker"

# Color functions
function Write-ColorOutput($ForegroundColor) {
    $fc = $host.UI.RawUI.ForegroundColor
    $host.UI.RawUI.ForegroundColor = $ForegroundColor
    if ($args) {
        Write-Output $args
    } else {
        $input | Write-Output
    }
    $host.UI.RawUI.ForegroundColor = $fc
}

function Write-Info($message) {
    Write-ColorOutput Green "[INFO] $message"
}

function Write-Warning($message) {
    Write-ColorOutput Yellow "[WARNING] $message"
}

function Write-Error($message) {
    Write-ColorOutput Red "[ERROR] $message"
}

# Function to check Docker installation
function Test-DockerInstallation {
    try {
        $dockerVersion = docker --version 2>$null
        $composeVersion = docker compose version 2>$null
        
        if ($dockerVersion -and $composeVersion) {
            Write-Info "Docker is installed: $dockerVersion"
            Write-Info "Docker Compose is installed: $composeVersion"
            return $true
        } else {
            Write-Error "Docker or Docker Compose is not installed or not accessible"
            return $false
        }
    } catch {
        Write-Error "Failed to check Docker installation: $_"
        return $false
    }
}

# Function to get compose file based on environment
function Get-ComposeFiles($env) {
    $baseFile = Join-Path $DockerPath "docker-compose.yml"
    
    switch ($env) {
        "dev" {
            return @($baseFile, (Join-Path $DockerPath "docker-compose.override.yml"))
        }
        "test" {
            return @($baseFile, (Join-Path $DockerPath "docker-compose.test.yml"))
        }
        "prod" {
            return @((Join-Path $DockerPath "docker-compose.prod.yml"))
        }
        default {
            return @($baseFile)
        }
    }
}

# Function to build compose command
function Build-ComposeCommand($env, $action, $additionalArgs = @()) {
    $composeFiles = Get-ComposeFiles $env
    $cmd = @("docker", "compose")
    
    foreach ($file in $composeFiles) {
        if (Test-Path $file) {
            $cmd += @("-f", $file)
        } else {
            Write-Warning "Compose file not found: $file"
        }
    }
    
    $cmd += @("--project-name", "cleanarch-$env")
    $cmd += $action
    $cmd += $additionalArgs
    
    return $cmd
}

# Function to start services
function Start-Services($env) {
    Write-Info "Starting services for environment: $env"
    
    $additionalArgs = @()
    if ($Detached) { $additionalArgs += "--detach" }
    if ($Build) { $additionalArgs += "--build" }
    if ($Verbose) { $additionalArgs += "--verbose" }
    
    $cmd = Build-ComposeCommand $env "up" $additionalArgs
    
    Write-Info "Executing: $($cmd -join ' ')"
    & $cmd[0] $cmd[1..($cmd.Length-1)]
    
    if ($LASTEXITCODE -eq 0) {
        Write-Info "Services started successfully"
        Start-Sleep 5
        Show-HealthStatus $env
    } else {
        Write-Error "Failed to start services"
    }
}

# Function to stop services
function Stop-Services($env) {
    Write-Info "Stopping services for environment: $env"
    
    $cmd = Build-ComposeCommand $env "down" @("--volumes", "--remove-orphans")
    
    Write-Info "Executing: $($cmd -join ' ')"
    & $cmd[0] $cmd[1..($cmd.Length-1)]
    
    if ($LASTEXITCODE -eq 0) {
        Write-Info "Services stopped successfully"
    } else {
        Write-Error "Failed to stop services"
    }
}

# Function to build images
function Build-Images($env) {
    Write-Info "Building images for environment: $env"
    
    $additionalArgs = @("--no-cache")
    if ($Verbose) { $additionalArgs += "--verbose" }
    
    $cmd = Build-ComposeCommand $env "build" $additionalArgs
    
    Write-Info "Executing: $($cmd -join ' ')"
    & $cmd[0] $cmd[1..($cmd.Length-1)]
    
    if ($LASTEXITCODE -eq 0) {
        Write-Info "Images built successfully"
    } else {
        Write-Error "Failed to build images"
    }
}

# Function to show logs
function Show-Logs($env) {
    Write-Info "Showing logs for environment: $env"
    
    $cmd = Build-ComposeCommand $env "logs" @("--follow", "--tail=100")
    
    Write-Info "Executing: $($cmd -join ' ')"
    & $cmd[0] $cmd[1..($cmd.Length-1)]
}

# Function to clean up
function Clean-Environment($env) {
    Write-Info "Cleaning up environment: $env"
    
    # Stop and remove containers
    $cmd = Build-ComposeCommand $env "down" @("--volumes", "--remove-orphans", "--rmi", "local")
    & $cmd[0] $cmd[1..($cmd.Length-1)]
    
    # Remove unused images and volumes
    Write-Info "Removing unused Docker resources..."
    docker system prune -f
    docker volume prune -f
    
    Write-Info "Cleanup completed"
}

# Function to check health status
function Show-HealthStatus($env) {
    Write-Info "Checking health status for environment: $env"
    
    $cmd = Build-ComposeCommand $env "ps"
    & $cmd[0] $cmd[1..($cmd.Length-1)]
    
    # Check individual service health
    $services = @("cleanarch-api", "sqlserver", "redis", "rabbitmq")
    
    foreach ($service in $services) {
        $containerName = "cleanarch-$service"
        if ($env -ne "prod") { $containerName += "-$env" }
        
        try {
            $health = docker inspect --format='{{.State.Health.Status}}' $containerName 2>$null
            if ($health) {
                $color = if ($health -eq "healthy") { "Green" } else { "Red" }
                Write-ColorOutput $color "  $service`: $health"
            } else {
                $status = docker inspect --format='{{.State.Status}}' $containerName 2>$null
                if ($status) {
                    $color = if ($status -eq "running") { "Yellow" } else { "Red" }
                    Write-ColorOutput $color "  $service`: $status (no health check)"
                }
            }
        } catch {
            Write-ColorOutput Red "  $service`: not found"
        }
    }
}

# Main execution
function Main {
    Write-Info "Clean Architecture Docker Setup"
    Write-Info "Environment: $Environment"
    Write-Info "Action: $Action"
    
    # Check Docker installation
    if (-not (Test-DockerInstallation)) {
        Write-Error "Docker is not properly installed. Please install Docker Desktop and try again."
        exit 1
    }
    
    # Change to project root directory
    Set-Location $ProjectRoot
    
    # Execute action
    switch ($Action) {
        "up" {
            Start-Services $Environment
        }
        "down" {
            Stop-Services $Environment
        }
        "build" {
            Build-Images $Environment
        }
        "logs" {
            Show-Logs $Environment
        }
        "clean" {
            Clean-Environment $Environment
        }
        "health" {
            Show-HealthStatus $Environment
        }
        default {
            Write-Error "Unknown action: $Action"
            exit 1
        }
    }
}

# Run main function
Main