using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TicketFlow.CourseUtils;

namespace TicketFlow.Shared.Caching;

internal sealed class RedisCacheService : ICacheService, IDisposable
{
    private readonly IConnectionMultiplexer _connection;
    private readonly IDatabase _database;
    private readonly string _prefix;
    private readonly bool _usePrefixes;
    private readonly TimeSpan _defaultExpiration;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(
        IConnectionMultiplexer connection,
        IOptions<CacheOptions> options,
        ILogger<RedisCacheService> logger)
    {
        _connection = connection;
        _database = connection.GetDatabase();
        _logger = logger;
        _usePrefixes = FeatureFlags.UseRedisKeyPrefixes;

        var cacheOptions = options.Value;

        if (_usePrefixes && string.IsNullOrWhiteSpace(cacheOptions.InstanceName))
        {
            throw new InvalidOperationException(
                "CacheOptions.InstanceName is required. Each service must have a unique prefix " +
                "to prevent key collisions when sharing a Redis instance.");
        }

        _prefix = cacheOptions.InstanceName;
        _defaultExpiration = TimeSpan.FromMinutes(cacheOptions.DefaultExpirationMinutes);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        if (_usePrefixes)
        {
            _logger.LogInformation("Redis cache initialized with prefix: {Prefix}", _prefix);
        }
        else
        {
            _logger.LogWarning(
                "Redis cache initialized WITHOUT prefixes (FeatureFlags.UseRedisKeyPrefixes=false). " +
                "Risk of key collision between services!");
        }
    }

    private string GetFullKey(string key) => _usePrefixes ? $"{_prefix}:{key}" : key;

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        var fullKey = GetFullKey(key);

        try
        {
            var value = await _database.StringGetAsync(fullKey);

            if (value.IsNullOrEmpty)
            {
                _logger.LogDebug("Cache miss for key: {Key}", fullKey);
                return null;
            }

            _logger.LogDebug("Cache hit for key: {Key}", fullKey);
            return JsonSerializer.Deserialize<T>((string)value!, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting cache key: {Key}", fullKey);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default) where T : class
    {
        var fullKey = GetFullKey(key);
        var actualExpiry = expiry ?? _defaultExpiration;

        try
        {
            var serialized = JsonSerializer.Serialize(value, _jsonOptions);
            await _database.StringSetAsync(fullKey, serialized, actualExpiry);
            _logger.LogDebug("Cache set for key: {Key}, expiry: {Expiry}", fullKey, actualExpiry);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error setting cache key: {Key}", fullKey);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        var fullKey = GetFullKey(key);

        try
        {
            await _database.KeyDeleteAsync(fullKey);
            _logger.LogDebug("Cache removed for key: {Key}", fullKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error removing cache key: {Key}", fullKey);
        }
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiry = null,
        CancellationToken ct = default) where T : class
    {
        var cached = await GetAsync<T>(key, ct);
        if (cached is not null)
        {
            return cached;
        }

        var value = await factory(ct);
        await SetAsync(key, value, expiry, ct);
        return value;
    }

    public void Dispose()
    {
    }
}
