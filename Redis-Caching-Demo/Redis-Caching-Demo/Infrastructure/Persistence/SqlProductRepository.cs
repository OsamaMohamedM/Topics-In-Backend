using Microsoft.EntityFrameworkCore;
using Redis_Caching_Demo.Application.Interfaces;
using Redis_Caching_Demo.Domain.Entities;

namespace Redis_Caching_Demo.Infrastructure.Persistence;

public class SqlProductRepository : IProductRepository
{
    private readonly AppDbContext _db;

    public SqlProductRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Product?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<List<Product>> GetByCategoryAsync(string category, CancellationToken ct = default)
    {
        return await _db.Products
            .AsNoTracking()
            .Where(p => p.Category == category)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);
    }

    public async Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        product.LastUpdated = DateTime.UtcNow;
        _db.Products.Update(product);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<Product>> GetTopAsync(int count, CancellationToken ct = default)
    {
        return await _db.Products
            .AsNoTracking()
            .OrderByDescending(p => (int)p.Price)// becouse we using sqllite
            .Take(count)
            .ToListAsync(ct);
    }
}