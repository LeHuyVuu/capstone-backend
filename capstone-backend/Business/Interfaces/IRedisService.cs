namespace capstone_backend.Business.Interfaces
{
    public interface IRedisService
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
        Task RemoveAsync(string key);
        Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> dbFallback, TimeSpan? expiry = null);
    }
}
