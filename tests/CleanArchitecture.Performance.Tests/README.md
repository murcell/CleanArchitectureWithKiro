# Performance Tests

This project contains comprehensive performance tests for the Clean Architecture application, including load testing, database query performance testing, and cache performance testing.

## Test Categories

### 1. Load Tests (`LoadTests/`)
- **ApiLoadTests**: Tests API endpoints under various load conditions
- Uses NBomber for load testing framework
- Tests user workflow, product workflow, and health check endpoints
- Validates response times, error rates, and throughput

### 2. Database Performance Tests (`Database/`)
- **DatabasePerformanceTests**: Benchmarks database operations
- Uses BenchmarkDotNet for precise performance measurements
- Tests CRUD operations, complex queries, and batch operations
- Validates query execution times and database performance thresholds

### 3. Cache Performance Tests (`Caching/`)
- **CachePerformanceTests**: Benchmarks Redis cache operations
- Tests cache get/set operations, hit ratios, and expiration
- Validates cache response times and concurrent access performance

## Performance Thresholds

### API Performance
- Average response time: < 500ms for user operations
- Average response time: < 600ms for product operations
- Health check response time: < 100ms
- Error rate: < 1%

### Database Performance
- Single entity lookup: < 100ms
- Complex queries (100 records): < 1000ms
- Single insert: < 500ms
- Batch insert (10 records): < 2000ms
- Single update: < 300ms

### Cache Performance
- Cache get operation: < 50ms
- Cache set operation: < 100ms
- 10 concurrent cache gets: < 200ms
- Batch cache operations (10 items): < 500ms
- Cache hit ratio: ≥ 70%

## Running Performance Tests

### Prerequisites
- .NET 9 SDK
- Docker (for test containers)
- SQL Server, Redis, and RabbitMQ containers running

### Running All Tests
```bash
# Run all performance tests (xUnit tests)
dotnet test tests/CleanArchitecture.Performance.Tests/

# Run specific test categories
dotnet test tests/CleanArchitecture.Performance.Tests/ --filter "Category=LoadTest"
dotnet test tests/CleanArchitecture.Performance.Tests/ --filter "Category=DatabasePerformance"
dotnet test tests/CleanArchitecture.Performance.Tests/ --filter "Category=CachePerformance"
```

### Running Benchmarks
```bash
# Run all benchmarks
dotnet run --project tests/CleanArchitecture.Performance.Tests/ --configuration Release

# Run specific benchmarks
dotnet run --project tests/CleanArchitecture.Performance.Tests/ --configuration Release -- database
dotnet run --project tests/CleanArchitecture.Performance.Tests/ --configuration Release -- cache
```

### Running Load Tests
```bash
# Run load tests specifically
dotnet test tests/CleanArchitecture.Performance.Tests/LoadTests/
```

## Test Results

### Benchmark Results
- Benchmark results are saved to `BenchmarkDotNet.Artifacts/` directory
- Results include detailed performance metrics, memory allocation data, and statistical analysis

### Load Test Results
- Load test results are saved to `load-test-results/` directory
- Results include HTML reports with charts and CSV data for analysis

## Configuration

### Test Configuration
Performance tests use the same configuration as integration tests:
- In-memory database for isolated testing
- Test containers for Redis and RabbitMQ
- Configurable performance thresholds

### Environment Variables
```bash
# Optional: Override default performance thresholds
PERF_API_RESPONSE_THRESHOLD_MS=500
PERF_DB_QUERY_THRESHOLD_MS=1000
PERF_CACHE_GET_THRESHOLD_MS=50
```

## Continuous Integration

### Performance Regression Detection
- Performance tests should be run in CI/CD pipeline
- Benchmark results can be compared against baseline to detect regressions
- Load test results validate system can handle expected traffic

### Performance Monitoring
- Integrate with monitoring tools to track performance trends
- Set up alerts for performance threshold violations
- Regular performance testing schedule recommended

## Troubleshooting

### Common Issues

1. **High Response Times**
   - Check database query execution plans
   - Verify cache hit ratios
   - Monitor system resources during tests

2. **Load Test Failures**
   - Ensure test environment has sufficient resources
   - Check for connection pool exhaustion
   - Verify test data cleanup between runs

3. **Benchmark Inconsistencies**
   - Run benchmarks multiple times for statistical significance
   - Ensure system is not under load during benchmarking
   - Use Release configuration for accurate results

### Performance Optimization Tips

1. **Database Optimization**
   - Add appropriate indexes for frequently queried columns
   - Use pagination for large result sets
   - Consider read replicas for read-heavy workloads

2. **Cache Optimization**
   - Implement cache warming strategies
   - Use appropriate cache expiration times
   - Monitor cache memory usage

3. **API Optimization**
   - Implement response compression
   - Use async/await properly
   - Consider API response caching

## Contributing

When adding new performance tests:
1. Follow the existing test structure and naming conventions
2. Include appropriate performance thresholds
3. Add documentation for new test scenarios
4. Update this README with new test information