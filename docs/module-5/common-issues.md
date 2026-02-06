# Common Performance Issues in Azure App Service

## Overview

This guide covers the most common performance issues encountered in Azure App Service and how to diagnose and resolve them.

## Issue 1: High CPU Usage

### Symptoms
- Slow response times
- Request timeouts
- CPU percentage consistently > 80%
- App may become unresponsive

### Common Causes

#### 1. Inefficient Code
```csharp
// ❌ Bad: CPU-intensive synchronous loop
public IActionResult ProcessData()
{
    var results = new List<Result>();
    for (int i = 0; i < 1000000; i++)
    {
        results.Add(ExpensiveOperation(i)); // Blocking
    }
    return Ok(results);
}

// ✅ Good: Optimized async processing
public async Task<IActionResult> ProcessData()
{
    var tasks = Enumerable.Range(0, 1000000)
        .Select(i => ProcessItemAsync(i));
    var results = await Task.WhenAll(tasks);
    return Ok(results);
}
```

#### 2. Infinite Loops or Recursion
```csharp
// ❌ Bad: No exit condition
public void ProcessQueue()
{
    while (true) // Will consume 100% CPU
    {
        var item = GetNextItem();
        Process(item);
    }
}

// ✅ Good: Proper loop control
public async Task ProcessQueue(CancellationToken cancellationToken)
{
    while (!cancellationToken.IsCancellationRequested)
    {
        var item = await GetNextItemAsync();
        if (item == null)
        {
            await Task.Delay(100, cancellationToken); // Prevent busy-wait
            continue;
        }
        await ProcessAsync(item);
    }
}
```

### Diagnosis

#### Using Azure Portal
1. Navigate to App Service → Metrics
2. View CPU Percentage metric
3. Check App Service Plan metrics
4. Review Application Insights for slow operations

#### Using Kusto Query
```kusto
performanceCounters
| where name == "% Processor Time"
| where timestamp > ago(1h)
| summarize avg(value) by bin(timestamp, 1m)
| render timechart
```

#### Using Diagnostic Tools
```bash
# Install profiling session
az webapp log config \
  --resource-group myResourceGroup \
  --name myWebApp \
  --application-logging filesystem \
  --level verbose

# Get process information
az webapp log show \
  --resource-group myResourceGroup \
  --name myWebApp
```

### Solutions

1. **Profile Your Application**
   - Use Application Insights Profiler
   - Identify hot code paths
   - Optimize algorithms

2. **Implement Async Patterns**
```csharp
// Use async for I/O operations
public async Task<ActionResult> GetData()
{
    var data = await _repository.GetDataAsync();
    return Ok(data);
}
```

3. **Optimize Loops and Queries**
```csharp
// Use LINQ for better performance
var filtered = items
    .Where(i => i.IsActive)
    .Select(i => i.Id)
    .ToList();
```

4. **Scale Up or Out**
```bash
# Scale up to more powerful tier
az appservice plan update \
  --name myPlan \
  --resource-group myResourceGroup \
  --sku P2v3

# Or scale out
az appservice plan update \
  --name myPlan \
  --resource-group myResourceGroup \
  --number-of-workers 3
```

## Issue 2: High Memory Usage

### Symptoms
- Application restarts frequently
- OutOfMemoryException
- Memory percentage > 85%
- Slow garbage collection

### Common Causes

#### 1. Memory Leaks
```csharp
// ❌ Bad: Event handler not unsubscribed
public class BadService
{
    private EventHandler _handler;
    
    public BadService()
    {
        SomeStaticEvent += HandleEvent; // Never unsubscribed!
    }
    
    private void HandleEvent(object sender, EventArgs e) { }
}

// ✅ Good: Proper cleanup
public class GoodService : IDisposable
{
    public GoodService()
    {
        SomeStaticEvent += HandleEvent;
    }
    
    public void Dispose()
    {
        SomeStaticEvent -= HandleEvent;
    }
    
    private void HandleEvent(object sender, EventArgs e) { }
}
```

#### 2. Large Object Retention
```csharp
// ❌ Bad: Keeping large objects in memory
private static Dictionary<int, byte[]> _cache = new();

public void CacheData(int id, byte[] data)
{
    _cache[id] = data; // Unbounded growth!
}

// ✅ Good: Use memory cache with limits
private readonly IMemoryCache _cache;

public void CacheData(int id, byte[] data)
{
    var options = new MemoryCacheEntryOptions()
        .SetSize(data.Length)
        .SetSlidingExpiration(TimeSpan.FromMinutes(5));
    
    _cache.Set(id, data, options);
}
```

#### 3. Excessive Caching
```csharp
// ✅ Configure cache size limits
services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024 * 1024 * 100; // 100MB limit
});
```

### Diagnosis

#### Memory Profiling
```kusto
// Memory usage over time
performanceCounters
| where name == "Available Bytes"
| where timestamp > ago(1h)
| summarize avg(value) by bin(timestamp, 1m)
| render timechart
```

#### Find Memory Leaks
```csharp
// Use Memory Profiler in Visual Studio or
// dotMemory for production analysis
```

### Solutions

1. **Fix Memory Leaks**
   - Unsubscribe from events
   - Dispose of objects properly
   - Use weak references when appropriate

2. **Implement Proper Cache Limits**
```csharp
services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024;
    options.CompactionPercentage = 0.25;
    options.ExpirationScanFrequency = TimeSpan.FromMinutes(5);
});
```

3. **Use Streaming for Large Data**
```csharp
// Instead of loading entire file
public async Task<FileResult> DownloadLargeFile()
{
    var stream = await _storage.OpenReadAsync("largefile.zip");
    return File(stream, "application/zip", "largefile.zip");
}
```

4. **Scale Up for More Memory**
```bash
az appservice plan update \
  --name myPlan \
  --resource-group myResourceGroup \
  --sku P2v3 # More memory
```

## Issue 3: Slow Response Times

### Symptoms
- High request duration
- Timeouts
- Poor user experience
- P95 latency > 2 seconds

### Common Causes

#### 1. Slow Database Queries
```csharp
// ❌ Bad: N+1 query problem
public async Task<List<OrderDTO>> GetOrders()
{
    var orders = await _context.Orders.ToListAsync();
    foreach (var order in orders)
    {
        order.Customer = await _context.Customers
            .FindAsync(order.CustomerId); // N queries!
    }
    return orders;
}

// ✅ Good: Eager loading
public async Task<List<OrderDTO>> GetOrders()
{
    return await _context.Orders
        .Include(o => o.Customer) // Single query
        .ToListAsync();
}
```

#### 2. Synchronous I/O
```csharp
// ❌ Bad: Blocking calls
public ActionResult GetData()
{
    var data = _httpClient.GetStringAsync("https://api.example.com/data")
        .Result; // Blocks thread!
    return Ok(data);
}

// ✅ Good: Async all the way
public async Task<ActionResult> GetData()
{
    var data = await _httpClient.GetStringAsync("https://api.example.com/data");
    return Ok(data);
}
```

#### 3. No Caching
```csharp
// ✅ Implement caching
[ResponseCache(Duration = 60)]
public async Task<ActionResult> GetProducts()
{
    var products = await _cache.GetOrCreateAsync("products", async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
        return await _repository.GetAllAsync();
    });
    return Ok(products);
}
```

### Diagnosis

#### Application Insights
```kusto
// Slowest operations
requests
| where timestamp > ago(1h)
| summarize avg(duration), count() by name
| order by avg_duration desc
| take 10

// Slow dependencies
dependencies
| where timestamp > ago(1h)
| where duration > 1000
| summarize count() by name, target
| order by count_ desc
```

### Solutions

1. **Optimize Database Access**
   - Use indexes
   - Implement eager loading
   - Use pagination
   - Cache frequently accessed data

2. **Implement Caching**
```csharp
services.AddResponseCaching();
services.AddMemoryCache();
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = Configuration["Redis:ConnectionString"];
});
```

3. **Use Async/Await Properly**
```csharp
// All I/O operations should be async
public async Task<ActionResult> ComplexOperation()
{
    var task1 = _service1.GetDataAsync();
    var task2 = _service2.GetDataAsync();
    var task3 = _service3.GetDataAsync();
    
    await Task.WhenAll(task1, task2, task3);
    
    return Ok(new { task1.Result, task2.Result, task3.Result });
}
```

4. **Enable Compression**
```csharp
services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
});
```

## Issue 4: Cold Start Problems

### Symptoms
- First request after idle is very slow (10+ seconds)
- Intermittent slow requests
- Timeouts on first request

### Common Causes
- App initialization takes too long
- Always On is disabled
- Large deployment package
- Expensive startup code

### Solutions

1. **Enable Always On**
```bash
az webapp config set \
  --resource-group myResourceGroup \
  --name myWebApp \
  --always-on true
```

2. **Optimize Startup**
```csharp
// Lazy load expensive resources
private readonly Lazy<ExpensiveResource> _resource = 
    new Lazy<ExpensiveResource>(() => new ExpensiveResource());

public void UseResource()
{
    var resource = _resource.Value; // Only initialized when first used
}
```

3. **Implement Warmup**
```csharp
// Warmup endpoint
[HttpGet("warmup")]
public IActionResult Warmup()
{
    // Initialize critical resources
    _cache.WarmUp();
    _dbContext.Database.OpenConnection();
    return Ok("Warmed up");
}
```

## Issue 5: Thread Starvation

### Symptoms
- Increasing response times under load
- ThreadPool exhaustion
- Application hangs

### Common Causes
```csharp
// ❌ Bad: Blocking async code
public ActionResult BadExample()
{
    var result = _service.GetDataAsync().Result; // Blocks thread!
    return Ok(result);
}

// ✅ Good: Async all the way
public async Task<ActionResult> GoodExample()
{
    var result = await _service.GetDataAsync();
    return Ok(result);
}
```

### Solutions

1. **Use Async/Await Properly**
2. **Increase ThreadPool Size (if necessary)**
```csharp
ThreadPool.SetMinThreads(100, 100);
```

3. **Monitor Thread Usage**
```kusto
performanceCounters
| where name == "Thread Count"
| summarize avg(value) by bin(timestamp, 1m)
| render timechart
```

## Issue 6: External Dependency Failures

### Symptoms
- Cascading failures
- Timeouts
- High error rates

### Solutions

1. **Implement Retry Logic**
```csharp
services.AddHttpClient("api")
    .AddTransientHttpErrorPolicy(policy => 
        policy.WaitAndRetryAsync(3, retryAttempt => 
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
```

2. **Use Circuit Breaker**
```csharp
services.AddHttpClient("api")
    .AddTransientHttpErrorPolicy(policy =>
        policy.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));
```

3. **Set Timeouts**
```csharp
var client = new HttpClient
{
    Timeout = TimeSpan.FromSeconds(10)
};
```

4. **Implement Fallbacks**
```csharp
public async Task<Data> GetDataWithFallback()
{
    try
    {
        return await _apiClient.GetDataAsync();
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "API call failed, using cached data");
        return await _cache.GetAsync<Data>("fallback-data");
    }
}
```

## Troubleshooting Checklist

- [ ] Check Application Insights for slow operations
- [ ] Review CPU and memory metrics
- [ ] Analyze dependency duration
- [ ] Check for memory leaks
- [ ] Verify async/await usage
- [ ] Review database query performance
- [ ] Check caching implementation
- [ ] Verify Always On is enabled
- [ ] Review error logs
- [ ] Test under load

## Diagnostic Tools

1. **Application Insights Profiler**
2. **App Service Diagnostics**
3. **Kudu Console** (https://yourapp.scm.azurewebsites.net)
4. **Visual Studio Profiler**
5. **dotMemory / dotTrace**

## Additional Resources

- [App Service Diagnostics](https://docs.microsoft.com/azure/app-service/overview-diagnostics)
- [Performance Troubleshooting](https://docs.microsoft.com/azure/app-service/troubleshoot-performance-degradation)
- [Application Insights](https://docs.microsoft.com/azure/azure-monitor/app/app-insights-overview)

---

[← Back to Module 5](../../README.md#module-5-troubleshooting)
