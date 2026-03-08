using Microsoft.EntityFrameworkCore;
using Redis_Caching_Demo.Infrastructure.Persistence;
using StackExchange.Redis;

namespace Redis_Caching_Demo.BackgroundServices;

public class ViewCountFlushService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<ViewCountFlushService> _logger;
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(30);

    public ViewCountFlushService(
        IServiceScopeFactory scopeFactory,
        IConnectionMultiplexer redis,
        ILogger<ViewCountFlushService> logger)
    {
        _scopeFactory = scopeFactory;
        _redis = redis;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ViewCountFlushService started — flushing every {Interval}s", FlushInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(FlushInterval, stoppingToken);

            await FlushViewCountsAsync(stoppingToken);
        }

        _logger.LogInformation("ViewCountFlushService stopped");
    }

    private async Task FlushViewCountsAsync(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Background flush: scanning Redis for view counters...");

            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var db = _redis.GetDatabase();
            var viewKeys = server
                .Keys(pattern: "demo:product:v1:*:views")
                .ToList();

            if (viewKeys.Count == 0)
            {
                _logger.LogInformation("No view counter keys found in Redis — nothing to flush");
                return;
            }

            _logger.LogInformation("Found {Count} view counter key(s) to flush", viewKeys.Count);

            var updates = new Dictionary<int, long>();
            foreach (var key in viewKeys)
            {
                var keyStr = key.ToString();
                var parts = keyStr.Split(':');

                if (parts.Length >= 4 && int.TryParse(parts[3], out var productId))
                {
                    var value = await db.StringGetAsync(key);
                    if (value.HasValue && long.TryParse((string?)value, out var count))
                    {
                        updates[productId] = count;
                    }
                }
            }

            if (updates.Count == 0)
                return;
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var productIds = updates.Keys.ToList();
            var products = await dbContext.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync(ct);

            int flushed = 0;
            foreach (var product in products)
            {
                if (updates.TryGetValue(product.Id, out var redisCount))
                {
                    product.ViewCount = (int)redisCount;
                    flushed++;
                }
            }

            await dbContext.SaveChangesAsync(ct);
            _logger.LogInformation("Flushed {Count} view count(s) to DB", flushed);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during view count flush — will retry in {Interval}s", FlushInterval.TotalSeconds);
        }
    }
}