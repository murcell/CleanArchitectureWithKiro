using CleanArchitecture.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Infrastructure.Security;

/// <summary>
/// Service for managing blacklisted tokens using Redis cache
/// </summary>
public class TokenBlacklistService : ITokenBlacklistService
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<TokenBlacklistService> _logger;
    private const string BlacklistPrefix = "blacklisted_token:";

    public TokenBlacklistService(ICacheService cacheService, ILogger<TokenBlacklistService> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task BlacklistTokenAsync(string token, DateTime expiry, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Token cannot be null or empty", nameof(token));
        }

        var key = $"{BlacklistPrefix}{GetTokenHash(token)}";
        var ttl = expiry - DateTime.UtcNow;
        
        if (ttl > TimeSpan.Zero)
        {
            await _cacheService.SetAsync(key, "blacklisted", ttl);
            _logger.LogInformation("Token blacklisted until {Expiry}", expiry);
        }
        else
        {
            _logger.LogWarning("Attempted to blacklist already expired token");
        }
    }

    public async Task<bool> IsTokenBlacklistedAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var key = $"{BlacklistPrefix}{GetTokenHash(token)}";
        var result = await _cacheService.GetAsync<string>(key);
        
        var isBlacklisted = !string.IsNullOrEmpty(result);
        
        if (isBlacklisted)
        {
            _logger.LogWarning("Blacklisted token access attempt detected");
        }
        
        return isBlacklisted;
    }

    public async Task CleanupExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        // Redis automatically handles TTL expiration, so this is mainly for logging
        _logger.LogInformation("Token blacklist cleanup completed (handled by Redis TTL)");
        await Task.CompletedTask;
    }

    private string GetTokenHash(string token)
    {
        // Use a simple hash for the cache key to avoid storing full tokens
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashBytes)[..16]; // Use first 16 chars
    }
}