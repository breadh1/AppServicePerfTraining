# Lab 3: Implementing Caching

## Objective

Learn to implement and measure the performance impact of various caching strategies in Azure App Service.

## Prerequisites

- Completed Lab 1 (Monitoring setup)
- Azure subscription
- .NET 8.0 SDK or Node.js 18+
- Basic understanding of HTTP caching

## Duration

60 minutes

## Lab Overview

You will:
1. Deploy a sample application without caching
2. Measure baseline performance
3. Implement in-memory caching
4. Implement distributed caching with Redis
5. Configure response caching
6. Compare performance results

## Part 1: Setup Baseline Application (10 minutes)

### Deploy Sample Application

```bash
# Variables
RESOURCE_GROUP="perf-lab-rg"
LOCATION="eastus"
APP_NAME="perf-lab-app-$RANDOM"
PLAN_NAME="perf-lab-plan"

# Create resources
az group create --name $RESOURCE_GROUP --location $LOCATION

az appservice plan create \
  --name $PLAN_NAME \
  --resource-group $RESOURCE_GROUP \
  --sku S1 \
  --is-linux

az webapp create \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --plan $PLAN_NAME \
  --runtime "DOTNET|8.0"

# Enable Application Insights
az monitor app-insights component create \
  --app "${APP_NAME}-insights" \
  --location $LOCATION \
  --resource-group $RESOURCE_GROUP \
  --application-type web

INSTRUMENTATION_KEY=$(az monitor app-insights component show \
  --app "${APP_NAME}-insights" \
  --resource-group $RESOURCE_GROUP \
  --query instrumentationKey -o tsv)

az webapp config appsettings set \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings APPINSIGHTS_INSTRUMENTATIONKEY=$INSTRUMENTATION_KEY
```

### Create Sample Application

**Program.cs:**
```csharp
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private static readonly List<Product> Products = GenerateProducts(1000);
    
    [HttpGet]
    public async Task<ActionResult<List<Product>>> GetAll()
    {
        // Simulate database latency
        await Task.Delay(200);
        return Ok(Products);
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetById(int id)
    {
        await Task.Delay(100);
        var product = Products.FirstOrDefault(p => p.Id == id);
        return product == null ? NotFound() : Ok(product);
    }
    
    private static List<Product> GenerateProducts(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => new Product
            {
                Id = i,
                Name = $"Product {i}",
                Price = Random.Shared.Next(10, 1000),
                Category = $"Category {Random.Shared.Next(1, 10)}"
            })
            .ToList();
    }
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public string Category { get; set; } = "";
}
```

### Deploy Application

```bash
dotnet publish -c Release
cd bin/Release/net8.0/publish
zip -r ../deploy.zip .
az webapp deployment source config-zip \
  --resource-group $RESOURCE_GROUP \
  --name $APP_NAME \
  --src ../deploy.zip
```

## Part 2: Baseline Performance Testing (10 minutes)

### Install Load Testing Tool

```bash
# Using Apache Bench
sudo apt-get install apache2-utils

# Or using k6
curl https://github.com/grafana/k6/releases/download/v0.47.0/k6-v0.47.0-linux-amd64.tar.gz -L | tar xvz
```

### Run Baseline Test

```bash
# Test GET all products
ab -n 1000 -c 10 https://${APP_NAME}.azurewebsites.net/api/products

# Record results:
# - Requests per second
# - Mean response time
# - Percentiles (50%, 95%, 99%)
```

### Baseline Metrics to Record

Create a spreadsheet with:
```
Test Type: Baseline (No Caching)
Requests: 1000
Concurrency: 10
Requests/sec: __________
Mean response time: __________
P50: __________
P95: __________
P99: __________
```

## Part 3: Implement In-Memory Caching (15 minutes)

### Update Application

```csharp
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddMemoryCache();
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMemoryCache _cache;
    private static readonly List<Product> Products = GenerateProducts(1000);
    private const string CacheKey = "AllProducts";
    
    public ProductsController(IMemoryCache cache)
    {
        _cache = cache;
    }
    
    [HttpGet]
    public async Task<ActionResult<List<Product>>> GetAll()
    {
        if (_cache.TryGetValue(CacheKey, out List<Product>? cachedProducts))
        {
            return Ok(cachedProducts);
        }
        
        // Simulate database latency
        await Task.Delay(200);
        
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(5))
            .SetSlidingExpiration(TimeSpan.FromMinutes(2));
        
        _cache.Set(CacheKey, Products, cacheOptions);
        
        return Ok(Products);
    }
    
    // ... rest of the code
}
```

### Redeploy and Test

```bash
# Deploy updated code
dotnet publish -c Release
# ... deployment commands

# Test again
ab -n 1000 -c 10 https://${APP_NAME}.azurewebsites.net/api/products

# Record improved results
```

### Expected Improvement

- Response time should decrease by 150-180ms
- Requests/second should increase significantly
- P95 and P99 should improve dramatically

## Part 4: Implement Distributed Caching with Redis (15 minutes)

### Create Redis Cache

```bash
REDIS_NAME="perf-lab-redis-$RANDOM"

az redis create \
  --name $REDIS_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Basic \
  --vm-size c0

# Get connection string
REDIS_KEY=$(az redis list-keys \
  --name $REDIS_NAME \
  --resource-group $RESOURCE_GROUP \
  --query primaryKey -o tsv)

REDIS_HOST=$(az redis show \
  --name $REDIS_NAME \
  --resource-group $RESOURCE_GROUP \
  --query hostName -o tsv)

REDIS_CONNECTION="${REDIS_HOST}:6380,password=${REDIS_KEY},ssl=True,abortConnect=False"

# Configure app setting
az webapp config appsettings set \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings "Redis:ConnectionString=$REDIS_CONNECTION"
```

### Update Application for Redis

```csharp
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
    options.InstanceName = "PerfLab:";
});
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IDistributedCache _cache;
    private static readonly List<Product> Products = GenerateProducts(1000);
    private const string CacheKey = "AllProducts";
    
    public ProductsController(IDistributedCache cache)
    {
        _cache = cache;
    }
    
    [HttpGet]
    public async Task<ActionResult<List<Product>>> GetAll()
    {
        var cachedData = await _cache.GetStringAsync(CacheKey);
        if (cachedData != null)
        {
            var products = JsonSerializer.Deserialize<List<Product>>(cachedData);
            return Ok(products);
        }
        
        // Simulate database latency
        await Task.Delay(200);
        
        var options = new DistributedCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
        
        await _cache.SetStringAsync(
            CacheKey,
            JsonSerializer.Serialize(Products),
            options
        );
        
        return Ok(Products);
    }
}
```

### Test and Compare

```bash
# Deploy and test
ab -n 1000 -c 10 https://${APP_NAME}.azurewebsites.net/api/products
```

### Compare Results

| Metric | Baseline | In-Memory | Redis |
|--------|----------|-----------|-------|
| Req/sec | ___ | ___ | ___ |
| Mean (ms) | ___ | ___ | ___ |
| P95 (ms) | ___ | ___ | ___ |

## Part 5: Response Caching (10 minutes)

### Add Response Caching

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddResponseCaching();
builder.Services.AddControllers();

var app = builder.Build();
app.UseResponseCaching();
app.MapControllers();
app.Run();

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    [HttpGet]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, 
                   VaryByQueryKeys = new[] { "*" })]
    public async Task<ActionResult<List<Product>>> GetAll()
    {
        await Task.Delay(200);
        return Ok(Products);
    }
}
```

### Test Response Caching

```bash
# First request - should be slow
curl -w "@curl-format.txt" https://${APP_NAME}.azurewebsites.net/api/products

# Second request - should be fast (cached)
curl -w "@curl-format.txt" https://${APP_NAME}.azurewebsites.net/api/products
```

## Part 6: Analysis and Comparison (10 minutes)

### View in Application Insights

1. Navigate to Application Insights
2. Go to Performance blade
3. Compare operation duration for different implementations

### Query Performance Data

```kusto
requests
| where timestamp > ago(1h)
| where name contains "GetAll"
| summarize 
    avg(duration),
    percentiles(duration, 50, 95, 99),
    count()
    by tostring(customDimensions.CachingStrategy)
| order by avg_duration asc
```

### Calculate Improvements

```
In-Memory Cache:
Improvement = (Baseline_Time - InMemory_Time) / Baseline_Time * 100
Expected: 70-90% improvement

Distributed Cache:
Improvement = (Baseline_Time - Redis_Time) / Baseline_Time * 100
Expected: 60-80% improvement (slightly slower due to network)

Response Cache:
Improvement = (Baseline_Time - ResponseCache_Time) / Baseline_Time * 100
Expected: 95-99% improvement (HTTP-level caching)
```

## Verification Steps

- [ ] Baseline metrics recorded
- [ ] In-memory caching implemented and tested
- [ ] Redis caching implemented and tested
- [ ] Response caching implemented and tested
- [ ] Performance comparison completed
- [ ] Cache hit rates monitored
- [ ] Results documented

## Common Issues

### Issue: No performance improvement
- **Check**: Verify cache is actually being used
- **Check**: Ensure cache key is consistent
- **Solution**: Add logging to verify cache hits/misses

### Issue: Redis connection failures
- **Check**: Verify connection string
- **Check**: Check firewall rules
- **Solution**: Enable Azure services access in Redis firewall

### Issue: Stale data
- **Check**: Review cache expiration settings
- **Solution**: Implement cache invalidation on updates

## Best Practices Learned

1. **Choose the Right Cache Type**
   - In-memory: Single instance, fastest
   - Redis: Multiple instances, shared state
   - Response: HTTP-level, public data

2. **Set Appropriate TTL**
   - Frequently changing: Short TTL (minutes)
   - Rarely changing: Long TTL (hours)
   - Static: Very long TTL (days)

3. **Monitor Cache Effectiveness**
   - Track hit rate
   - Monitor cache size
   - Measure latency impact

4. **Handle Cache Failures**
   - Always have fallback to source
   - Log cache errors
   - Don't let cache failures break app

## Next Steps

- Experiment with different cache durations
- Implement cache warming strategies
- Add cache invalidation logic
- Explore CDN caching for static content
- Test cache behavior during scale events

## Additional Resources

- [ASP.NET Core Caching](https://docs.microsoft.com/aspnet/core/performance/caching/)
- [Azure Cache for Redis](https://docs.microsoft.com/azure/azure-cache-for-redis/)
- [Response Caching Middleware](https://docs.microsoft.com/aspnet/core/performance/caching/middleware)

---

[← Back to Labs](../../README.md#hands-on-labs) | [Next Lab: Auto-scaling →](../lab-4-autoscaling/README.md)
