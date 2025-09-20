using CleanArchitecture.Application;
using CleanArchitecture.Infrastructure;
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

// Simple configuration without API versioning for now

var app = builder.Build();

// Enable CORS (must be early in pipeline)
app.UseCors();

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