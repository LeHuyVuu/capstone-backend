using capstone_backend.Business.Interfaces;
using Microsoft.Extensions.Logging;

namespace capstone_backend.Business.Common.Helpers
{
    public static class InsightCacheHelper
    {
        private static readonly string[] CacheKeys = new[]
        {
            "insights:venue:all",
            "insights:venue:today",
            "insights:venue:week",
            "insights:venue:month",
            "insights:venue:year"
        };

        public static async Task ClearAllInsightCachesAsync(IRedisService redisService, ILogger? logger = null)
        {
            if (redisService == null)
            {
                logger?.LogWarning("[INSIGHT CACHE] RedisService is null, cannot clear cache");
                return;
            }

            try
            {
                logger?.LogInformation("[INSIGHT CACHE] Starting to clear {Count} cache keys", CacheKeys.Length);
                
                foreach (var key in CacheKeys)
                {
                    await redisService.RemoveAsync(key);
                    logger?.LogInformation("[INSIGHT CACHE] Cleared cache key: {Key}", key);
                }
                
                logger?.LogInformation("[INSIGHT CACHE] Successfully cleared all insight caches");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "[INSIGHT CACHE] Failed to clear insight caches");
                // Silent fail - cache clearing shouldn't break operations
            }
        }
    }
}
