# Clean Architecture .NET 9 Project

[![CI/CD Pipeline](https://github.com/username/clean-architecture-dotnet/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/username/clean-architecture-dotnet/actions/workflows/ci-cd.yml)
[![codecov](https://codecov.io/gh/username/clean-architecture-dotnet/branch/main/graph/badge.svg)](https://codecov.io/gh/username/clean-architecture-dotnet)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/9.0)

A modern, scalable, and maintainable .NET 9 Web API project following Clean Architecture principles with enterprise-grade features.

## ✨ Features

- 🏗️ **Clean Architecture** - Layered architecture with dependency inversion
- 🔐 **Security** - JWT Authentication, API Key, XSS protection, SQL Injection protection
- 📊 **Monitoring** - Structured logging (Serilog), Health checks, Performance monitoring
- 🚀 **Performance** - Redis caching, Connection pooling, Async/await patterns
- 🔄 **Messaging** - Asynchronous messaging with RabbitMQ
- 🧪 **Testing** - Unit, Integration, End-to-End and Performance tests
- 🐳 **DevOps** - Docker containerization, CI/CD pipeline, Multi-environment deployment
- 📝 **Documentation** - Swagger/OpenAPI, Code documentation

## 🛠️ Tech Stack

### Core Framework
- **.NET 9** - Latest .NET framework
- **ASP.NET Core** - Web API framework
- **C# 13** - Programming language

### Architecture & Patterns
- **Clean Architecture** - Layered architecture
- **CQRS** - Command Query Responsibility Segregation (MediatR)
- **Repository Pattern** - Data access abstraction
- **Unit of Work** - Transaction management

### Database & Caching
- **Entity Framework Core 9** - ORM
- **SQL Server 2022** - Primary database
- **Redis 7** - Distributed caching and session storage

### Messaging & Communication
- **RabbitMQ** - Message broker for async communication
- **SignalR** - Real-time communication (if applicable)

### Security
- **JWT Bearer Authentication** - Token-based authentication
- **API Key Authentication** - Service-to-service authentication
- **BCrypt** - Password hashing
- **XSS Protection** - Cross-site scripting prevention
- **SQL Injection Protection** - Parameterized queries

### Validation & Mapping
- **FluentValidation** - Input validation
- **AutoMapper** - Object-to-object mapping

### Logging & Monitoring
- **Serilog** - Structured logging
- **Health Checks** - Application health monitoring
- **Performance Counters** - Performance metrics

### Testing
- **xUnit** - Unit testing framework
- **Moq** - Mocking framework
- **FluentAssertions** - Assertion library
- **TestContainers** - Integration testing with containers
- **NBomber** - Performance testing

### DevOps & Deployment
- **Docker** - Containerization
- **Docker Compose** - Multi-container orchestration
- **GitHub Actions** - CI/CD pipeline
- **Nginx** - Reverse proxy and load balancer

### API Documentation
- **Swagger/OpenAPI** - API documentation
- **API Versioning** - Version management

## 📁 Project Structure

```
CleanArchitecture.Solution/
├── 📁 src/
│   ├── 📁 CleanArchitecture.Domain/           # 🎯 Domain Layer
│   │   ├── Entities/                          # Domain entities
│   │   ├── ValueObjects/                      # Value objects
│   │   ├── Events/                            # Domain events
│   │   ├── Interfaces/                        # Repository interfaces
│   │   └── Exceptions/                        # Domain exceptions
│   │
│   ├── 📁 CleanArchitecture.Application/      # 🔧 Application Layer
│   │   ├── Features/                          # CQRS handlers
│   │   ├── Common/                            # DTOs, Mappings, Validators
│   │   ├── Interfaces/                        # Service interfaces
│   │   └── Behaviors/                         # Pipeline behaviors
│   │
│   ├── 📁 CleanArchitecture.Infrastructure/   # 🔌 Infrastructure Layer
│   │   ├── Data/                              # EF Core DbContext
│   │   ├── Repositories/                      # Repository implementations
│   │   ├── Services/                          # External services
│   │   ├── Caching/                           # Redis implementation
│   │   ├── Messaging/                         # RabbitMQ implementation
│   │   ├── Security/                          # Authentication & Authorization
│   │   └── HealthChecks/                      # Health check implementations
│   │
│   └── 📁 CleanArchitecture.WebAPI/          # 🌐 Presentation Layer
│       ├── Controllers/                       # API controllers
│       ├── Middleware/                        # Custom middleware
│       ├── Configuration/                     # Startup configuration
│       └── Logging/                           # Logging configuration
│
├── 📁 tests/
│   ├── 📁 CleanArchitecture.Domain.Tests/         # Unit tests
│   ├── 📁 CleanArchitecture.Application.Tests/    # Unit tests
│   ├── 📁 CleanArchitecture.Infrastructure.Tests/ # Unit tests
│   ├── 📁 CleanArchitecture.WebAPI.Tests/         # Integration tests
│   └── 📁 CleanArchitecture.Performance.Tests/    # Performance tests
│
├── 📁 docker/                                 # 🐳 Docker configuration
│   ├── docker-compose.yml                    # Development services
│   ├── docker-compose.prod.yml               # Production services
│   ├── Dockerfile                            # Application container
│   └── nginx.conf                             # Nginx configuration
│
├── 📁 deployment/                             # 🚀 Deployment scripts
│   ├── docker-compose.production.yml         # Production deployment
│   ├── deploy.ps1                            # Deployment script
│   └── nginx/                                 # Production nginx config
│
├── 📁 scripts/                               # 🔧 Build & utility scripts
│   ├── build.ps1                             # Build script
│   ├── run-tests.ps1                         # Test runner
│   └── docker-build.ps1                      # Docker build script
│
└── 📁 .github/workflows/                     # ⚙️ CI/CD pipelines
    └── ci-cd.yml                             # GitHub Actions workflow
```

## 🚀 Quick Start

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Git](https://git-scm.com/)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/username/clean-architecture-dotnet.git
   cd clean-architecture-dotnet
   ```

2. **Start Docker services**
   ```bash
   cd docker
   docker-compose up -d
   ```

3. **Restore dependencies**
   ```bash
   dotnet restore
   ```

4. **Build the project**
   ```bash
   dotnet build
   ```

5. **Create the database**
   ```bash
   dotnet ef database update --project src/CleanArchitecture.Infrastructure --startup-project src/CleanArchitecture.WebAPI
   ```

6. **Run tests**
   ```bash
   dotnet test
   ```

7. **Start the API**
   ```bash
   dotnet run --project src/CleanArchitecture.WebAPI
   ```

8. **Test the API**
   - Swagger UI: https://localhost:7196/swagger
   - Health Check: https://localhost:7196/health

## 🐳 Docker Servisleri

| Servis | Port | Credentials | Management UI |
|--------|------|-------------|---------------|
| **SQL Server** | 1433 | sa/YourStrong@Passw0rd | - |
| **Redis** | 6379 | - | - |
| **RabbitMQ** | 5672 | admin/admin123 | http://localhost:15672 |
| **API** | 7001 (HTTPS), 5001 (HTTP) | - | https://localhost:7001/swagger |

## 🧪 Testing

### Test Types

- **Unit Tests** - Isolated unit tests
- **Integration Tests** - API endpoint tests
- **End-to-End Tests** - Complete workflow tests
- **Performance Tests** - Load and performance tests

### Test Commands

```bash
# Run all tests
dotnet test

# Run only unit tests
dotnet test --filter Category=Unit

# Run only integration tests
dotnet test --filter Category=Integration

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"

# Run performance tests
dotnet test tests/CleanArchitecture.Performance.Tests
```

## 🔐 Security Features

- **Authentication**: JWT Bearer token and API Key support
- **Authorization**: Role-based and policy-based authorization
- **Input Validation**: Comprehensive validation with FluentValidation
- **XSS Protection**: Cross-site scripting protection
- **SQL Injection Protection**: Parameterized queries
- **CORS**: Cross-origin resource sharing configuration
- **Rate Limiting**: API rate limiting
- **Security Headers**: Security headers

## 📊 Monitoring & Logging

### Structured Logging
- **Serilog** structured logging
- **Console**, **File**, and **Seq** sinks
- **Correlation ID** tracking
- **Performance logging** middleware

### Health Checks
- Database connectivity
- Redis connectivity  
- RabbitMQ connectivity
- Custom application health checks

### Endpoints
- Health: `/health`
- Health UI: `/health-ui`
- Metrics: `/metrics`

## 🚀 Deployment

### Development
```bash
docker-compose -f docker/docker-compose.yml up -d
```

### Production
```bash
# Production deployment
./deployment/deploy.ps1 -Environment Production

# Or using Docker Compose
docker-compose -f deployment/docker-compose.production.yml up -d
```

### CI/CD Pipeline

GitHub Actions workflow automatically:
- ✅ Runs build and test processes
- 🔍 Performs security scans
- 🐳 Creates Docker images
- 🚀 Deploys to Staging and Production
- 📊 Runs performance tests

## 📚 API Documentation

API documentation is automatically generated with Swagger/OpenAPI:
- **Development**: https://localhost:7196/swagger
- **Production**: https://yourdomain.com/swagger

### API Versioning
API versioning is supported:
- Header: `X-Version: 1.0`
- Query: `?version=1.0`
- URL: `/api/v1/users`

## 🤝 Contributing

1. Fork the project
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html) - Robert C. Martin
- [.NET Community](https://dotnet.microsoft.com/platform/community)
- All open source contributors

## 📞 Contact

- **Project Owner**: [Your Name](mailto:your.email@example.com)
- **GitHub**: [@username](https://github.com/username)
- **LinkedIn**: [Your LinkedIn](https://linkedin.com/in/username)

---

⭐ If you like this project, don't forget to give it a star!