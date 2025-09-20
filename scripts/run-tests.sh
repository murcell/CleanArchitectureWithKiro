#!/bin/bash

# Test automation script for Clean Architecture .NET project (Linux/macOS)
# Usage: ./run-tests.sh [test-type] [configuration] [coverage] [output-path]

set -e

# Default values
TEST_TYPE=${1:-All}
CONFIGURATION=${2:-Release}
COVERAGE=${3:-false}
OUTPUT_PATH=${4:-./test-results}

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
print_color "🔍 Checking test prerequisites..." $BLUE

if ! check_command dotnet; then
    print_color "❌ .NET SDK not found." $RED
    exit 1
fi

# Clean and create output directory
rm -rf $OUTPUT_PATH
mkdir -p $OUTPUT_PATH

# Build solution first
print_color "🔨 Building solution for tests..." $BLUE
dotnet build --configuration $CONFIGURATION --verbosity minimal

TEST_RESULTS=()
ALL_PASSED=true

run_test_project() {
    local project_path=$1
    local test_name=$2
    local log_file=$3
    
    print_color "🧪 Running $test_name tests..." $YELLOW
    
    local coverage_args=""
    if [ "$COVERAGE" = "true" ]; then
        coverage_args="--collect:\"XPlat Code Coverage\" --settings:coverlet.runsettings"
    fi
    
    local test_command="dotnet test \"$project_path\" --configuration $CONFIGURATION --no-build --verbosity normal --logger \"trx;LogFileName=$log_file\" --results-directory \"$OUTPUT_PATH\" $coverage_args"
    
    print_color "Executing: $test_command" $BLUE
    
    if eval $test_command; then
        print_color "✅ $test_name tests passed" $GREEN
        TEST_RESULTS+=("$test_name:PASSED")
        return 0
    else
        print_color "❌ $test_name tests failed" $RED
        TEST_RESULTS+=("$test_name:FAILED")
        ALL_PASSED=false
        return 1
    fi
}

# Record start time
START_TIME=$(date +%s)

# Run tests based on type
if [ "$TEST_TYPE" = "Unit" ] || [ "$TEST_TYPE" = "All" ]; then
    print_color "🚀 Starting Unit Tests..." $BLUE
    
    if [ -d "tests/CleanArchitecture.Domain.Tests" ]; then
        run_test_project "tests/CleanArchitecture.Domain.Tests" "Domain Unit" "domain-unit-tests.trx" || true
    fi
    
    if [ -d "tests/CleanArchitecture.Application.Tests" ]; then
        run_test_project "tests/CleanArchitecture.Application.Tests" "Application Unit" "application-unit-tests.trx" || true
    fi
    
    if [ -d "tests/CleanArchitecture.Infrastructure.Tests" ]; then
        run_test_project "tests/CleanArchitecture.Infrastructure.Tests" "Infrastructure Unit" "infrastructure-unit-tests.trx" || true
    fi
fi

if [ "$TEST_TYPE" = "Integration" ] || [ "$TEST_TYPE" = "All" ]; then
    print_color "🚀 Starting Integration Tests..." $BLUE
    
    # Start test dependencies if needed
    if check_command docker-compose; then
        print_color "🐳 Starting test dependencies..." $YELLOW
        docker-compose -f docker/docker-compose.test.yml up -d --wait
        
        # Wait a bit for services to be ready
        sleep 5
    fi
    
    if [ -d "tests/CleanArchitecture.WebAPI.Tests" ]; then
        run_test_project "tests/CleanArchitecture.WebAPI.Tests" "Integration" "integration-tests.trx" || true
    fi
    
    if check_command docker-compose; then
        print_color "🐳 Stopping test dependencies..." $YELLOW
        docker-compose -f docker/docker-compose.test.yml down
    fi
fi

if [ "$TEST_TYPE" = "Performance" ] || [ "$TEST_TYPE" = "All" ]; then
    print_color "🚀 Starting Performance Tests..." $BLUE
    
    if [ -d "tests/CleanArchitecture.Performance.Tests" ]; then
        run_test_project "tests/CleanArchitecture.Performance.Tests" "Performance" "performance-tests.trx" || true
    fi
fi

# Calculate duration
END_TIME=$(date +%s)
DURATION=$((END_TIME - START_TIME))
DURATION_MIN=$((DURATION / 60))
DURATION_SEC=$((DURATION % 60))

# Generate test summary
print_color "📊 Test Summary" $BLUE
print_color "=================" $BLUE
printf "${BLUE}Duration: %02d:%02d${NC}\n" $DURATION_MIN $DURATION_SEC

for result in "${TEST_RESULTS[@]}"; do
    IFS=':' read -r project status <<< "$result"
    if [ "$status" = "PASSED" ]; then
        print_color "$project: ✅ PASSED" $GREEN
    else
        print_color "$project: ❌ FAILED" $RED
    fi
done

# Generate coverage report if requested
if [ "$COVERAGE" = "true" ]; then
    print_color "📈 Generating coverage report..." $BLUE
    
    if check_command reportgenerator; then
        COVERAGE_FILES=$(find $OUTPUT_PATH -name "coverage.cobertura.xml" -type f)
        if [ -n "$COVERAGE_FILES" ]; then
            COVERAGE_FILE_PATHS=$(echo $COVERAGE_FILES | tr ' ' ';')
            reportgenerator -reports:$COVERAGE_FILE_PATHS -targetdir:$OUTPUT_PATH/coverage-report -reporttypes:Html\;Cobertura\;JsonSummary
            print_color "✅ Coverage report generated in $OUTPUT_PATH/coverage-report" $GREEN
        fi
    else
        print_color "⚠️ ReportGenerator not found. Install with: dotnet tool install -g dotnet-reportgenerator-globaltool" $YELLOW
    fi
fi

# Create test summary JSON
cat > "$OUTPUT_PATH/test-summary.json" << EOF
{
  "TestRun": {
    "StartTime": "$(date -d @$START_TIME -u +"%Y-%m-%d %H:%M:%S UTC")",
    "EndTime": "$(date -d @$END_TIME -u +"%Y-%m-%d %H:%M:%S UTC")",
    "Duration": $DURATION,
    "TestType": "$TEST_TYPE",
    "Configuration": "$CONFIGURATION"
  },
  "Results": [
$(IFS=$'\n'; for result in "${TEST_RESULTS[@]}"; do
    IFS=':' read -r project status <<< "$result"
    echo "    {\"Project\": \"$project\", \"Status\": \"$status\"}"
done | sed '$!s/$/,/')
  ],
  "Success": $([ "$ALL_PASSED" = true ] && echo "true" || echo "false")
}
EOF

if [ "$ALL_PASSED" = true ]; then
    print_color "🎉 All tests completed successfully!" $GREEN
    exit 0
else
    print_color "❌ Some tests failed. Check the results above." $RED
    exit 1
fi