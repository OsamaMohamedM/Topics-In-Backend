namespace Redis_Caching_Demo.Infrastructure.Cache;

public static class CacheKeys
{
    public static string Product(int id) => $"demo:product:v1:{id}";

    public static string Category(string category) => $"demo:product:v1:list:{category}";

    public static string Views(int id) => $"demo:product:v1:{id}:views";

    public const string Categories = "demo:product:v1:categories";
}