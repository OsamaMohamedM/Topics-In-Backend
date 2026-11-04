using Redis_Caching_Demo.Application.Interfaces;
using Redis_Caching_Demo.Domain.Entities;

namespace Redis_Caching_Demo.Application.Services;

public class ProductService
{
    private readonly IProductRepository _repository;

    public ProductService(IProductRepository repository)
    {
        _repository = repository;
    }

    public Task<Product?> GetProductAsync(int id, CancellationToken ct = default)
        => _repository.GetByIdAsync(id, ct);

    public Task<List<Product>> GetProductsByCategoryAsync(string category, CancellationToken ct = default)
        => _repository.GetByCategoryAsync(category, ct);

    public Task UpdateProductAsync(Product product, CancellationToken ct = default)
        => _repository.UpdateAsync(product, ct);

    public Task<List<Product>> GetTopProductsAsync(int count, CancellationToken ct = default)
        => _repository.GetTopAsync(count, ct);
}