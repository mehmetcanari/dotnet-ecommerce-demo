using ECommerce.Application.Interfaces.Service;
using StackExchange.Redis;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Services.Cache;

public class RedisCacheService : ICacheService
{
    private readonly IDatabase _database;
    private readonly ILogger<RedisCacheService> _logger;
    
    public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
    {
        _database = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            if (await _database.KeyExistsAsync(key))
            {
                var value = await _database.StringGetAsync(key);
                if (value.IsNull) 
                {
                    _logger.LogWarning("Cache value for key: {Key} is null", key);
                    return default;
                }

                var result = JsonSerializer.Deserialize<T>(value!);
                return result;
            }
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while getting cache value for key: {Key}", key);
            throw;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        try
        {
            var serialized = JsonSerializer.Serialize(value);
            await _database.StringSetAsync(key, serialized, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while setting cache value for key: {Key}", key);
            throw;
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            if (await _database.KeyExistsAsync(key))
            {
                await _database.KeyDeleteAsync(key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while removing cache value for key: {Key}", key);
            throw;
        }
    }
}
