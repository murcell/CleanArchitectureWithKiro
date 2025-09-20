#!/bin/bash

# Build script for Clean Architecture .NET project (Linux/macOS)
# Usage: ./build.sh [configuration] [skip-tests] [output-path]

set -e

# Default values
CONFIGURATION=${1:-Release}
SKIP_TESTS=${2:-false}
OUTPUT_PATH=${3:-./artifacts}

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

print_color() {
    printf "${2}${1}${NC}\n"
}

check_command() {
    if ! command -v $1 &> /dev/null; then
        return 1
    fi
    return 0
}

# Check prerequisites
print_color "🔍 Checking prerequisites..." $BLUE

if ! check_command dotnet; then
    print_color "❌ .NET SDK not found. Please install .NET 9 SDK." $RED
    exit 1
fi

DOTNET_VERSION=$(dotnet --version)
print_color "✅ .NET SDK version: $DOTNET_VERSION" $GREEN

# Clean previous builds
print_color "🧹 Cleaning previous builds..." $BLUE
rm -rf $OUTPUT_PATH
mkdir -p $OUTPUT_PATH

dotnet clean --configuration $CONFIGURATION --verbosity minimal

# Restore packages
print_color "📦 Restoring NuGet packages..." $BLUE
dotnet restore --verbosity minimal

# Build solution
print_color "🔨 Building solution..." $BLUE
dotnet build --configuration $CONFIGURATION --no-restore --verbosity minimal

print_color "✅ Build completed successfully" $GREEN

# Run tests if not skipped
if [ "$SKIP_TESTS" != "true" ]; then
    print_color "🧪 Running tests..." $BLUE
    
    # Run unit tests
    print_color "Running unit tests..." $YELLOW
    dotnet test --configuration $CONFIGURATION --no-build --verbosity minimal --logger "trx;LogFileName=unit-tests.trx" --results-directory "$OUTPUT_PATH/test-results"
    
    # Run integration tests
    print_color "Running integration tests..." $YELLOW
    dotnet test tests/CleanArchitecture.WebAPI.Tests --configuration $CONFIGURATION --no-build --verbosity minimal --logger "trx;LogFileName=integration-tests.trx" --results-directory "$OUTPUT_PATH/test-results"
    
    print_color "✅ All tests passed" $GREEN
fi

# Publish applications
print_color "📦 Publishing applications..." $BLUE

# Publish Web API
dotnet publish src/CleanArchitecture.WebAPI --configuration $CONFIGURATION --no-build --output "$OUTPUT_PATH/webapi" --verbosity minimal

print_color "✅ Applications published successfully" $GREEN

# Generate build info
BUILD_TIME=$(date -u +"%Y-%m-%d %H:%M:%S UTC")
GIT_COMMIT=$(git rev-parse HEAD 2>/dev/null || echo "unknown")
GIT_BRANCH=$(git rev-parse --abbrev-ref HEAD 2>/dev/null || echo "unknown")

cat > "$OUTPUT_PATH/build-info.json" << EOF
{
  "BuildTime": "$BUILD_TIME",
  "Configuration": "$CONFIGURATION",
  "DotNetVersion": "$DOTNET_VERSION",
  "GitCommit": "$GIT_COMMIT",
  "GitBranch": "$GIT_BRANCH"
}
EOF

print_color "📋 Build information saved to $OUTPUT_PATH/build-info.json" $BLUE
print_color "🎉 Build process completed successfully!" $GREEN
print_color "📁 Artifacts available in: $OUTPUT_PATH" $BLUE