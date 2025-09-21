using CleanArchitecture.Application;
using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Infrastructure;
using CleanArchitecture.WebAPI.Configuration;
using CleanArchitecture.WebAPI.Middleware;
using CleanArchitecture.WebAPI.Services;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Clean Architecture API", 
        Version = "v1",
        Description = "A comprehensive Clean Architecture implementation with .NET 9"
    });
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Add Application layer services (includes MediatR)
builder.Services.AddApplication(builder.Configuration);

// Add Infrastructure layer services (includes EF Core, Redis, RabbitMQ)
builder.Services.AddInfrastructure(builder.Configuration);

// Add rate limiting configuration
builder.Services.Configure<RateLimitOptions>(builder.Configuration.GetSection("RateLimit"));

// Add HTTP context accessor
builder.Services.AddHttpContextAccessor();

// Add authorization policies
builder.Services.AddAuthorizationPolicies();

// Add correlation ID service
builder.Services.AddScoped<ICorrelationIdService, CorrelationIdService>();

// Add current user service
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Simple configuration without API versioning for now

var app = builder.Build();

// Enable CORS (must be early in pipeline)
app.UseCors();

// Add global exception handler (should be early)
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// Add correlation ID tracking
app.UseMiddleware<CorrelationIdMiddleware>();

// Add security headers
app.UseMiddleware<SecurityHeadersMiddleware>();

// Add rate limiting
app.UseMiddleware<RateLimitingMiddleware>();

// Add JWT validation
app.UseMiddleware<JwtValidationMiddleware>();

// Add performance monitoring
app.UseMiddleware<PerformanceMonitoringMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Clean Architecture API v1");
        options.RoutePrefix = string.Empty; // Swagger UI at root
        options.DefaultModelsExpandDepth(-1);
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
        options.EnableDeepLinking();
        options.DisplayOperationId();
        options.DisplayRequestDuration();
    });
}

// Only use HTTPS redirection in production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthorization();

app.MapControllers();
app.MapGet("/", () => "Clean Architecture API is running!");
app.MapGet("/health", () => "Healthy");

app.Run();

public partial class Program { }