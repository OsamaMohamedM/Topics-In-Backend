namespace Redis_Caching_Demo.Domain.Entities;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public DateTime LastUpdated { get; set; }

    public int ViewCount { get; set; }
}