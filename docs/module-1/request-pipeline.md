# Request Processing Pipeline

## Overview

Understanding how Azure App Service processes HTTP requests is essential for optimizing performance and troubleshooting issues.

## Request Flow Architecture

```
Internet → Azure Load Balancer → Front End → App Service Instance → Your Application
```

### Detailed Request Flow

1. **Client Request** → HTTP/HTTPS request from user
2. **Azure Load Balancer** → Distributes traffic across instances
3. **Front End (FE)** → Handles SSL termination, routing
4. **App Service Instance** → Processes request on worker VM
5. **Application Code** → Your application logic
6. **Response** → Flows back through same path

## Components

### 1. Azure Load Balancer
- **Function**: Traffic distribution
- **Features**: 
  - Round-robin load balancing
  - Health checks
  - Sticky sessions (ARR affinity)
- **Performance Impact**: Minimal latency (< 5ms)

### 2. Front End (FE)
- **Function**: SSL/TLS termination, request routing
- **Features**:
  - SSL certificate management
  - Request inspection
  - Rate limiting
  - DDoS protection
- **Performance Impact**: SSL adds 5-20ms

### 3. Worker Instance
- **Function**: Runs your application
- **Components**:
  - IIS (Windows) or HTTP server (Linux)
  - Application runtime (.NET, Node.js, Python, etc.)
  - Your application code

## Request Processing Stages

### Stage 1: Connection Establishment
```
Time: 0-50ms
- DNS lookup
- TCP handshake
- SSL/TLS handshake (if HTTPS)
```

**Optimization**:
- Use connection pooling
- Enable HTTP/2
- Leverage keep-alive connections

### Stage 2: Request Routing
```
Time: 5-20ms
- Load balancer routing
- Front end processing
- Instance selection
```

**Optimization**:
- Use regional deployments
- Enable ARR affinity if needed
- Minimize cross-region traffic

### Stage 3: Application Processing
```
Time: Variable (10ms - seconds)
- Application code execution
- Database queries
- External API calls
- Business logic
```

**Optimization**:
- Optimize code efficiency
- Implement caching
- Use async patterns
- Minimize external dependencies

### Stage 4: Response Delivery
```
Time: 5-100ms
- Response serialization
- Compression
- Network transfer
```

**Optimization**:
- Enable response compression
- Minimize response payload
- Use CDN for static content

## Performance Metrics

### Key Metrics to Monitor

1. **Request Duration**
   - End-to-end time for request processing
   - Target: < 200ms for API, < 1s for web pages

2. **Server Response Time**
   - Time spent in application code
   - Target: < 100ms

3. **Dependency Duration**
   - Time for external calls (DB, APIs)
   - Target: Minimize and parallelize

4. **Connection Time**
   - Time to establish connection
   - Target: < 50ms

### Using Application Insights

```csharp
// Track custom metrics
var telemetry = new TelemetryClient();
var stopwatch = Stopwatch.StartNew();

// Your code here
await ProcessRequest();

stopwatch.Stop();
telemetry.TrackMetric("RequestProcessingTime", stopwatch.ElapsedMilliseconds);
```

## Common Bottlenecks

### 1. Cold Start
- **Issue**: First request after idle period is slow
- **Cause**: App needs to initialize
- **Solution**: 
  - Enable "Always On" setting
  - Use warm-up requests
  - Implement lightweight initialization

### 2. Slow Dependencies
- **Issue**: External calls take too long
- **Cause**: Slow database, API, or network
- **Solution**:
  - Optimize queries
  - Implement caching
  - Use async patterns
  - Set appropriate timeouts

### 3. Thread Starvation
- **Issue**: App runs out of available threads
- **Cause**: Synchronous I/O, blocking operations
- **Solution**:
  - Use async/await
  - Increase thread pool if needed
  - Avoid blocking operations

### 4. Memory Pressure
- **Issue**: High memory usage causes slowdowns
- **Cause**: Memory leaks, large objects, excessive caching
- **Solution**:
  - Fix memory leaks
  - Implement proper disposal
  - Optimize cache size
  - Scale up if needed

## Request Pipeline Configuration

### IIS Configuration (Windows)

```xml
<!-- web.config -->
<system.webServer>
  <httpProtocol>
    <customHeaders>
      <add name="X-Response-Time" value="true" />
    </customHeaders>
  </httpProtocol>
  
  <staticContent>
    <clientCache cacheControlMode="UseMaxAge" cacheControlMaxAge="7.00:00:00" />
  </staticContent>
  
  <urlCompression doStaticCompression="true" doDynamicCompression="true" />
</system.webServer>
```

### Application Configuration

```csharp
// ASP.NET Core
public void Configure(IApplicationBuilder app)
{
    // Response compression
    app.UseResponseCompression();
    
    // Request logging
    app.Use(async (context, next) =>
    {
        var watch = Stopwatch.StartNew();
        await next();
        watch.Stop();
        context.Response.Headers.Add("X-Response-Time", 
            $"{watch.ElapsedMilliseconds}ms");
    });
    
    // Your middleware
    app.UseRouting();
    app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
}
```

## Advanced Topics

### HTTP/2 Support
- Multiplexing: Multiple requests over single connection
- Header compression
- Server push
- Automatic in App Service

### WebSockets
- Long-lived connections
- Real-time communication
- Different resource considerations

### Connection Limits
- Default: 5,000 concurrent connections per instance
- Can be increased with support ticket
- Consider scaling out instead

## Monitoring Request Pipeline

### Using Azure Portal
1. Navigate to your App Service
2. Select "Diagnose and solve problems"
3. Choose "Performance and Availability"
4. View request metrics and latency breakdown

### Using Application Insights

```csharp
// Track request telemetry
var requestTelemetry = new RequestTelemetry
{
    Name = "GET /api/products",
    Timestamp = DateTimeOffset.UtcNow,
    Duration = TimeSpan.FromMilliseconds(123),
    ResponseCode = "200",
    Success = true
};

telemetryClient.TrackRequest(requestTelemetry);
```

### Log Analysis Query

```kusto
requests
| where timestamp > ago(1h)
| summarize 
    count(),
    avg(duration),
    percentiles(duration, 50, 95, 99)
    by name
| order by avg_duration desc
```

## Best Practices

1. **Minimize Request Processing Time**
   - Keep application logic efficient
   - Use caching where appropriate
   - Implement async patterns

2. **Optimize Connection Handling**
   - Use connection pooling
   - Enable keep-alive
   - Implement proper timeout settings

3. **Monitor Key Metrics**
   - Track request duration
   - Monitor dependency calls
   - Set up alerts for anomalies

4. **Handle Errors Gracefully**
   - Implement retry logic
   - Use circuit breakers
   - Provide meaningful error responses

5. **Enable Compression**
   - Compress responses
   - Use appropriate compression levels
   - Consider trade-offs with CPU

## Lab Exercise

### Exercise: Request Pipeline Analysis

1. **Deploy Sample Application**
   ```bash
   az webapp create --name myapp --resource-group mygroup --plan myplan
   ```

2. **Enable Application Insights**
   ```bash
   az monitor app-insights component create --app myapp --location eastus --resource-group mygroup
   ```

3. **Generate Load**
   - Use load testing tool
   - Monitor metrics in real-time

4. **Analyze Results**
   - View request duration breakdown
   - Identify bottlenecks
   - Optimize based on findings

## Troubleshooting Guide

### Symptom: High Latency
- Check: Network latency, dependency duration
- Action: Optimize slow dependencies, enable caching

### Symptom: Intermittent Slow Requests
- Check: Cold starts, auto-scaling events
- Action: Enable Always On, warm-up requests

### Symptom: Timeout Errors
- Check: Long-running operations, blocking code
- Action: Implement async, optimize operations

## Additional Resources

- [App Service Diagnostics](https://docs.microsoft.com/azure/app-service/overview-diagnostics)
- [Application Insights Request Tracking](https://docs.microsoft.com/azure/azure-monitor/app/api-custom-events-metrics)
- [HTTP/2 Support](https://docs.microsoft.com/azure/app-service/overview-http2)

---

[← Previous: Compute Resources](./compute-resources.md) | [Next: Performance Metrics →](./performance-metrics.md)
