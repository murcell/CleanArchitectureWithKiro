# Docker Setup for Clean Architecture Project

This directory contains Docker configuration files for running the Clean Architecture project in different environments.

## Files Overview

- `Dockerfile` - Multi-stage optimized Dockerfile for the API application
- `docker-compose.yml` - Base compose file with core services
- `docker-compose.prod.yml` - Production-optimized compose configuration
- `docker-compose.test.yml` - Test environment configuration
- `docker-compose.override.yml` - Development environment overrides
- `healthcheck.sh` - Comprehensive health check script
- `redis.conf` - Redis production configuration
- `rabbitmq.conf` - RabbitMQ production configuration
- `nginx.conf` - Nginx reverse proxy configuration
- `setup.ps1` - PowerShell script for managing Docker environments

## Quick Start

### Development Environment

```powershell
# Start development environment
.\docker\setup.ps1 -Environment dev -Action up -Detached

# View logs
.\docker\setup.ps1 -Environment dev -Action logs

# Check health status
.\docker\setup.ps1 -Environment dev -Action health

# Stop development environment
.\docker\setup.ps1 -Environment dev -Action down
```

### Production Environment

```powershell
# Build and start production environment
.\docker\setup.ps1 -Environment prod -Action up -Build -Detached

# Check health status
.\docker\setup.ps1 -Environment prod -Action health

# View logs
.\docker\setup.ps1 -Environment prod -Action logs

# Stop production environment
.\docker\setup.ps1 -Environment prod -Action down
```

### Test Environment

```powershell
# Start test environment
.\docker\setup.ps1 -Environment test -Action up -Detached

# Run tests (after containers are up)
dotnet test

# Stop test environment
.\docker\setup.ps1 -Environment test -Action down
```

## Manual Docker Commands

### Development

```bash
# Start development environment
docker compose -f docker/docker-compose.yml -f docker/docker-compose.override.yml up -d

# View logs
docker compose -f docker/docker-compose.yml -f docker/docker-compose.override.yml logs -f

# Stop development environment
docker compose -f docker/docker-compose.yml -f docker/docker-compose.override.yml down --volumes
```

### Production

```bash
# Start production environment
docker compose -f docker/docker-compose.prod.yml up -d

# View logs
docker compose -f docker/docker-compose.prod.yml logs -f

# Stop production environment
docker compose -f docker/docker-compose.prod.yml down --volumes
```

### Test

```bash
# Start test environment
docker compose -f docker/docker-compose.yml -f docker/docker-compose.test.yml up -d

# Stop test environment
docker compose -f docker/docker-compose.yml -f docker/docker-compose.test.yml down --volumes
```

## Environment Configurations

### Development
- Uses Developer edition of SQL Server
- Enables hot reload for the API
- Reduced resource limits
- Debug logging enabled
- Shorter cache expiration times

### Test
- Uses in-memory/temporary storage where possible
- Optimized for fast startup and teardown
- Separate database and message queue instances
- Health checks with shorter intervals

### Production
- Uses Express edition of SQL Server (change to Standard/Enterprise as needed)
- Includes Nginx reverse proxy
- Resource limits and reservations
- Comprehensive health checks
- Security hardening
- Persistent volumes for data

## Health Checks

All services include comprehensive health checks:

### Application Health Checks
- **Liveness**: `/api/health/live` - Basic application responsiveness
- **Readiness**: `/api/health/ready` - Application ready to serve traffic
- **Detailed**: `/api/health` - Comprehensive health status including dependencies

### Infrastructure Health Checks
- **SQL Server**: Connection and query execution test
- **Redis**: Ping command test
- **RabbitMQ**: Management API ping test

### Container Health Checks
- Custom health check script with retry logic
- Configurable timeouts and intervals
- Proper exit codes for container orchestration

## Security Features

### Application Security
- Non-root user execution
- Minimal base images
- Security headers via Nginx
- Rate limiting
- Input validation

### Network Security
- Custom bridge network
- Service isolation
- Configurable firewall rules
- SSL/TLS support (configure certificates)

### Data Security
- Encrypted connections
- Secure credential management
- Volume encryption support
- Audit logging

## Performance Optimizations

### Docker Image Optimizations
- Multi-stage builds
- Layer caching optimization
- Minimal runtime dependencies
- Optimized .NET runtime settings

### Resource Management
- Memory limits and reservations
- CPU limits and reservations
- Disk I/O optimization
- Network performance tuning

### Caching Strategies
- Docker layer caching
- Application-level caching with Redis
- Static file caching via Nginx
- Database query optimization

## Monitoring and Logging

### Application Logging
- Structured logging with Serilog
- Log aggregation to volumes
- Correlation ID tracking
- Performance metrics logging

### Container Monitoring
- Health check status monitoring
- Resource usage tracking
- Container lifecycle events
- Service dependency monitoring

### Infrastructure Monitoring
- Database performance metrics
- Cache hit/miss ratios
- Message queue throughput
- Network latency monitoring

## Troubleshooting

### Common Issues

1. **Port Conflicts**
   ```powershell
   # Check port usage
   netstat -an | findstr :8080
   
   # Use different ports in compose files
   ```

2. **Memory Issues**
   ```powershell
   # Check Docker memory usage
   docker stats
   
   # Adjust memory limits in compose files
   ```

3. **Health Check Failures**
   ```powershell
   # Check health check logs
   docker logs cleanarch-api
   
   # Test health endpoints manually
   curl http://localhost:8080/api/health/live
   ```

4. **Database Connection Issues**
   ```powershell
   # Check SQL Server logs
   docker logs cleanarch-sqlserver
   
   # Test connection
   docker exec -it cleanarch-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P YourStrong@Passw0rd
   ```

### Debugging Commands

```powershell
# View container logs
docker logs <container-name> --follow

# Execute commands in container
docker exec -it <container-name> /bin/bash

# Inspect container configuration
docker inspect <container-name>

# Check network connectivity
docker network ls
docker network inspect cleanarch-network

# Monitor resource usage
docker stats

# Clean up resources
docker system prune -a
docker volume prune
```

## Configuration Management

### Environment Variables
- Use `.env` files for environment-specific configurations
- Override settings via compose files
- Secure credential management with Docker secrets

### Volume Management
- Persistent volumes for production data
- Temporary volumes for test environments
- Backup and restore procedures

### Network Configuration
- Custom bridge networks for service isolation
- Port mapping for external access
- Load balancer integration

## Deployment Strategies

### Development Deployment
- Hot reload enabled
- Debug symbols included
- Development tools available

### Staging Deployment
- Production-like configuration
- Performance testing enabled
- Monitoring and alerting

### Production Deployment
- High availability configuration
- Load balancing and scaling
- Backup and disaster recovery
- Security hardening

## Maintenance

### Regular Tasks
- Update base images regularly
- Monitor security vulnerabilities
- Backup persistent data
- Clean up unused resources

### Scaling Considerations
- Horizontal scaling with multiple instances
- Load balancer configuration
- Database connection pooling
- Cache distribution strategies

## Support

For issues and questions:
1. Check the troubleshooting section above
2. Review Docker and application logs
3. Verify configuration files
4. Test individual components
5. Consult the main project documentation