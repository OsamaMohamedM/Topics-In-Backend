using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Redis_Caching_Demo.Domain.Entities;
using Redis_Caching_Demo.Infrastructure.Cache;
using Redis_Caching_Demo.Infrastructure.Persistence;

namespace Redis_Caching_Demo.Presentation.Controllers;

[ApiController]
[Route("api/demo1")]
public class Demo1Controller : ControllerBase
{
    private readonly IMemoryCache _cache;
    private readonly SqlProductRepository _db;
    private readonly ILogger<Demo1Controller> _logger;
    private static readonly string[] KnownCategories = ["Electronics", "Books", "Clothing"];

    public Demo1Controller(
        IMemoryCache cache,
        SqlProductRepository db,
        ILogger<Demo1Controller> logger)
    {
        _cache = cache;
        _db = db;
        _logger = logger;
    }

    [HttpGet("products/{id:int}")]
    public async Task<IActionResult> GetProduct(int id, CancellationToken ct)
    {
        var key = CacheKeys.Product(id);

        if (_cache.TryGetValue(key, out Product? cached))
        {
            _logger.LogInformation("Cache HIT  [{Key}]", key);
            return Ok(new { source = "cache", product = cached });
        }

        _logger.LogInformation(" Cache MISS [{Key}] — querying DB", key);
        var product = await _db.GetByIdAsync(id, ct);

        if (product is null)
            return NotFound(new { message = $"Product {id} not found." });

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15),
            SlidingExpiration = TimeSpan.FromMinutes(5),
            Size = 1
        };

        _cache.Set(key, product, cacheOptions);
        _logger.LogInformation("Stored [{Key}] in memory cache", key);

        return Ok(new { source = "db", product });
    }

    [HttpGet("categories")]
    public IActionResult GetCategories()
    {
        var key = CacheKeys.Categories;

        if (_cache.TryGetValue(key, out string[]? categories))
        {
            _logger.LogInformation("Cache HIT  [{Key}]", key);
            return Ok(new { source = "cache", categories });
        }
        _logger.LogInformation("Cache MISS [{Key}] — populating from static list", key);

        _cache.Set(key, KnownCategories, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60),
            Size = 1
        });

        return Ok(new { source = "static", categories = KnownCategories });
    }

    [HttpDelete("products/{id:int}/cache")]
    public IActionResult InvalidateCache(int id)
    {
        var key = CacheKeys.Product(id);
        var existed = _cache.TryGetValue(key, out _);

        _cache.Remove(key);

        if (existed)
        {
            _logger.LogInformation("Cache key [{Key}] was found and removed", key);
            return Ok(new { key, removed = true, message = "Cache entry was found and removed. Next GET will hit the DB." });
        }

        _logger.LogInformation("Cache key [{Key}] was not present (nothing to remove)", key);
        return Ok(new { key, removed = false, message = "Cache entry was not present (already expired or never cached)." });
    }
}