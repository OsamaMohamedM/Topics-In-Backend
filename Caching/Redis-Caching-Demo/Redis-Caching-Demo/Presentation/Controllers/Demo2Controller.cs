using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Redis_Caching_Demo.Application.Services;
using Redis_Caching_Demo.Infrastructure.Cache;

namespace Redis_Caching_Demo.Presentation.Controllers;

[ApiController]
[Route("api/demo2")]
public class Demo2Controller : ControllerBase
{
    private readonly ProductService _service;
    private readonly IDistributedCache _cache;
    private readonly ILogger<Demo2Controller> _logger;

    public Demo2Controller(
        ProductService service,
        IDistributedCache cache,
        ILogger<Demo2Controller> logger)
    {
        _service = service;
        _cache = cache;
        _logger = logger;
    }

    [HttpGet("products/{id:int}")]
    public async Task<IActionResult> GetProduct(int id, CancellationToken ct)
    {
        var product = await _service.GetProductAsync(id, ct);

        if (product is null)
            return NotFound(new { message = $"Product {id} not found." });

        return Ok(product);
    }

    [HttpGet("products/{id:int}/cache-status")]
    public async Task<IActionResult> GetCacheStatus(int id, CancellationToken ct)
    {
        var key = CacheKeys.Product(id);
        string? rawValue = null;

        try
        {
            rawValue = await _cache.GetStringAsync(key, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable — cannot check cache status for [{Key}]", key);
            return Ok(new
            {
                key,
                existsInCache = (bool?)null,
                message = "Redis is unavailable — cache status unknown."
            });
        }

        return Ok(new
        {
            key,
            existsInCache = rawValue is not null,
            message = rawValue is not null
                ? "Key exists in Redis. GET /api/demo2/products/{id} would be a cache HIT."
                : "Key is not in Redis. GET /api/demo2/products/{id} will be a cache MISS → will call DB."
        });
    }

    [HttpPut("products/{id:int}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductRequest request, CancellationToken ct)
    {
        var product = await _service.GetProductAsync(id, ct);

        if (product is null)
            return NotFound(new { message = $"Product {id} not found." });

        if (request.Name is not null) product.Name = request.Name;
        if (request.Price.HasValue) product.Price = request.Price.Value;
        if (request.Stock.HasValue) product.Stock = request.Stock.Value;
        if (request.Category is not null) product.Category = request.Category;
        await _service.UpdateProductAsync(product, ct);

        return Ok(new
        {
            message = "Product updated. Cache key has been invalidated — next GET will fetch from DB.",
            product
        });
    }
}

public record UpdateProductRequest(
    string? Name,
    decimal? Price,
    int? Stock,
    string? Category
);