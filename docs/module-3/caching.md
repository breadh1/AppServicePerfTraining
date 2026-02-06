# Caching Strategies for Azure App Service

## Overview

Caching is one of the most effective performance optimizations. This guide covers various caching strategies for Azure App Service applications.

## Types of Caching

### 1. In-Memory Caching

Best for single-instance scenarios or session-specific data.

**Advantages:**
- Fastest access (no network overhead)
- Simple to implement
- No additional infrastructure

**Disadvantages:**
- Not shared across instances
- Lost on app restart
- Limited by instance memory

**Implementation (ASP.NET Core):**

```csharp
// Startup configuration
services.AddMemoryCache();

// Usage in controller
public class ProductController : ControllerBase
{
    private readonly IMemoryCache _cache;
    private const string CacheKey = "ProductList";
    
    public ProductController(IMemoryCache cache)
    {
        _cache = cache;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        if (!_cache.TryGetValue(CacheKey, out List<Product> products))
        {
            products = await _productService.GetAllAsync();
            
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5))
                .SetSlidingExpiration(TimeSpan.FromMinutes(2));
            
            _cache.Set(CacheKey, products, cacheOptions);
        }
        
        return Ok(products);
    }
}
```

### 2. Distributed Caching

Best for multi-instance scenarios requiring shared cache.

**Common Options:**
- Azure Redis Cache
- SQL Server
- NCache
- Cosmos DB

**Advantages:**
- Shared across all instances
- Survives app restarts
- Scalable

**Disadvantages:**
- Network latency
- Additional cost
- More complex setup

**Implementation with Redis:**

```csharp
// Startup configuration
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = Configuration.GetConnectionString("Redis");
    options.InstanceName = "MyApp:";
});

// Usage
public class ProductController : ControllerBase
{
    private readonly IDistributedCache _cache;
    
    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        var cacheKey = "products:all";
        var cachedData = await _cache.GetStringAsync(cacheKey);
        
        if (cachedData != null)
        {
            var products = JsonSerializer.Deserialize<List<Product>>(cachedData);
            return Ok(products);
        }
        
        var products = await _productService.GetAllAsync();
        
        var options = new DistributedCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
        
        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(products),
            options
        );
        
        return Ok(products);
    }
}
```

### 3. Response Caching

Caches entire HTTP responses at the server level.

**Advantages:**
- Extremely fast
- Minimal code changes
- Built into ASP.NET Core

**Disadvantages:**
- Coarse-grained
- Not suitable for user-specific data

**Implementation:**

```csharp
// Startup configuration
services.AddResponseCaching();

app.UseResponseCaching();

// Controller
[HttpGet]
[ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
public async Task<IActionResult> GetProducts()
{
    var products = await _productService.GetAllAsync();
    return Ok(products);
}
```

### 4. Output Caching

Similar to response caching but more flexible.

```csharp
// ASP.NET Core 7+
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(builder => builder.Expire(TimeSpan.FromMinutes(5)));
    options.AddPolicy("Products", builder => builder.Expire(TimeSpan.FromMinutes(10)));
});

app.UseOutputCache();

// Usage
[HttpGet]
[OutputCache(PolicyName = "Products")]
public async Task<IActionResult> GetProducts()
{
    var products = await _productService.GetAllAsync();
    return Ok(products);
}
```

### 5. CDN Caching

Best for static content and globally distributed apps.

**Use Cases:**
- Images, CSS, JavaScript
- Static HTML pages
- API responses (public data)

**Configuration:**

```csharp
// Add CDN headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("Cache-Control", "public, max-age=3600");
    await next();
});
```

**Azure CDN Setup:**

```bash
# Create CDN profile
az cdn profile create \
  --name myCdnProfile \
  --resource-group myResourceGroup \
  --sku Standard_Microsoft

# Create CDN endpoint
az cdn endpoint create \
  --name myEndpoint \
  --profile-name myCdnProfile \
  --resource-group myResourceGroup \
  --origin myapp.azurewebsites.net
```

## Cache Invalidation Strategies

### 1. Time-Based Expiration

**Absolute Expiration:**
```csharp
.SetAbsoluteExpiration(TimeSpan.FromMinutes(10))
```

**Sliding Expiration:**
```csharp
.SetSlidingExpiration(TimeSpan.FromMinutes(5))
```

### 2. Event-Based Invalidation

```csharp
public class ProductService
{
    private readonly IDistributedCache _cache;
    
    public async Task UpdateProductAsync(Product product)
    {
        await _repository.UpdateAsync(product);
        
        // Invalidate cache
        await _cache.RemoveAsync($"product:{product.Id}");
        await _cache.RemoveAsync("products:all");
    }
}
```

### 3. Tag-Based Invalidation

```csharp
// Using tags for related cache entries
var tags = new[] { "products", $"category:{product.CategoryId}" };
await _cache.SetAsync(key, value, tags);

// Invalidate all products
await _cache.InvalidateByTagAsync("products");
```

## Best Practices

### 1. Choose the Right Cache Type

- **In-Memory**: Single instance, fast access needed
- **Distributed**: Multiple instances, shared state
- **Response**: Public data, HTTP caching
- **CDN**: Static content, global distribution

### 2. Set Appropriate Expiration

```csharp
// Frequently changing data: Short TTL
.SetAbsoluteExpiration(TimeSpan.FromMinutes(1))

// Rarely changing data: Longer TTL
.SetAbsoluteExpiration(TimeSpan.FromHours(1))

// Static data: Very long TTL
.SetAbsoluteExpiration(TimeSpan.FromDays(1))
```

### 3. Handle Cache Misses Gracefully

```csharp
public async Task<List<Product>> GetProductsAsync()
{
    try
    {
        var cached = await _cache.GetStringAsync("products");
        if (cached != null)
            return JsonSerializer.Deserialize<List<Product>>(cached);
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Cache read failed, falling back to database");
    }
    
    // Always have fallback to source
    return await _repository.GetAllAsync();
}
```

### 4. Cache Keys Naming Convention

```csharp
// Good: Descriptive and hierarchical
"product:123"
"products:category:electronics"
"user:456:profile"

// Bad: Unclear or collision-prone
"p123"
"data"
"cache1"
```

### 5. Monitor Cache Performance

```csharp
var stopwatch = Stopwatch.StartNew();
var data = await _cache.GetAsync(key);
stopwatch.Stop();

_telemetry.TrackMetric("CacheAccessTime", stopwatch.ElapsedMilliseconds);
_telemetry.TrackMetric("CacheHitRate", data != null ? 1 : 0);
```

## Redis Cache Configuration

### Optimal Settings for App Service

```bash
# Premium tier for production
az redis create \
  --name myRedisCache \
  --resource-group myResourceGroup \
  --location eastus \
  --sku Premium \
  --vm-size P1 \
  --enable-non-ssl-port false
```

### Connection Resilience

```csharp
services.AddStackExchangeRedisCache(options =>
{
    options.ConfigurationOptions = new ConfigurationOptions
    {
        EndPoints = { Configuration["Redis:Endpoint"] },
        Password = Configuration["Redis:AccessKey"],
        AbortOnConnectFail = false,
        ConnectRetry = 3,
        ConnectTimeout = 5000,
        SyncTimeout = 5000
    };
});
```

## Performance Comparison

| Cache Type | Latency | Throughput | Shared | Cost |
|------------|---------|------------|--------|------|
| In-Memory | <1ms | Very High | No | Low |
| Redis | 1-5ms | High | Yes | Medium |
| SQL Server | 5-20ms | Medium | Yes | Low |
| CDN | Varies | Very High | Yes | Low-Medium |

## Common Patterns

### Cache-Aside Pattern

```csharp
public async Task<Product> GetProductAsync(int id)
{
    var cacheKey = $"product:{id}";
    
    // Try cache first
    var cached = await _cache.GetAsync(cacheKey);
    if (cached != null)
        return Deserialize<Product>(cached);
    
    // Load from source
    var product = await _repository.GetByIdAsync(id);
    
    // Update cache
    await _cache.SetAsync(cacheKey, Serialize(product), TimeSpan.FromMinutes(5));
    
    return product;
}
```

### Read-Through Pattern

```csharp
public class CachingProductRepository : IProductRepository
{
    private readonly IProductRepository _inner;
    private readonly IDistributedCache _cache;
    
    public async Task<Product> GetByIdAsync(int id)
    {
        return await _cache.GetOrCreateAsync(
            $"product:{id}",
            async () => await _inner.GetByIdAsync(id),
            TimeSpan.FromMinutes(5)
        );
    }
}
```

### Write-Through Pattern

```csharp
public async Task UpdateProductAsync(Product product)
{
    // Update source
    await _repository.UpdateAsync(product);
    
    // Update cache
    var cacheKey = $"product:{product.Id}";
    await _cache.SetAsync(cacheKey, Serialize(product), TimeSpan.FromMinutes(5));
}
```

## Troubleshooting

### High Cache Miss Rate
- Review expiration times
- Check cache key consistency
- Verify cache warming strategy

### Memory Issues
- Implement cache size limits
- Use LRU eviction policy
- Monitor memory usage

### Redis Connection Issues
- Check network security groups
- Verify connection string
- Monitor connection pool

## Lab Exercise

Practice implementing different caching strategies:

1. Start with no caching, measure baseline
2. Add in-memory caching, measure improvement
3. Switch to Redis, measure difference
4. Implement cache invalidation
5. Monitor cache hit rates

## Additional Resources

- [Response Caching in ASP.NET Core](https://docs.microsoft.com/aspnet/core/performance/caching/response)
- [Distributed Caching in ASP.NET Core](https://docs.microsoft.com/aspnet/core/performance/caching/distributed)
- [Azure Cache for Redis](https://docs.microsoft.com/azure/azure-cache-for-redis/)

---

[← Back to Module 3](../../README.md#module-3-performance-optimization)
