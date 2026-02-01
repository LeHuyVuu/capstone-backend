using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IRedisService _redis;

    public HealthController(IRedisService redis)
    {
        _redis = redis;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });

        
    }

    /// <summary>
    /// Test redis connection
    /// </summary>
    [HttpGet("ping")]
    public async Task<IActionResult> Ping()
    {
        var key = "system:status";
        var value = $"Online {DateTime.Now}";

        // Write to Redis
        await _redis.SetAsync(key, value, TimeSpan.FromMinutes(1));

        // Read from Redis
        var cachedValue = await _redis.GetAsync<string>(key);

        return Ok(new
        {
            Status = "Success",
            Message = "Redis is ready!",
            Written = value,
            Read = cachedValue,
            Match = value == cachedValue
        });
    }

    /// <summary>
    /// Test set redis
    /// </summary>
    [HttpGet("cache-aside")]
    public async Task<IActionResult> TestCacheAside()
    {
        var key = "user:fake:123";

        // Simulate a fake DB call
        Func<Task<object>> fakeDbCall = async () =>
        {
            await Task.Delay(2000); // Fake sleep 2s to simulate DB call
            return new { Id = 123, Name = "Nam Dev", Role = "Admin" };
        };

        var start = DateTime.Now;

        // Call 1: will be slow (cache miss)
        var data = await _redis.GetOrSetAsync(key, fakeDbCall, TimeSpan.FromMinutes(5));

        var duration = (DateTime.Now - start).TotalMilliseconds;

        return Ok(new
        {
            Data = data,
            TimeTakenMs = duration,
            Note = duration > 2000
                ? "First time: Slow for call DB (Cache Miss)"
                : "Later: Fast for take from redis (Cache Hit)"
        });
    }

    /// <summary>
    /// Clear test cache
    /// </summary>
    [HttpDelete("clear")]
    public async Task<IActionResult> Clear()
    {
        await _redis.RemoveAsync("user:fake:123");
        return Ok("Removed fake user!");
    }
}
