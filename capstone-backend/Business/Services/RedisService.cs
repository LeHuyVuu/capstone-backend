using capstone_backend.Business.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace capstone_backend.Business.Services
{
    public class RedisService : IRedisService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public RedisService(IConnectionMultiplexer redis)
        {
            _redis = redis;
            _db = redis.GetDatabase();
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                var value = await _db.StringGetAsync(key);
                if (value.IsNullOrEmpty) return default;
                return JsonSerializer.Deserialize<T>(value!, _jsonOptions);
            }
            catch
            {
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            try
            {
                var json = JsonSerializer.Serialize(value, _jsonOptions);
                await _db.StringSetAsync(key, json, expiry ?? TimeSpan.FromMinutes(15));
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task RemoveAsync(string key)
        {
            try 
            { 
                await _db.KeyDeleteAsync(key); 
            }
            catch (Exception)
            {
                throw;
            }
        }

        // This method tries to get the value from Redis cache. If not, it calls the provided dbFallback function to get the data from the database then store it in redis
        public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> dbFallback, TimeSpan? expiry = null)
        {
            // Check if redis has the value
            var cachedValue = await GetAsync<T>(key);
            if (cachedValue != null) return cachedValue;

            // Db fallback if cache miss
            var data = await dbFallback();

            if (data != null)
            {
                await SetAsync(key, data, expiry);
            }

            return data;
        }
    }
}
