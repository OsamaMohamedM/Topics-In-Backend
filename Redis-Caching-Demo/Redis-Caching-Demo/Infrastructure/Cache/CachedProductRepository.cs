using Microsoft.Extensions.Caching.Distributed;
using Redis_Caching_Demo.Application.Interfaces;
using Redis_Caching_Demo.Domain.Entities;
using Redis_Caching_Demo.Infrastructure.Cache;
using System.Text.Json;

namespace Redis_Caching_Demo.Infrastructure.Cache;

public class CachedProductRepository : IProductRepository
{
    private readonly IProductRepository _inner;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachedProductRepository> _logger;

    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
        SlidingExpiration = TimeSpan.FromMinutes(10)
    };

    public CachedProductRepository(
        IProductRepository inner,
        IDistributedCache cache,
        ILogger<CachedProductRepository> logger)
    {
        _inner = inner;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Product?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var key = CacheKeys.Product(id);

        try
        {
            var cached = await _cache.GetStringAsync(key, ct);
            if (cached is not null)
            {
                _logger.LogInformation("Cache HIT  [{Key}]", key);
                return JsonSerializer.Deserialize<Product>(cached);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for GET [{Key}] — falling back to DB", key);
            return await _inner.GetByIdAsync(id, ct);
        }
        _logger.LogInformation("Cache MISS [{Key}] — querying DB", key);
        var product = await _inner.GetByIdAsync(id, ct);

        if (product is not null)
        {
            try
            {
                var serialized = JsonSerializer.Serialize(product);
                await _cache.SetStringAsync(key, serialized, CacheOptions, ct);
                _logger.LogInformation("Saved [{Key}] to Redis", key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis unavailable for SET [{Key}] — skipping cache write", key);
            }
        }

        return product;
    }

    public async Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        await _inner.UpdateAsync(product, ct);
        var key = CacheKeys.Product(product.Id);
        try
        {
            await _cache.RemoveAsync(key, ct);
            _logger.LogInformation("Invalidated cache key [{Key}]", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable — could not invalidate [{Key}]", key);
        }
    }

    public async Task<List<Product>> GetByCategoryAsync(string category, CancellationToken ct = default)
    {
        var key = CacheKeys.Category(category);

        try
        {
            var cached = await _cache.GetStringAsync(key, ct);
            if (cached is not null)
            {
                _logger.LogInformation("Cache HIT  [{Key}]", key);
                return JsonSerializer.Deserialize<List<Product>>(cached) ?? [];
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for GET [{Key}] — falling back to DB", key);
            return await _inner.GetByCategoryAsync(category, ct);
        }

        _logger.LogInformation(" Cache MISS [{Key}] — querying DB", key);
        var products = await _inner.GetByCategoryAsync(category, ct);

        try
        {
            await _cache.SetStringAsync(key, JsonSerializer.Serialize(products), CacheOptions, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for SET [{Key}]", key);
        }

        return products;
    }

    public Task<List<Product>> GetTopAsync(int count, CancellationToken ct = default)
        => _inner.GetTopAsync(count, ct);
}