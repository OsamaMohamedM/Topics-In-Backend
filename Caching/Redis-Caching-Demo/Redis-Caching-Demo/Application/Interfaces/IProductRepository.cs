using Redis_Caching_Demo.Domain.Entities;

namespace Redis_Caching_Demo.Application.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<List<Product>> GetByCategoryAsync(string category, CancellationToken ct = default);

    Task UpdateAsync(Product product, CancellationToken ct = default);

    Task<List<Product>> GetTopAsync(int count, CancellationToken ct = default);
}