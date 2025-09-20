using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net;
using Asp.Versioning;

namespace CleanArchitecture.WebAPI.Controllers;

/// <summary>
/// Health check controller for monitoring application health
/// </summary>
[ApiController]
[Route("api/[controller]")]
[ApiVersion("1.0")]
[ApiVersion("2.0")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(HealthCheckService healthCheckService, ILogger<HealthController> logger)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the overall health status of the application
    /// </summary>
    /// <returns>Health status information</returns>
    [HttpGet]
    [ProducesResponseType(typeof(HealthCheckResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(HealthCheckResponse), (int)HttpStatusCode.ServiceUnavailable)]
    public async Task<IActionResult> GetHealth()
    {
        try
        {
            var healthReport = await _healthCheckService.CheckHealthAsync();
            var response = CreateHealthResponse(healthReport);

            var statusCode = healthReport.Status == HealthStatus.Healthy 
                ? HttpStatusCode.OK 
                : HttpStatusCode.ServiceUnavailable;

            return StatusCode((int)statusCode, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed with exception");
            
            var errorResponse = new HealthCheckResponse
            {
                Status = "Unhealthy",
                TotalDuration = TimeSpan.Zero,
                Entries = new Dictionary<string, HealthCheckEntry>
                {
                    ["error"] = new HealthCheckEntry
                    {
                        Status = "Unhealthy",
                        Description = "Health check service failed",
                        Exception = ex.Message
                    }
                }
            };

            return StatusCode((int)HttpStatusCode.ServiceUnavailable, errorResponse);
        }
    }

    /// <summary>
    /// Gets the health status of specific components
    /// </summary>
    /// <param name="tags">Comma-separated list of tags to filter health checks</param>
    /// <returns>Filtered health status information</returns>
    [HttpGet("detailed")]
    [ProducesResponseType(typeof(HealthCheckResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(HealthCheckResponse), (int)HttpStatusCode.ServiceUnavailable)]
    public async Task<IActionResult> GetDetailedHealth([FromQuery] string? tags = null)
    {
        try
        {
            var tagList = string.IsNullOrEmpty(tags) 
                ? null 
                : tags.Split(',', StringSplitOptions.RemoveEmptyEntries);

            var healthReport = await _healthCheckService.CheckHealthAsync(check =>
                tagList == null || tagList.Any(tag => check.Tags.Contains(tag)));

            var response = CreateDetailedHealthResponse(healthReport);

            var statusCode = healthReport.Status == HealthStatus.Healthy 
                ? HttpStatusCode.OK 
                : HttpStatusCode.ServiceUnavailable;

            return StatusCode((int)statusCode, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Detailed health check failed with exception");
            return StatusCode((int)HttpStatusCode.ServiceUnavailable, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Simple health check endpoint for load balancers
    /// </summary>
    /// <returns>Simple OK response if healthy</returns>
    [HttpGet("ready")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.ServiceUnavailable)]
    public async Task<IActionResult> GetReadiness()
    {
        try
        {
            var healthReport = await _healthCheckService.CheckHealthAsync();
            
            return healthReport.Status == HealthStatus.Healthy 
                ? Ok("Ready") 
                : StatusCode((int)HttpStatusCode.ServiceUnavailable, "Not Ready");
        }
        catch
        {
            return StatusCode((int)HttpStatusCode.ServiceUnavailable, "Not Ready");
        }
    }

    /// <summary>
    /// Liveness probe endpoint
    /// </summary>
    /// <returns>Simple OK response if application is alive</returns>
    [HttpGet("live")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    public IActionResult GetLiveness()
    {
        return Ok("Alive");
    }

    private static HealthCheckResponse CreateHealthResponse(HealthReport healthReport)
    {
        return new HealthCheckResponse
        {
            Status = healthReport.Status.ToString(),
            TotalDuration = healthReport.TotalDuration,
            Entries = healthReport.Entries.ToDictionary(
                kvp => kvp.Key,
                kvp => new HealthCheckEntry
                {
                    Status = kvp.Value.Status.ToString(),
                    Description = kvp.Value.Description,
                    Duration = kvp.Value.Duration
                })
        };
    }

    private static HealthCheckResponse CreateDetailedHealthResponse(HealthReport healthReport)
    {
        return new HealthCheckResponse
        {
            Status = healthReport.Status.ToString(),
            TotalDuration = healthReport.TotalDuration,
            Entries = healthReport.Entries.ToDictionary(
                kvp => kvp.Key,
                kvp => new HealthCheckEntry
                {
                    Status = kvp.Value.Status.ToString(),
                    Description = kvp.Value.Description,
                    Duration = kvp.Value.Duration,
                    Data = kvp.Value.Data?.ToDictionary(d => d.Key, d => d.Value),
                    Exception = kvp.Value.Exception?.Message,
                    Tags = kvp.Value.Tags.ToList()
                })
        };
    }
}

/// <summary>
/// Health check response model
/// </summary>
public class HealthCheckResponse
{
    public string Status { get; set; } = string.Empty;
    public TimeSpan TotalDuration { get; set; }
    public Dictionary<string, HealthCheckEntry> Entries { get; set; } = new();
}

/// <summary>
/// Individual health check entry model
/// </summary>
public class HealthCheckEntry
{
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TimeSpan Duration { get; set; }
    public Dictionary<string, object>? Data { get; set; }
    public string? Exception { get; set; }
    public List<string>? Tags { get; set; }
}