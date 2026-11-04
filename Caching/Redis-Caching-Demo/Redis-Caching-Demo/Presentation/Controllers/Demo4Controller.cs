using Microsoft.AspNetCore.Mvc;
using Redis_Caching_Demo.Infrastructure.Cache;
using Redis_Caching_Demo.Infrastructure.Persistence;
using StackExchange.Redis;

namespace Redis_Caching_Demo.Presentation.Controllers;

[ApiController]
[Route("api/demo4")]
public class Demo4Controller : ControllerBase
{
    private readonly SqlProductRepository _db;
    private readonly IDatabase _redis;
    private readonly ILogger<Demo4Controller> _logger;

    public Demo4Controller(
        SqlProductRepository db,
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<Demo4Controller> logger)
    {
        _db = db;
        _redis = connectionMultiplexer.GetDatabase();
        _logger = logger;
    }

    [HttpGet("products/{id:int}")]
    public async Task<IActionResult> GetProduct(int id, CancellationToken ct)
    {
        var product = await _db.GetByIdAsync(id, ct);

        if (product is null)
            return NotFound(new { message = $"Product {id} not found." });
        var viewKey = CacheKeys.Views(id);
        var newCount = await _redis.StringIncrementAsync(viewKey);

        _logger.LogInformation("Incremented view count for product {Id}: Redis counter = {Count}", id, newCount);

        return Ok(new
        {
            product,
            viewCountInRedis = newCount,
            note = "View counter incremented in Redis. Will be flushed to DB by background service every 30 seconds."
        });
    }

    [HttpGet("products/{id:int}/views")]
    public async Task<IActionResult> GetViewCount(int id, CancellationToken ct)
    {
        var viewKey = CacheKeys.Views(id);
        var redisValue = await _redis.StringGetAsync(viewKey);

        if (redisValue.HasValue)
        {
            _logger.LogInformation("View count for product {Id} from Redis: {Count}", id, (long)redisValue);

            return Ok(new
            {
                productId = id,
                viewCount = (long)redisValue,
                source = "redis",
                note = "This is the real-time count from Redis. DB may be up to 30 seconds behind."
            });
        }

        var product = await _db.GetByIdAsync(id, ct);

        if (product is null)
            return NotFound(new { message = $"Product {id} not found." });
        _logger.LogInformation("View count for product {Id} from DB (Redis key absent): {Count}", id, product.ViewCount);

        return Ok(new
        {
            productId = id,
            viewCount = product.ViewCount,
            source = "db",
            note = "Redis key not found (Redis may have restarted). Showing the last value flushed to the DB."
        });
    }
}