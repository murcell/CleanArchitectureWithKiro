# Deployment Guide

This directory contains all the necessary files and scripts for deploying the Clean Architecture .NET application to different environments.

## Overview

The deployment setup supports multiple environments:
- **Staging**: For testing and validation before production
- **Production**: Live environment with production-grade configurations

## Prerequisites

- Docker and Docker Compose installed
- PowerShell (for Windows) or Bash (for Linux/macOS)
- Access to container registry (if using remote images)
- SSL certificates (for production HTTPS)

## Quick Start

### 1. Environment Configuration

Copy and customize the environment files:

```bash
# For staging
cp deployment/.env.staging deployment/.env.staging.local
# Edit the file with your actual values

# For production  
cp deployment/.env.production deployment/.env.production.local
# Edit the file with your actual values
```

**Important**: Never commit files with real passwords to version control!

### 2. Deploy to Staging

```powershell
# Windows PowerShell
.\deployment\deploy.ps1 -Environment staging -Tag latest

# Linux/macOS
./deployment/deploy.sh staging latest
```

### 3. Deploy to Production

```powershell
# Windows PowerShell
.\deployment\deploy.ps1 -Environment production -Tag v1.0.0

# Linux/macOS
./deployment/deploy.sh production v1.0.0
```

## File Structure

```
deployment/
├── README.md                           # This file
├── docker-compose.production.yml       # Production Docker Compose
├── docker-compose.staging.yml          # Staging Docker Compose (if different)
├── .env.staging                        # Staging environment template
├── .env.production                     # Production environment template
├── deploy.ps1                          # PowerShell deployment script
├── deploy.sh                           # Bash deployment script
├── nginx/
│   ├── nginx.conf                      # Nginx configuration
│   └── ssl/                            # SSL certificates directory
├── backups/                            # Deployment backups
└── current/                            # Current deployment info
```

## Environment Variables

### Required Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `SQL_PASSWORD` | SQL Server SA password | `YourStrongPassword123!` |
| `REDIS_PASSWORD` | Redis password | `RedisPassword123!` |
| `RABBITMQ_USER` | RabbitMQ username | `admin` |
| `RABBITMQ_PASSWORD` | RabbitMQ password | `RabbitPassword123!` |
| `JWT_SECRET` | JWT signing secret | `YourJwtSecret32Characters!` |

### Optional Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `TAG` | Docker image tag | `latest` |
| `LOG_LEVEL` | Logging level | `Information` |
| `API_VERSION` | API version | `v1` |
| `CORS_ORIGINS` | Allowed CORS origins | `*` |

## SSL Configuration (Production)

For production deployment with HTTPS:

1. Place your SSL certificates in `deployment/nginx/ssl/`:
   - `cert.pem` - SSL certificate
   - `key.pem` - Private key

2. Update the nginx configuration if needed

3. Ensure the environment variables point to the correct certificate paths

## Monitoring and Health Checks

The deployment includes several monitoring endpoints:

- **Application Health**: `http://localhost/health`
- **RabbitMQ Management**: `http://localhost:15672`
- **API Documentation**: `http://localhost/swagger` (non-production only)

## Backup and Recovery

### Automatic Backups

The deployment script automatically creates backups before each deployment in `deployment/backups/`.

### Manual Backup

```bash
# Backup current deployment
docker-compose -f deployment/docker-compose.production.yml exec sqlserver \
  /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P $SQL_PASSWORD \
  -Q "BACKUP DATABASE CleanArchitectureDB TO DISK = '/var/opt/mssql/backup/backup.bak'"
```

### Restore from Backup

```bash
# Stop services
docker-compose -f deployment/docker-compose.production.yml down

# Restore from backup directory
cp -r deployment/backups/YYYYMMDD-HHMMSS/* deployment/current/

# Restart services
docker-compose -f deployment/docker-compose.production.yml up -d
```

## Troubleshooting

### Common Issues

1. **Services not starting**
   - Check Docker logs: `docker-compose logs [service-name]`
   - Verify environment variables are set correctly
   - Ensure required ports are not in use

2. **Database connection issues**
   - Verify SQL Server is healthy: `docker-compose ps`
   - Check connection string format
   - Ensure password meets SQL Server requirements

3. **SSL certificate issues**
   - Verify certificate files exist and have correct permissions
   - Check certificate validity: `openssl x509 -in cert.pem -text -noout`
   - Ensure private key matches certificate

### Logs

View application logs:
```bash
# All services
docker-compose -f deployment/docker-compose.production.yml logs

# Specific service
docker-compose -f deployment/docker-compose.production.yml logs cleanarchitecture-api

# Follow logs
docker-compose -f deployment/docker-compose.production.yml logs -f
```

### Performance Monitoring

Monitor resource usage:
```bash
# Container stats
docker stats

# System resources
docker system df
docker system events
```

## Security Considerations

1. **Passwords**: Use strong, unique passwords for all services
2. **SSL/TLS**: Always use HTTPS in production
3. **Network**: Use Docker networks to isolate services
4. **Updates**: Regularly update base images and dependencies
5. **Secrets**: Never commit secrets to version control
6. **Firewall**: Configure firewall rules appropriately

## Scaling

For horizontal scaling:

1. **Load Balancer**: Add multiple API instances behind nginx
2. **Database**: Consider read replicas for read-heavy workloads
3. **Cache**: Redis cluster for high availability
4. **Message Queue**: RabbitMQ cluster for reliability

Example scaling configuration:
```yaml
cleanarchitecture-api:
  deploy:
    replicas: 3
    resources:
      limits:
        cpus: '1'
        memory: 1G
```

## CI/CD Integration

This deployment setup integrates with the CI/CD pipeline:

1. **Build**: Images are built and tagged automatically
2. **Test**: Deployment is tested in staging first
3. **Deploy**: Production deployment triggered on releases
4. **Rollback**: Previous versions can be restored from backups

## Support

For deployment issues:
1. Check the logs first
2. Verify environment configuration
3. Consult the troubleshooting section
4. Review Docker Compose and nginx configurations