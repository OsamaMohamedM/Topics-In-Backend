using Microsoft.EntityFrameworkCore;
using Redis_Caching_Demo.Domain.Entities;

namespace Redis_Caching_Demo.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Category).IsRequired().HasMaxLength(100);
            entity.Property(p => p.Price).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Product>().HasData(

            new Product { Id = 1, Name = "Laptop Pro 15", Category = "Electronics", Price = 1299.99m, Stock = 45, LastUpdated = new DateTime(2025, 1, 10), ViewCount = 0 },
            new Product { Id = 2, Name = "Wireless Headphones", Category = "Electronics", Price = 249.99m, Stock = 120, LastUpdated = new DateTime(2025, 1, 15), ViewCount = 0 },
            new Product { Id = 3, Name = "Mechanical Keyboard", Category = "Electronics", Price = 149.99m, Stock = 75, LastUpdated = new DateTime(2025, 2, 1), ViewCount = 0 },
            new Product { Id = 4, Name = "4K Monitor 27\"", Category = "Electronics", Price = 549.99m, Stock = 30, LastUpdated = new DateTime(2025, 2, 5), ViewCount = 0 },

            new Product { Id = 5, Name = "Clean Code", Category = "Books", Price = 39.99m, Stock = 200, LastUpdated = new DateTime(2025, 1, 20), ViewCount = 0 },
            new Product { Id = 6, Name = "Domain-Driven Design", Category = "Books", Price = 54.99m, Stock = 85, LastUpdated = new DateTime(2025, 1, 22), ViewCount = 0 },
            new Product { Id = 7, Name = "Designing Data-Intensive Apps", Category = "Books", Price = 59.99m, Stock = 110, LastUpdated = new DateTime(2025, 2, 10), ViewCount = 0 },
            new Product { Id = 8, Name = "The Pragmatic Programmer", Category = "Books", Price = 44.99m, Stock = 95, LastUpdated = new DateTime(2025, 2, 12), ViewCount = 0 },
            new Product { Id = 9, Name = "Merino Wool Sweater", Category = "Clothing", Price = 89.99m, Stock = 60, LastUpdated = new DateTime(2025, 1, 25), ViewCount = 0 },
            new Product { Id = 10, Name = "Running Jacket", Category = "Clothing", Price = 129.99m, Stock = 40, LastUpdated = new DateTime(2025, 1, 28), ViewCount = 0 },
            new Product { Id = 11, Name = "Denim Jeans", Category = "Clothing", Price = 79.99m, Stock = 150, LastUpdated = new DateTime(2025, 2, 15), ViewCount = 0 },
            new Product { Id = 12, Name = "Casual Sneakers", Category = "Clothing", Price = 109.99m, Stock = 80, LastUpdated = new DateTime(2025, 2, 18), ViewCount = 0 }
        );
    }
}