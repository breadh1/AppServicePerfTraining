# Performance Metrics Overview

## Introduction

Effective performance monitoring requires understanding which metrics to track and how to interpret them. This guide covers the essential performance metrics for Azure App Service.

## Core Performance Metrics

### 1. Response Time Metrics

#### Average Response Time
- **Description**: Mean time to process requests
- **Target**: < 200ms for APIs, < 1s for pages
- **When to Alert**: > 2x baseline

#### Response Time Percentiles
- **P50 (Median)**: 50% of requests faster
- **P95**: 95% of requests faster (important for SLAs)
- **P99**: 99% of requests faster (outlier detection)
- **Target**: P95 < 500ms, P99 < 1s

### 2. Resource Utilization Metrics

#### CPU Percentage
- **Description**: CPU usage across all instances
- **Normal**: 30-70%
- **Warning**: 70-85%
- **Critical**: > 85%
- **Action**: Scale up or out when consistently high

#### Memory Percentage
- **Description**: Memory usage of the App Service
- **Normal**: < 70%
- **Warning**: 70-85%
- **Critical**: > 85%
- **Action**: Investigate leaks, optimize, or scale

#### Disk I/O
- **Metrics**: Read/write operations per second, throughput
- **Impact**: Affects file operations, logging
- **Action**: Optimize file access, consider external storage

### 3. Throughput Metrics

#### Requests per Second
- **Description**: Number of HTTP requests processed
- **Use**: Capacity planning, scaling decisions
- **Baseline**: Establish normal load patterns

#### Data In/Out
- **Description**: Network traffic in/out of app
- **Use**: Bandwidth monitoring, cost optimization
- **Action**: Compress responses, use CDN

### 4. Availability Metrics

#### HTTP Status Codes
- **2xx**: Success (target: > 99%)
- **4xx**: Client errors (investigate if high)
- **5xx**: Server errors (target: < 0.1%)
- **Action**: Alert on 5xx rate > 1%

#### Health Check Status
- **Description**: Application health probe results
- **Use**: Auto-scaling, load balancer decisions
- **Action**: Ensure health endpoints are responsive

### 5. Dependency Metrics

#### Dependency Duration
- **Description**: Time spent in external calls
- **Common Dependencies**: 
  - Database queries
  - External APIs
  - Cache operations
  - Queue operations
- **Target**: < 100ms for most operations

#### Dependency Success Rate
- **Description**: Percentage of successful dependency calls
- **Target**: > 99.9%
- **Action**: Implement retry logic, circuit breakers

## Application Insights Metrics

### Request Telemetry

```csharp
// Automatic request tracking
public void ConfigureServices(IServiceCollection services)
{
    services.AddApplicationInsightsTelemetry();
}
```

### Custom Metrics

```csharp
// Track custom business metrics
var telemetry = new TelemetryClient();

// Counter
telemetry.TrackMetric("OrdersProcessed", 1);

// Gauge
telemetry.TrackMetric("QueueLength", queueLength);

// Timing
using (var operation = telemetry.StartOperation<RequestTelemetry>("ProcessOrder"))
{
    // Your code
    operation.Telemetry.Success = true;
}
```

### Dependency Tracking

```csharp
// Track dependency calls
var dependency = new DependencyTelemetry
{
    Name = "GET /api/products",
    Target = "api.example.com",
    Data = "https://api.example.com/api/products",
    Timestamp = start,
    Duration = duration,
    Success = success,
    ResultCode = statusCode
};

telemetryClient.TrackDependency(dependency);
```

## Metric Collection and Analysis

### Azure Monitor Metrics

```bash
# Query CPU metrics
az monitor metrics list \
  --resource /subscriptions/{sub-id}/resourceGroups/{rg}/providers/Microsoft.Web/sites/{app-name} \
  --metric CpuPercentage \
  --interval PT1M \
  --start-time 2024-01-01T00:00:00Z \
  --end-time 2024-01-01T23:59:59Z
```

### Log Analytics Queries

```kusto
// Request performance
requests
| where timestamp > ago(1d)
| summarize 
    RequestCount = count(),
    AvgDuration = avg(duration),
    P50 = percentile(duration, 50),
    P95 = percentile(duration, 95),
    P99 = percentile(duration, 99)
    by bin(timestamp, 1h), name
| order by timestamp desc

// Failed requests
requests
| where success == false
| summarize FailureCount = count() by resultCode, name
| order by FailureCount desc

// Slow dependencies
dependencies
| where duration > 1000 // > 1 second
| summarize 
    SlowCallCount = count(),
    AvgDuration = avg(duration)
    by name, target
| order by SlowCallCount desc

// Error rate
requests
| summarize 
    Total = count(),
    Failures = countif(success == false)
    by bin(timestamp, 5m)
| extend ErrorRate = (Failures * 100.0) / Total
| where ErrorRate > 1
```

## Setting Up Alerts

### Metric Alerts

```bash
# Create CPU alert
az monitor metrics alert create \
  --name "High CPU Alert" \
  --resource-group myResourceGroup \
  --scopes /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Web/sites/{app} \
  --condition "avg CpuPercentage > 85" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --action-group myActionGroup
```

### Log Alerts

```bash
# Create error rate alert
az monitor scheduled-query rule create \
  --name "High Error Rate" \
  --resource-group myResourceGroup \
  --location eastus \
  --condition "count() > 10" \
  --query "requests | where success == false | where timestamp > ago(5m)" \
  --evaluation-frequency 5m \
  --window-size 5m
```

## Dashboard Creation

### Key Metrics Dashboard

Essential metrics to display:

1. **Overview Tile**
   - Request rate
   - Average response time
   - Error rate
   - Availability %

2. **Resource Usage**
   - CPU percentage
   - Memory percentage
   - Disk I/O

3. **Performance Breakdown**
   - Response time percentiles
   - Slowest endpoints
   - Dependency duration

4. **Error Analysis**
   - Status code distribution
   - Failed dependencies
   - Exception count

### PowerShell Dashboard Creation

```powershell
# Create dashboard
$dashboard = @{
    properties = @{
        lenses = @{
            "0" = @{
                order = 0
                parts = @{
                    "0" = @{
                        position = @{ x = 0; y = 0; colSpan = 6; rowSpan = 4 }
                        metadata = @{
                            type = "Extension/Microsoft_Azure_Monitoring/PartType/MetricsChartPart"
                            settings = @{
                                content = @{
                                    metrics = @(@{
                                        resourceId = "/subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Web/sites/{app}"
                                        name = "CpuPercentage"
                                    })
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
```

## Performance Baselines

### Establishing Baselines

1. **Collect Data**: Monitor for 1-2 weeks
2. **Identify Patterns**: Daily, weekly cycles
3. **Calculate Statistics**: Mean, median, percentiles
4. **Set Thresholds**: Based on percentiles (e.g., alert at P95 + 50%)

### Example Baseline

```
Normal Load (Business Hours):
- Requests/sec: 50-200
- Avg Response Time: 150ms
- P95 Response Time: 400ms
- CPU: 40-60%
- Memory: 50-65%
- Error Rate: < 0.1%

Peak Load:
- Requests/sec: 300-500
- Avg Response Time: 250ms
- P95 Response Time: 600ms
- CPU: 70-85%
- Memory: 65-80%
- Error Rate: < 0.5%
```

## Performance Testing

### Load Testing Metrics

```bash
# Azure Load Testing
az load test create \
  --name "performance-test" \
  --resource-group myResourceGroup \
  --test-plan-file loadtest.yaml
```

### Key Test Scenarios

1. **Baseline Test**: Normal load for extended period
2. **Spike Test**: Sudden traffic increase
3. **Stress Test**: Gradually increasing load
4. **Soak Test**: Sustained high load
5. **Scalability Test**: Performance across scale levels

## Best Practices

### 1. Metric Selection
- ✅ Track what matters for your SLAs
- ✅ Monitor user-impacting metrics
- ✅ Include business metrics
- ❌ Don't track everything (signal vs noise)

### 2. Alert Configuration
- ✅ Alert on trends, not spikes
- ✅ Use appropriate time windows
- ✅ Set actionable thresholds
- ❌ Avoid alert fatigue

### 3. Data Retention
- Real-time: 1 hour (1-minute granularity)
- Short-term: 30 days (5-minute granularity)
- Long-term: 1 year+ (Application Insights)

### 4. Cost Optimization
- Sample high-volume telemetry
- Use appropriate retention periods
- Archive to storage for long-term analysis

## Common Antipatterns

### ❌ Monitoring Too Much
- Results in noise and increased costs
- Focus on actionable metrics

### ❌ No Baselines
- Can't detect anomalies without baseline
- Always establish normal patterns

### ❌ Ignoring Context
- Metrics without context are meaningless
- Consider time of day, day of week

### ❌ Alert Fatigue
- Too many alerts leads to ignoring them
- Be selective and actionable

## Practical Exercises

### Exercise 1: Setup Monitoring
1. Enable Application Insights
2. Configure custom metrics
3. Create dashboard
4. Set up alerts

### Exercise 2: Baseline Analysis
1. Collect metrics for 1 week
2. Identify patterns
3. Calculate percentiles
4. Set thresholds

### Exercise 3: Performance Investigation
1. Identify slow requests
2. Analyze dependency calls
3. Find bottlenecks
4. Implement improvements

## Checklist

- [ ] Application Insights enabled
- [ ] Key metrics being tracked
- [ ] Baselines established
- [ ] Alerts configured
- [ ] Dashboard created
- [ ] Regular review process
- [ ] Performance testing in place
- [ ] Runbook for common issues

## Additional Resources

- [Azure Monitor Metrics](https://docs.microsoft.com/azure/azure-monitor/essentials/data-platform-metrics)
- [Application Insights Telemetry](https://docs.microsoft.com/azure/azure-monitor/app/data-model)
- [Kusto Query Language](https://docs.microsoft.com/azure/data-explorer/kusto/query/)
- [Performance Testing](https://docs.microsoft.com/azure/load-testing/overview-what-is-azure-load-testing)

---

[← Previous: Request Pipeline](./request-pipeline.md) | [Back to Modules](../../README.md#training-modules)
