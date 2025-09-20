# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file first for better layer caching
COPY CleanArchitecture.sln ./

# Copy project files for dependency restoration
COPY src/CleanArchitecture.Domain/CleanArchitecture.Domain.csproj ./src/CleanArchitecture.Domain/
COPY src/CleanArchitecture.Application/CleanArchitecture.Application.csproj ./src/CleanArchitecture.Application/
COPY src/CleanArchitecture.Infrastructure/CleanArchitecture.Infrastructure.csproj ./src/CleanArchitecture.Infrastructure/
COPY src/CleanArchitecture.WebAPI/CleanArchitecture.WebAPI.csproj ./src/CleanArchitecture.WebAPI/

# Restore dependencies (cached layer if project files haven't changed)
RUN dotnet restore CleanArchitecture.sln

# Copy source code
COPY src/ ./src/

# Build the application with optimizations
RUN dotnet build CleanArchitecture.sln -c Release --no-restore --verbosity minimal

# Publish the application with optimizations
RUN dotnet publish src/CleanArchitecture.WebAPI/CleanArchitecture.WebAPI.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    --no-build \
    --verbosity minimal \
    /p:PublishTrimmed=false \
    /p:PublishSingleFile=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Set working directory
WORKDIR /app

# Copy health check script
COPY docker/healthcheck.sh /usr/local/bin/healthcheck.sh
RUN chmod +x /usr/local/bin/healthcheck.sh

# Copy published application with proper ownership
COPY --from=build --chown=appuser:appuser /app/publish .

# Create logs directory
RUN mkdir -p /app/logs && chown -R appuser:appuser /app/logs

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Add comprehensive health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD /usr/local/bin/healthcheck.sh

# Set environment variables for production
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Set entry point with proper signal handling
ENTRYPOINT ["dotnet", "CleanArchitecture.WebAPI.dll"]