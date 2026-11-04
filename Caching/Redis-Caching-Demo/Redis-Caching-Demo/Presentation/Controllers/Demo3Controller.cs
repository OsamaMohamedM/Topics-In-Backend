using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Redis_Caching_Demo.Domain.Entities;
using Redis_Caching_Demo.Infrastructure.Persistence;

namespace Redis_Caching_Demo.Presentation.Controllers;

[ApiController]
[Route("api/demo3")]
public class Demo3Controller : ControllerBase
{
    private readonly SqlProductRepository _db;
    private readonly IOutputCacheStore _outputCacheStore;
    private readonly ILogger<Demo3Controller> _logger;

    public Demo3Controller(
        SqlProductRepository db,
        IOutputCacheStore outputCacheStore,
        ILogger<Demo3Controller> logger)
    {
        _db = db;
        _outputCacheStore = outputCacheStore;
        _logger = logger;
    }

    [HttpGet("products")]
    [OutputCache(PolicyName = "ProductList")]
    public async Task<IActionResult> GetProducts(
        [FromQuery] string? category,
        [FromQuery] int page = 1,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Demo3 GetProducts EXECUTING (cache miss or first request). category={Category}, page={Page}", category, page);

        IEnumerable<Product> products;
        if (!string.IsNullOrWhiteSpace(category))
            products = await _db.GetByCategoryAsync(category, ct);
        else
            products = await _db.GetTopAsync(20, ct);
        const int pageSize = 5;
        var paged = products.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Ok(new
        {
            page,
            category,
            count = paged.Count,
            products = paged
        });
    }

    [HttpGet("products/{id:int}")]
    [OutputCache(Duration = 600, Tags = ["products"])]
    public async Task<IActionResult> GetProduct(int id, CancellationToken ct)
    {
        _logger.LogInformation("Demo3 GetProduct EXECUTING (cache miss). id={Id}", id);

        var product = await _db.GetByIdAsync(id, ct);

        if (product is null)
            return NotFound(new { message = $"Product {id} not found." });

        return Ok(product);
    }

    [HttpPost("products")]
    public async Task<IActionResult> AddProduct([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        var product = new Product
        {
            Name = request.Name,
            Category = request.Category,
            Price = request.Price,
            Stock = request.Stock,
            LastUpdated = DateTime.UtcNow,
            ViewCount = 0
        };

        await _db.UpdateAsync(product, ct);
        await _outputCacheStore.EvictByTagAsync("products", ct);
        _logger.LogInformation("Evicted all 'products' tagged output cache entries");

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    [HttpPut("products/{id:int}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductRequest3 request, CancellationToken ct)
    {
        var product = await _db.GetByIdAsync(id, ct);

        if (product is null)
            return NotFound(new { message = $"Product {id} not found." });

        if (request.Name is not null) product.Name = request.Name;
        if (request.Price.HasValue) product.Price = request.Price.Value;
        if (request.Stock.HasValue) product.Stock = request.Stock.Value;

        await _db.UpdateAsync(product, ct);

        await _outputCacheStore.EvictByTagAsync("products", ct);
        _logger.LogInformation("Evicted all 'products' tagged output cache entries after update of product {Id}", id);

        return Ok(new
        {
            message = "Product updated. All 'products' tagged output cache entries have been evicted.",
            product
        });
    }
}

public record CreateProductRequest(string Name, string Category, decimal Price, int Stock);
public record UpdateProductRequest3(string? Name, decimal? Price, int? Stock);