# 🗄️ Redis Caching Demo — .NET 10 Web API

A learning-focused project that demonstrates the **4 core caching patterns** in a real .NET 10 Web API using Redis, EF Core (SQLite), and ASP.NET Core Output Cache.

Each demo is fully isolated — its own controller, its own cache logic — so you can study one pattern at a time.

---

## 📋 Table of Contents

- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
- [Caching Concepts](#caching-concepts)
  - [Cache-Aside](#1-cache-aside-lazy-loading)
  - [Read-Through](#2-read-through)
  - [Write-Through](#3-write-through)
  - [Write-Behind](#4-write-behind-write-back)
- [Demos](#demos)
  - [Demo 1 — IMemoryCache (Cache-Aside)](#demo-1--in-memory-cache-imemorycache)
  - [Demo 2 — Redis Decorator (Read-Through via Decorator)](#demo-2--redis-distributed-cache-decorator-pattern)
  - [Demo 3 — Output Cache](#demo-3--output-cache)
  - [Demo 4 — Write-Behind](#demo-4--write-behind-pattern)
- [Pattern Comparison Table](#pattern-comparison-table)
- [API Endpoints Reference](#api-endpoints-reference)

---

## Tech Stack

| Technology | Purpose |
|---|---|
| **.NET 10 Web API** | Controller-based API framework |
| **Entity Framework Core + SQLite** | Real database with seed data |
| **StackExchange.Redis** | Direct Redis access (INCR, key scanning) |
| **Microsoft.Extensions.Caching.StackExchangeRedis** | `IDistributedCache` backed by Redis |
| **Microsoft.Extensions.Caching.Memory** | `IMemoryCache` (in-process, Demo 1) |
| **ASP.NET Core OutputCache** | Response-level caching (Demo 3) |
| **Scrutor** | Decorator pattern DI registration (Demo 2) |

---

## Project Structure

```
Redis-Caching-Demo/
├── Domain/
│   └── Entities/
│       └── Product.cs                      ← Shared entity (Id, Name, Category, Price, Stock, LastUpdated, ViewCount)
├── Application/
│   ├── Interfaces/
│   │   └── IProductRepository.cs           ← Repository contract
│   └── Services/
│       └── ProductService.cs               ← Thin service, 100% cache-unaware
├── Infrastructure/
│   ├── Persistence/
│   │   ├── AppDbContext.cs                 ← EF Core + SQLite + 12 seeded products
│   │   └── SqlProductRepository.cs         ← Real DB implementation
│   └── Cache/
│       ├── CacheKeys.cs                    ← Centralized key factory
│       └── CachedProductRepository.cs      ← Demo 2: Redis decorator
├── BackgroundServices/
│   └── ViewCountFlushService.cs            ← Demo 4: periodic Redis → DB flush
└── Presentation/
    └── Controllers/
        ├── Demo1Controller.cs              ← IMemoryCache Cache-Aside
        ├── Demo2Controller.cs              ← Redis via Decorator Pattern
        ├── Demo3Controller.cs              ← Output Cache + tag eviction
        └── Demo4Controller.cs              ← Write-Behind INCR
```

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download)
- Redis running on `localhost:6379`
  - **Docker:** `docker run -d -p 6379:6379 redis`
  - **Windows:** [Redis for Windows](https://github.com/tporadowski/redis/releases)

> **Note:** If Redis is not running, Demo 1 and Demo 3 still work. Demo 2 silently falls back to the DB. Demo 4 will fail to connect.

### Run

```bash
cd Redis-Caching-Demo
dotnet run
```

The SQLite database (`caching-demo.db`) is **auto-created** on first run with 12 seeded products across 3 categories (Electronics, Books, Clothing). No migrations needed.

- **Swagger UI:** `https://localhost:{port}/swagger`
- **OpenAPI:** `https://localhost:{port}/openapi/v1.json`

---

## Caching Concepts

### 1. Cache-Aside (Lazy Loading)

The application **manually manages** the cache. The cache is not involved in the data loading path automatically — the code has to check, load, and store explicitly.

```
READ:
  1. Check cache for key
  2. HIT  → return cached value                    (fast path)
  3. MISS → query DB → store in cache → return     (slow path, only on first read)

WRITE:
  → Write directly to DB
  → Optionally delete/update the cache key
```

**Pros:**
- Only caches data that is actually requested
- Cache failures don't break the app (DB is the fallback)
- Full control over what goes in and stays in the cache

**Cons:**
- First request after a cache miss (or startup) is always slow
- Risk of stale data if you forget to invalidate the cache after writes
- Cache logic lives in application code (not hidden)

**Used in → [Demo 1](#demo-1--in-memory-cache-imemorycache) (IMemoryCache) and [Demo 2](#demo-2--redis-distributed-cache-decorator-pattern) (Redis Decorator)**

---

### 2. Read-Through

Very similar to Cache-Aside, but the **cache layer sits in front of the DB transparently**. The application calls the cache, and the cache itself is responsible for querying the DB on a miss. The application never talks to the DB directly.

```
READ:
  1. App calls cache.Get(key)
  2. HIT  → cache returns value
  3. MISS → cache fetches from DB, stores it, returns it
  (App never knows whether it was a hit or miss)

WRITE:
  → Usually same as Cache-Aside (write to DB, invalidate cache)
```

**Pros:**
- Application code is cleaner — it only talks to one interface
- Cache population is automatic and consistent

**Cons:**
- First-access latency still exists (same as Cache-Aside)
- Requires a cache implementation that knows how to load from the DB

**Used in → [Demo 2](#demo-2--redis-distributed-cache-decorator-pattern) via the Decorator Pattern**

> In Demo 2, `CachedProductRepository` wraps `SqlProductRepository`. `ProductService` only calls `IProductRepository` — it never knows if the response came from Redis or the DB.

---

### 3. Write-Through

When the application writes data, it **writes to the cache AND the DB simultaneously** (cache first, then DB — or both in the same call). The cache is always up-to-date.

```
WRITE:
  1. App writes to cache
  2. Cache immediately writes to DB
  3. Both are always in sync

READ:
  → Always a cache hit (data was pre-populated on write)
```

**Pros:**
- Cache is always fresh — no stale data
- Read path is always fast (cache is pre-warmed on writes)

**Cons:**
- Writes are slower (double write: cache + DB)
- Cache may hold data that is never read (wastes memory)

**Used in → [Demo 2](#demo-2--redis-distributed-cache-decorator-pattern) for the invalidation step**

> In Demo 2, `CachedProductRepository.UpdateAsync()` updates the DB first, then immediately removes the stale cache key. This is the Write-Through invalidation variant — the next read will re-populate the cache with fresh data.

---

### 4. Write-Behind (Write-Back)

The application **writes to the cache first and returns immediately**. A background process asynchronously flushes the cache data to the DB later. The DB is **eventually consistent** with the cache.

```
WRITE:
  1. App writes to cache (Redis) → returns immediately (very fast)
  2. Background worker periodically reads cache → writes to DB

READ:
  → Read from cache (source of truth)
  → Fall back to DB if cache key doesn't exist
```

**Pros:**
- Extremely fast writes (no DB round-trip on the hot path)
- DB write pressure is drastically reduced (batch updates)
- Ideal for high-frequency, low-criticality data

**Cons:**
- Data loss risk: if cache crashes before the flush, recent writes are lost
- DB is stale between flush intervals
- More complex to implement (requires a background worker)

**Used in → [Demo 4](#demo-4--write-behind-pattern)**

> In Demo 4, each product view increments a Redis counter via `INCR` (atomic, sub-millisecond). `ViewCountFlushService` runs every 30 seconds and batch-writes all counters to `Product.ViewCount` in SQLite.

---

## Demos

### Demo 1 — In-Memory Cache (IMemoryCache)

**Pattern:** Cache-Aside  
**Cache:** `IMemoryCache` (in-process, single-server only)  
**When to use:** Single-server apps with rarely-changing reference data (config, lookup tables, product catalog)

#### How It Works

```
GET /api/demo1/products/{id}

Request → Demo1Controller → IMemoryCache.TryGetValue(key)
             │
         HIT ─────────────────────────────► return { source: "cache", product }
             │
         MISS → SqlProductRepository.GetByIdAsync()
                    → IMemoryCache.Set(key, product, options)
                    └───────────────────────────────► return { source: "db", product }
```

#### Key Configuration

```csharp
// Program.cs — SizeLimit is required for per-entry Size to be respected
builder.Services.AddMemoryCache(options => { options.SizeLimit = 1000; });
```

```csharp
// Demo1Controller.cs — per-entry options
var cacheOptions = new MemoryCacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15), // Hard max lifetime
    SlidingExpiration = TimeSpan.FromMinutes(5),                // Evict if idle for 5 min
    Size = 1                                                    // Counts against SizeLimit
};
```

- **AbsoluteExpiration:** The entry is evicted after 15 minutes no matter how often it's accessed.
- **SlidingExpiration:** If nobody reads the entry for 5 minutes, it's evicted early. Resets on each access (but never past the absolute limit).
- **SizeLimit:** Prevents unbounded memory growth. Without it, `Size` on individual entries is ignored.

#### Limitations

- **Not distributed:** Each server process has its own separate memory cache. Running 2 instances = 2 different caches = stale data risk.
- **Not persistent:** A restart clears all entries.
- Use [Demo 2](#demo-2--redis-distributed-cache-decorator-pattern) for multi-server scenarios.

#### Endpoints

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/demo1/products/{id}` | Cache-Aside: returns `{ source: "cache"\|"db", product }` |
| `GET` | `/api/demo1/categories` | Caches static list with 60-min TTL |
| `DELETE` | `/api/demo1/products/{id}/cache` | Manual eviction, returns `{ removed: true\|false }` |

---

### Demo 2 — Redis Distributed Cache (Decorator Pattern)

**Pattern:** Read-Through + Cache-Aside via Decorator  
**Cache:** `IDistributedCache` backed by Redis  
**When to use:** Multi-server / load-balanced apps that need a shared cache

#### How It Works

The key insight is the **Decorator Pattern**: `CachedProductRepository` wraps `SqlProductRepository` and is registered at the DI level using Scrutor. `ProductService` only knows about `IProductRepository` — it has zero cache code.

```
GET /api/demo2/products/{id}

Request → Demo2Controller → ProductService.GetProductAsync()
                                → IProductRepository.GetByIdAsync()  ← DI resolves this as:
                                    CachedProductRepository.GetByIdAsync()
                                         │
                                     HIT ─────────────────────────────► deserialize JSON → return
                                         │
                                     MISS → SqlProductRepository.GetByIdAsync()
                                                → Redis.SetStringAsync(key, JSON)
                                                └──────────────────────────────► return
```

#### Scrutor Registration

```csharp
// Program.cs
services.AddScoped<IProductRepository, SqlProductRepository>();  // Step 1: register real impl
services.Decorate<IProductRepository, CachedProductRepository>(); // Step 2: wrap with decorator
```

After this, the DI chain is:
```
IProductRepository → CachedProductRepository → SqlProductRepository → AppDbContext
```

`ProductService` is injected with `IProductRepository` and never sees the caching layer.

#### TTL Strategy

```csharp
private static readonly DistributedCacheEntryOptions CacheOptions = new()
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30), // Hard max
    SlidingExpiration = TimeSpan.FromMinutes(10)                // Idle eviction
};
```

#### Redis Failure Resilience

Every Redis call in `CachedProductRepository` is wrapped in a `try/catch`. If Redis is down:
- A warning is logged
- The call falls through to `SqlProductRepository` (the DB)
- No exception is thrown — the service continues normally

The cache is treated as an **optimization**, not a hard dependency.

#### Endpoints

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/demo2/products/{id}` | Read via decorator (transparent caching) |
| `GET` | `/api/demo2/products/{id}/cache-status` | Probe Redis: `{ existsInCache: true\|false }` |
| `PUT` | `/api/demo2/products/{id}` | Update DB → cache key auto-invalidated by decorator |

---

### Demo 3 — Output Cache

**Pattern:** Response-level caching (full HTTP response cached)  
**Cache:** ASP.NET Core Output Cache Middleware  
**When to use:** Public pages with the same response for all users (no personalization)

#### How It Works

Output Cache is fundamentally different from Demo 1 & 2:

- **Demo 1 & 2:** You manually check the cache, call the DB, store the result, and return.
- **Demo 3:** You put `[OutputCache]` on the action. On a cache hit, **the action method does not execute at all** — the response is served directly by the middleware.

```
GET /api/demo3/products?category=Electronics

First request:
  Middleware → no cached response found
  → Demo3Controller.GetProducts() EXECUTES
  → Middleware stores the full HTTP response
  → Returns to client

Second request (same query string, within 10 min):
  Middleware → cached response found ⚡
  → Demo3Controller.GetProducts() DOES NOT EXECUTE
  → Returns stored response directly
```

#### Policy Definition

```csharp
// Program.cs
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("ProductList", policy => policy
        .Expire(TimeSpan.FromMinutes(10))
        .Tag("products")                          // Groups entries for bulk eviction
        .SetVaryByQuery("category", "page"));     // Separate entry per query combo
});
```

#### Tag-Based Eviction

After any write (POST/PUT), all cache entries tagged `"products"` are evicted in one call:

```csharp
await _outputCacheStore.EvictByTagAsync("products", ct);
```

This removes both the listing response (`/products?category=Electronics`) **and** all individual product responses (`/products/1`, `/products/2`, etc.) in one shot — without needing to know the exact cache keys.

#### Endpoints

| Method | Route | Cache Behavior |
|---|---|---|
| `GET` | `/api/demo3/products?category=&page=` | Cached 10 min; varies by category + page |
| `GET` | `/api/demo3/products/{id}` | Cached 600 seconds, tagged "products" |
| `POST` | `/api/demo3/products` | Adds product → evicts all "products" tagged entries |
| `PUT` | `/api/demo3/products/{id}` | Updates product → evicts all "products" tagged entries |

---

### Demo 4 — Write-Behind Pattern

**Pattern:** Write-Behind (Write-Back)  
**Cache:** Redis (direct `IDatabase`)  
**When to use:** High-frequency writes where losing a few updates is acceptable (counters, analytics, view counts, like counts)

#### How It Works

```
GET /api/demo4/products/{id}

  1. SqlProductRepository.GetByIdAsync() → return product to client
  2. IDatabase.StringIncrementAsync("demo:product:v1:{id}:views")
     → atomic Redis INCR, no DB call, sub-millisecond
     (client response is NOT delayed by this)

Every 30 seconds — ViewCountFlushService:
  1. Scan Redis for all keys matching "demo:product:v1:*:views"
  2. For each key: read counter value, parse product ID
  3. UPDATE Products SET ViewCount = {redis_count} WHERE Id = {id}
  4. SaveChangesAsync() → one DB round-trip for all products
  (Redis keys are NOT deleted — they are the running total)
```

#### Why Redis INCR?

`INCR` in Redis is a **single atomic operation**. Even if 10,000 requests hit the endpoint simultaneously, each increment is applied exactly once with no race conditions. No locking, no transactions needed.

Contrast with a naive DB approach:
```
// UNSAFE under concurrency (race condition):
var product = db.Find(id);
product.ViewCount++;      // Two threads read the same value → one increment is lost
db.SaveChanges();

// Redis INCR — always correct:
await redis.StringIncrementAsync(key);   // Atomic, server-side, no race
```

#### Source of Truth

| Source | Freshness | Used For |
|---|---|---|
| **Redis counter** | Real-time (updated on every request) | `GET /api/demo4/products/{id}/views` |
| **DB ViewCount** | Up to 30 seconds stale | Fallback if Redis key is absent; persistent storage |

#### ViewCountFlushService

```csharp
// Runs every 30 seconds
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        await FlushViewCountsAsync(stoppingToken);
    }
}
```

Key decisions:
- **Does NOT delete Redis keys after flush** — keeps the running total; deletion would reset the counter.
- **Single `SaveChangesAsync()` call** — all products updated in one DB round-trip (batch write).
- **Errors are caught and logged** — the background service never crashes on transient Redis/DB errors.

#### Trade-offs

| | Write-Through (per-request DB write) | Write-Behind (Demo 4) |
|---|---|---|
| **Write speed** | Slow (DB on every request) | Fast (Redis INCR only) |
| **Data loss risk** | None | Yes (up to 30s of counts if Redis crashes) |
| **DB pressure** | High (1 write per view) | Low (1 write per 30s per product) |
| **Use case** | Orders, payments, balances | View counts, likes, analytics |

#### Endpoints

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/demo4/products/{id}` | Returns product + increments Redis counter |
| `GET` | `/api/demo4/products/{id}/views` | Returns real-time count from Redis (or DB fallback) |

---

## Pattern Comparison Table

| | **Cache-Aside** | **Read-Through** | **Write-Through** | **Write-Behind** |
|---|---|---|---|---|
| **Who loads the cache?** | Application code | Cache layer (transparent) | Cache layer (on write) | Background worker |
| **Cache miss path** | App → DB → Cache → App | App → Cache → DB → Cache → App | N/A (pre-populated on write) | App → Redis → (flush later) |
| **Write path** | App → DB (cache invalidated manually) | App → DB (cache invalidated) | App → Cache → DB (simultaneous) | App → Cache → (DB async later) |
| **Stale data risk** | Medium (depend on TTL + invalidation) | Medium | Low (always in sync) | High (intentional) |
| **Write speed** | Fast | Fast | Slower (double write) | Very fast |
| **Complexity** | Low | Medium | Medium | High |
| **Data loss risk** | None | None | None | Yes (crash before flush) |
| **Demo** | Demo 1, Demo 2 | Demo 2 (Decorator) | Demo 2 (invalidation) | Demo 4 |

---

## API Endpoints Reference

### Demo 1 — IMemoryCache
```
GET    /api/demo1/products/{id}          → Cache-Aside (IMemoryCache, 15-min TTL)
GET    /api/demo1/categories             → Cached category list (60-min TTL)
DELETE /api/demo1/products/{id}/cache    → Manual cache eviction
```

### Demo 2 — Redis Distributed (Decorator)
```
GET /api/demo2/products/{id}                  → Read-Through via CachedProductRepository
GET /api/demo2/products/{id}/cache-status     → Probe Redis: { existsInCache: bool }
PUT /api/demo2/products/{id}                  → Update DB + auto-invalidate Redis key
    Body: { "name": "...", "price": 9.99, "stock": 10, "category": "..." }
```

### Demo 3 — Output Cache
```
GET  /api/demo3/products?category=Electronics&page=1  → Output cached (10 min, varies by query)
GET  /api/demo3/products/{id}                         → Output cached (600 seconds)
POST /api/demo3/products                              → Add product + evict "products" tag
     Body: { "name": "...", "category": "...", "price": 9.99, "stock": 10 }
PUT  /api/demo3/products/{id}                         → Update product + evict "products" tag
     Body: { "name": "...", "price": 9.99, "stock": 10 }
```

### Demo 4 — Write-Behind
```
GET /api/demo4/products/{id}        → Returns product + increments Redis view counter
GET /api/demo4/products/{id}/views  → Returns { viewCount, source: "redis"|"db" }
```

---

## Cache Key Convention

All cache keys follow a consistent naming scheme defined in `CacheKeys.cs`:

```
demo:product:v1:{id}           → Single product
demo:product:v1:list:{category} → Product list by category
demo:product:v1:{id}:views     → View counter (Demo 4)
demo:product:v1:categories     → Static category list (Demo 1)
```

The `v1` segment enables **instant cache-wide invalidation** — changing it to `v2` in `CacheKeys.cs` makes all existing cached entries unreachable (they expire naturally via TTL).
