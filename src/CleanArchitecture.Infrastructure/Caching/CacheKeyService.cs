using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using System.Text;

namespace CleanArchitecture.Infrastructure.Caching;

public class CacheKeyService : ICacheKeyService
{
    private readonly CacheOptions _options;
    private const string Separator = ":";

    public CacheKeyService(IOptions<CacheOptions> options)
    {
        _options = options.Value;
    }

    public string GenerateKey(string entityName, object id)
    {
        return BuildKey(_options.KeyPrefix, entityName, id.ToString());
    }

    public string GenerateListKey(string entityName, params object[] parameters)
    {
        var keyParts = new List<string> { _options.KeyPrefix, entityName, "list" };
        
        if (parameters.Length > 0)
        {
            keyParts.AddRange(parameters.Select(p => p.ToString() ?? string.Empty));
        }

        return BuildKey(keyParts.ToArray());
    }

    public string GenerateUserKey(int userId, string dataType, params object[] parameters)
    {
        var keyParts = new List<string> { _options.KeyPrefix, "user", userId.ToString(), dataType };
        
        if (parameters.Length > 0)
        {
            keyParts.AddRange(parameters.Select(p => p.ToString() ?? string.Empty));
        }

        return BuildKey(keyParts.ToArray());
    }

    public IEnumerable<string> GetEntityKeys(string entityName, object id)
    {
        var keys = new List<string>
        {
            GenerateKey(entityName, id),
            GenerateListKey(entityName),
            BuildKey(_options.KeyPrefix, entityName, "count"),
            BuildKey(_options.KeyPrefix, entityName, "search", "*")
        };

        return keys;
    }

    public string GetEntityPattern(string entityName)
    {
        return BuildKey(_options.KeyPrefix, entityName, "*");
    }

    private static string BuildKey(params string?[] parts)
    {
        var validParts = parts.Where(p => !string.IsNullOrWhiteSpace(p));
        return string.Join(Separator, validParts);
    }
}