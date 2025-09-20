# Clean Architecture .NET 9 Project

[![CI/CD Pipeline](https://github.com/username/clean-architecture-dotnet/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/username/clean-architecture-dotnet/actions/workflows/ci-cd.yml)
[![codecov](https://codecov.io/gh/username/clean-architecture-dotnet/branch/main/graph/badge.svg)](https://codecov.io/gh/username/clean-architecture-dotnet)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/9.0)

Modern, ölçeklenebilir ve sürdürülebilir bir .NET 9 web API projesi. Clean Architecture prensiplerini takip eder ve enterprise-grade özellikler sunar.

## ✨ Özellikler

- 🏗️ **Clean Architecture** - Katmanlı mimari ve bağımlılık tersine çevirme
- 🔐 **Güvenlik** - JWT Authentication, API Key, XSS koruması, SQL Injection koruması
- 📊 **Monitoring** - Structured logging (Serilog), Health checks, Performance monitoring
- 🚀 **Performance** - Redis caching, Connection pooling, Async/await patterns
- 🔄 **Messaging** - RabbitMQ ile asenkron mesajlaşma
- 🧪 **Testing** - Unit, Integration, End-to-End ve Performance testleri
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

## 📁 Proje Yapısı

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

## 🚀 Hızlı Başlangıç

### Gereksinimler

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Git](https://git-scm.com/)

### Kurulum

1. **Repository'yi klonlayın**
   ```bash
   git clone https://github.com/username/clean-architecture-dotnet.git
   cd clean-architecture-dotnet
   ```

2. **Docker servislerini başlatın**
   ```bash
   cd docker
   docker-compose up -d
   ```

3. **Bağımlılıkları yükleyin**
   ```bash
   dotnet restore
   ```

4. **Projeyi derleyin**
   ```bash
   dotnet build
   ```

5. **Veritabanını oluşturun**
   ```bash
   dotnet ef database update --project src/CleanArchitecture.Infrastructure --startup-project src/CleanArchitecture.WebAPI
   ```

6. **Testleri çalıştırın**
   ```bash
   dotnet test
   ```

7. **API'yi başlatın**
   ```bash
   dotnet run --project src/CleanArchitecture.WebAPI
   ```

8. **API'yi test edin**
   - Swagger UI: https://localhost:7001/swagger
   - Health Check: https://localhost:7001/health

## 🐳 Docker Servisleri

| Servis | Port | Credentials | Management UI |
|--------|------|-------------|---------------|
| **SQL Server** | 1433 | sa/YourStrong@Passw0rd | - |
| **Redis** | 6379 | - | - |
| **RabbitMQ** | 5672 | admin/admin123 | http://localhost:15672 |
| **API** | 7001 (HTTPS), 5001 (HTTP) | - | https://localhost:7001/swagger |

## 🧪 Testing

### Test Türleri

- **Unit Tests** - İzole birim testleri
- **Integration Tests** - API endpoint testleri
- **End-to-End Tests** - Tam workflow testleri
- **Performance Tests** - Yük ve performans testleri

### Test Komutları

```bash
# Tüm testleri çalıştır
dotnet test

# Sadece unit testleri
dotnet test --filter Category=Unit

# Sadece integration testleri
dotnet test --filter Category=Integration

# Code coverage ile
dotnet test --collect:"XPlat Code Coverage"

# Performance testleri
dotnet test tests/CleanArchitecture.Performance.Tests
```

## 🔐 Güvenlik Özellikleri

- **Authentication**: JWT Bearer token ve API Key desteği
- **Authorization**: Role-based ve policy-based yetkilendirme
- **Input Validation**: FluentValidation ile kapsamlı doğrulama
- **XSS Protection**: Cross-site scripting koruması
- **SQL Injection Protection**: Parameterized queries
- **CORS**: Cross-origin resource sharing yapılandırması
- **Rate Limiting**: API rate limiting
- **Security Headers**: Güvenlik başlıkları

## 📊 Monitoring & Logging

### Structured Logging
- **Serilog** ile yapılandırılmış loglama
- **Console**, **File**, ve **Seq** sink'leri
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

GitHub Actions workflow otomatik olarak:
- ✅ Build ve test işlemlerini çalıştırır
- 🔍 Security scan yapar
- 🐳 Docker image oluşturur
- 🚀 Staging ve Production'a deploy eder
- 📊 Performance testleri çalıştırır

## 📚 API Documentation

API dokümantasyonu Swagger/OpenAPI ile otomatik oluşturulur:
- **Development**: https://localhost:7001/swagger
- **Production**: https://yourdomain.com/swagger

### API Versioning
API versioning desteklenir:
- Header: `X-Version: 1.0`
- Query: `?version=1.0`
- URL: `/api/v1/users`

## 🤝 Katkıda Bulunma

1. Fork edin
2. Feature branch oluşturun (`git checkout -b feature/amazing-feature`)
3. Değişikliklerinizi commit edin (`git commit -m 'Add amazing feature'`)
4. Branch'inizi push edin (`git push origin feature/amazing-feature`)
5. Pull Request oluşturun

## 📄 Lisans

Bu proje MIT lisansı altında lisanslanmıştır. Detaylar için [LICENSE](LICENSE) dosyasına bakın.

## 🙏 Teşekkürler

- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html) - Robert C. Martin
- [.NET Community](https://dotnet.microsoft.com/platform/community)
- Tüm açık kaynak katkıda bulunanlar

## 📞 İletişim

- **Proje Sahibi**: [Your Name](mailto:your.email@example.com)
- **GitHub**: [@username](https://github.com/username)
- **LinkedIn**: [Your LinkedIn](https://linkedin.com/in/username)

---

⭐ Bu projeyi beğendiyseniz yıldız vermeyi unutmayın!