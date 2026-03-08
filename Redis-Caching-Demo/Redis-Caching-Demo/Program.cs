using Microsoft.EntityFrameworkCore;
using Redis_Caching_Demo.Application.Interfaces;
using Redis_Caching_Demo.Application.Services;
using Redis_Caching_Demo.BackgroundServices;
using Redis_Caching_Demo.Infrastructure.Cache;
using Redis_Caching_Demo.Infrastructure.Persistence;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSwaggerGen();
builder.Services.AddScoped<SqlProductRepository>();

builder.Services.AddScoped<ProductService>();

builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1000;
});

var redisConnectionString = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = string.Empty;
});

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var config = ConfigurationOptions.Parse(redisConnectionString);
    config.AbortOnConnectFail = false;
    return ConnectionMultiplexer.Connect(config);
});

builder.Services.AddScoped<IProductRepository, SqlProductRepository>();
builder.Services.Decorate<IProductRepository, CachedProductRepository>();

builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("ProductList", policy => policy
        .Expire(TimeSpan.FromMinutes(10))
        .Tag("products")
        .SetVaryByQuery("category", "page"));
});

builder.Services.AddHostedService<ViewCountFlushService>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}
app.UseHttpsRedirection();
app.UseRouting();
app.UseOutputCache();

app.MapControllers();

app.Run();