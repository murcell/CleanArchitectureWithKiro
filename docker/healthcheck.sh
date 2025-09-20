#!/bin/bash

# Health check script for Clean Architecture API
# This script performs comprehensive health checks

set -e

# Configuration
API_URL="http://localhost:8080"
HEALTH_ENDPOINT="/api/health/live"
READY_ENDPOINT="/api/health/ready"
TIMEOUT=10
MAX_RETRIES=3

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Logging function
log() {
    echo -e "${GREEN}[$(date +'%Y-%m-%d %H:%M:%S')]${NC} $1"
}

error() {
    echo -e "${RED}[$(date +'%Y-%m-%d %H:%M:%S')] ERROR:${NC} $1" >&2
}

warn() {
    echo -e "${YELLOW}[$(date +'%Y-%m-%d %H:%M:%S')] WARNING:${NC} $1"
}

# Function to check if service is responding
check_endpoint() {
    local endpoint=$1
    local description=$2
    local retry_count=0
    
    while [ $retry_count -lt $MAX_RETRIES ]; do
        if curl -f -s --max-time $TIMEOUT "${API_URL}${endpoint}" > /dev/null 2>&1; then
            log "$description check passed"
            return 0
        fi
        
        retry_count=$((retry_count + 1))
        if [ $retry_count -lt $MAX_RETRIES ]; then
            warn "$description check failed, retrying ($retry_count/$MAX_RETRIES)..."
            sleep 2
        fi
    done
    
    error "$description check failed after $MAX_RETRIES attempts"
    return 1
}

# Function to check detailed health
check_detailed_health() {
    local response
    response=$(curl -f -s --max-time $TIMEOUT "${API_URL}/api/health" 2>/dev/null)
    
    if [ $? -eq 0 ]; then
        local status
        status=$(echo "$response" | grep -o '"Status":"[^"]*"' | cut -d'"' -f4)
        
        if [ "$status" = "Healthy" ]; then
            log "Detailed health check passed - Status: $status"
            return 0
        else
            warn "Detailed health check shows degraded status: $status"
            echo "$response" | jq '.' 2>/dev/null || echo "$response"
            return 1
        fi
    else
        error "Detailed health check failed - no response"
        return 1
    fi
}

# Main health check logic
main() {
    log "Starting health check for Clean Architecture API..."
    
    # Check if curl is available
    if ! command -v curl &> /dev/null; then
        error "curl is not available. Installing..."
        apk add --no-cache curl || apt-get update && apt-get install -y curl || {
            error "Failed to install curl"
            exit 1
        }
    fi
    
    # Perform liveness check
    if ! check_endpoint "$HEALTH_ENDPOINT" "Liveness"; then
        exit 1
    fi
    
    # Perform readiness check
    if ! check_endpoint "$READY_ENDPOINT" "Readiness"; then
        exit 1
    fi
    
    # Perform detailed health check (optional, for monitoring)
    if [ "${DETAILED_HEALTH_CHECK:-false}" = "true" ]; then
        check_detailed_health || warn "Detailed health check failed, but continuing..."
    fi
    
    log "All health checks passed successfully"
    exit 0
}

# Handle script termination
trap 'error "Health check interrupted"; exit 1' INT TERM

# Run main function
main "$@"