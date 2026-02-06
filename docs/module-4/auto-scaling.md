# Auto-scaling Configuration

## Overview

Auto-scaling automatically adjusts the number of instances running your app based on demand, optimizing performance and cost.

## Auto-scaling Basics

### When to Use Auto-scaling

✅ **Use auto-scaling when:**
- Traffic patterns are predictable
- Traffic varies significantly by time
- Cost optimization is important
- You need automatic response to load

❌ **Don't use auto-scaling when:**
- Traffic is consistently high (just scale manually)
- App has state issues across instances
- Scale operations are too slow for traffic spikes
- Budget is extremely constrained

## Auto-scaling Rules

### Rule Types

#### 1. Metric-Based Rules

Scale based on resource metrics:

**CPU Percentage:**
```bash
az monitor autoscale rule create \
  --resource-group myResourceGroup \
  --autoscale-name myAutoscale \
  --condition "CpuPercentage > 70 avg 5m" \
  --scale out 1
```

**Memory Percentage:**
```bash
az monitor autoscale rule create \
  --resource-group myResourceGroup \
  --autoscale-name myAutoscale \
  --condition "MemoryPercentage > 80 avg 5m" \
  --scale out 1
```

**Request Count:**
```bash
az monitor autoscale rule create \
  --resource-group myResourceGroup \
  --autoscale-name myAutoscale \
  --condition "Requests > 1000 total 5m" \
  --scale out 1
```

#### 2. Schedule-Based Rules

Scale based on time patterns:

```bash
# Scale up during business hours
az monitor autoscale rule create \
  --resource-group myResourceGroup \
  --autoscale-name myAutoscale \
  --scale to 5 \
  --start-time "2024-01-01T09:00:00Z" \
  --end-time "2024-01-01T18:00:00Z" \
  --recurrence frequency week days Monday Tuesday Wednesday Thursday Friday
```

#### 3. Application Insights Metrics

Scale based on application-specific metrics:

```bash
az monitor autoscale rule create \
  --resource-group myResourceGroup \
  --autoscale-name myAutoscale \
  --condition "ResponseTime > 2000 avg 5m" \
  --scale out 1
```

## Configuration Best Practices

### 1. Set Appropriate Thresholds

**Scale Out (Add Instances):**
```yaml
Metric: CPU Percentage
Threshold: > 70%
Duration: 5 minutes
Action: Add 1 instance
Cooldown: 5 minutes
```

**Scale In (Remove Instances):**
```yaml
Metric: CPU Percentage
Threshold: < 30%
Duration: 10 minutes
Action: Remove 1 instance
Cooldown: 10 minutes
```

### 2. Configure Instance Limits

```bash
az monitor autoscale create \
  --resource-group myResourceGroup \
  --name myAutoscale \
  --resource /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Web/serverFarms/{plan} \
  --min-count 2 \
  --max-count 10 \
  --count 3
```

**Recommendations:**
- **Minimum**: At least 2 for high availability
- **Maximum**: Based on budget and expected peak load
- **Default**: Optimal for normal load

### 3. Cooldown Periods

Prevent flapping (rapid scale in/out):

```yaml
Scale Out Cooldown: 5 minutes
Scale In Cooldown: 10-15 minutes
```

**Why longer cooldown for scale-in:**
- Allows new instance to stabilize
- Prevents premature scale-in
- Accounts for metric lag

### 4. Multiple Rules

Combine rules for comprehensive scaling:

```bash
# Rule 1: CPU-based
az monitor autoscale rule create \
  --autoscale-name myAutoscale \
  --condition "CpuPercentage > 70 avg 5m" \
  --scale out 1

# Rule 2: Memory-based
az monitor autoscale rule create \
  --autoscale-name myAutoscale \
  --condition "MemoryPercentage > 75 avg 5m" \
  --scale out 1

# Rule 3: Request-based
az monitor autoscale rule create \
  --autoscale-name myAutoscale \
  --condition "Requests > 10000 total 5m" \
  --scale out 2
```

## ARM Template Configuration

Complete auto-scaling configuration:

```json
{
  "type": "Microsoft.Insights/autoscalesettings",
  "apiVersion": "2015-04-01",
  "name": "autoscaleSettings",
  "location": "[resourceGroup().location]",
  "properties": {
    "profiles": [
      {
        "name": "DefaultProfile",
        "capacity": {
          "minimum": "2",
          "maximum": "10",
          "default": "3"
        },
        "rules": [
          {
            "metricTrigger": {
              "metricName": "CpuPercentage",
              "metricResourceUri": "[resourceId('Microsoft.Web/serverfarms', variables('appServicePlanName'))]",
              "timeGrain": "PT1M",
              "statistic": "Average",
              "timeWindow": "PT5M",
              "timeAggregation": "Average",
              "operator": "GreaterThan",
              "threshold": 70
            },
            "scaleAction": {
              "direction": "Increase",
              "type": "ChangeCount",
              "value": "1",
              "cooldown": "PT5M"
            }
          },
          {
            "metricTrigger": {
              "metricName": "CpuPercentage",
              "metricResourceUri": "[resourceId('Microsoft.Web/serverfarms', variables('appServicePlanName'))]",
              "timeGrain": "PT1M",
              "statistic": "Average",
              "timeWindow": "PT10M",
              "timeAggregation": "Average",
              "operator": "LessThan",
              "threshold": 30
            },
            "scaleAction": {
              "direction": "Decrease",
              "type": "ChangeCount",
              "value": "1",
              "cooldown": "PT10M"
            }
          }
        ]
      },
      {
        "name": "BusinessHoursProfile",
        "capacity": {
          "minimum": "3",
          "maximum": "15",
          "default": "5"
        },
        "rules": [],
        "recurrence": {
          "frequency": "Week",
          "schedule": {
            "timeZone": "Pacific Standard Time",
            "days": ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"],
            "hours": [9],
            "minutes": [0]
          }
        }
      }
    ],
    "enabled": true,
    "targetResourceUri": "[resourceId('Microsoft.Web/serverfarms', variables('appServicePlanName'))]"
  }
}
```

## Monitoring Auto-scaling

### View Scale Operations

```bash
# Get auto-scale history
az monitor autoscale show \
  --resource-group myResourceGroup \
  --name myAutoscale

# Get activity logs
az monitor activity-log list \
  --resource-group myResourceGroup \
  --start-time 2024-01-01T00:00:00Z \
  --query "[?contains(operationName.value, 'autoscale')]"
```

### Key Metrics to Monitor

1. **Instance Count**: Current number of instances
2. **Scale Events**: Frequency of scale operations
3. **Metric Values**: Actual CPU, memory, request values
4. **Scale Duration**: Time taken for scale operations

### Log Analytics Query

```kusto
// Auto-scale events
AzureActivity
| where CategoryValue == "Autoscale"
| project TimeGenerated, OperationName, Level, ResultType
| order by TimeGenerated desc

// Performance during scale events
let scaleEvents = AzureActivity
| where CategoryValue == "Autoscale"
| project ScaleTime = TimeGenerated, OperationName;
requests
| join kind=inner scaleEvents on $left.timestamp == $right.ScaleTime
| summarize avg(duration), count() by bin(timestamp, 5m)
```

## Application Considerations

### Stateless Design

Apps must be stateless for effective auto-scaling:

```csharp
// ❌ Bad: Instance-specific state
public class BadController : Controller
{
    private static List<Order> _orders = new List<Order>(); // Lost on scale
    
    [HttpPost]
    public IActionResult AddOrder(Order order)
    {
        _orders.Add(order); // Only on this instance!
        return Ok();
    }
}

// ✅ Good: Shared state
public class GoodController : Controller
{
    private readonly IDistributedCache _cache;
    
    [HttpPost]
    public async Task<IActionResult> AddOrder(Order order)
    {
        await _cache.SetStringAsync($"order:{order.Id}", 
            JsonSerializer.Serialize(order));
        return Ok();
    }
}
```

### Session Affinity (ARR Affinity)

**Default:** Enabled (sticky sessions)

**When to disable:**
- App is fully stateless
- Want optimal load distribution
- Using distributed session state

```bash
# Disable ARR affinity
az webapp update \
  --resource-group myResourceGroup \
  --name myWebApp \
  --client-affinity-enabled false
```

### Health Checks

Ensure new instances are healthy before receiving traffic:

```csharp
// Startup.cs
app.UseHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString()
            })
        });
        await context.Response.WriteAsync(result);
    }
});
```

```bash
# Configure health check path
az webapp config set \
  --resource-group myResourceGroup \
  --name myWebApp \
  --health-check-path "/health"
```

## Performance During Scale Events

### Minimize Impact

1. **Warm Up New Instances**

```csharp
// Application_Start or equivalent
public void Warmup()
{
    // Initialize caches
    _cache.WarmupAsync().Wait();
    
    // Pre-load configurations
    _config.LoadAsync().Wait();
    
    // Establish database connections
    _dbContext.Database.OpenConnection();
}
```

2. **Graceful Shutdown**

```csharp
// Handle shutdown gracefully
public class Startup
{
    public void Configure(IApplicationBuilder app, IHostApplicationLifetime lifetime)
    {
        lifetime.ApplicationStopping.Register(() =>
        {
            // Finish processing current requests
            Thread.Sleep(5000);
            
            // Close connections gracefully
            _dbContext.Dispose();
        });
    }
}
```

### Scale Speed

**Factors affecting scale speed:**
- Instance size (larger = slower to start)
- Application startup time
- Docker image size (for containers)
- Cold start optimizations

**Typical scale times:**
- Scale out: 2-5 minutes
- Scale in: Immediate (after cooldown)

## Common Patterns

### Pattern 1: Predictable Traffic

```yaml
# Business hours: More instances
08:00-18:00 Mon-Fri: 5-15 instances

# Off hours: Fewer instances
18:00-08:00, Weekends: 2-5 instances
```

### Pattern 2: Event-Driven

```yaml
# Normal: Baseline instances
Default: 3 instances

# During events: Scale aggressively
CPU > 60%: Add 2 instances
CPU > 80%: Add 5 instances
```

### Pattern 3: Cost-Optimized

```yaml
# Minimize instances while maintaining SLA
Min: 1 instance
Max: 5 instances
CPU > 85%: Scale out
CPU < 20%: Scale in
Cooldown: 15 minutes
```

## Troubleshooting

### Issue: Flapping (Rapid Scale In/Out)

**Symptoms:**
- Frequent scale operations
- Performance instability

**Solutions:**
- Increase cooldown periods
- Adjust thresholds (wider gap between scale-out and scale-in)
- Use longer time windows

### Issue: Slow to Scale

**Symptoms:**
- High latency during traffic spikes
- Scale operations too slow

**Solutions:**
- Lower scale-out threshold
- Reduce time window
- Use schedule-based pre-scaling
- Consider Premium tier (faster provisioning)

### Issue: Excessive Scaling

**Symptoms:**
- Too many instances
- High costs

**Solutions:**
- Review thresholds
- Check for metric anomalies
- Adjust maximum instance count
- Review application efficiency

## Cost Optimization

### Strategies

1. **Right-Size Instances**: Use smaller instances with more count
2. **Scheduled Scaling**: Scale down during off-hours
3. **Reserved Capacity**: Use reserved instances for baseline
4. **Monitor Costs**: Set budget alerts

### Cost Monitoring

```bash
# Get cost analysis
az consumption usage list \
  --start-date 2024-01-01 \
  --end-date 2024-01-31 \
  --query "[?contains(instanceName, 'myAppServicePlan')]"
```

## Lab Exercise

Complete auto-scaling configuration:

1. **Setup**: Create App Service Plan (S1 or higher)
2. **Configure**: Set up auto-scale rules
3. **Test**: Generate load and observe scaling
4. **Monitor**: Track scale events and performance
5. **Optimize**: Adjust thresholds based on results

## Checklist

- [ ] Auto-scaling enabled
- [ ] Minimum instances set (≥2)
- [ ] Maximum instances set
- [ ] Scale-out rules configured
- [ ] Scale-in rules configured
- [ ] Cooldown periods appropriate
- [ ] Application is stateless
- [ ] Health checks configured
- [ ] Monitoring and alerts set up
- [ ] Costs monitored

## Additional Resources

- [Auto-scale in Azure](https://docs.microsoft.com/azure/azure-monitor/autoscale/autoscale-overview)
- [Best Practices for Auto-scale](https://docs.microsoft.com/azure/azure-monitor/autoscale/autoscale-best-practices)
- [App Service Auto-scale](https://docs.microsoft.com/azure/app-service/manage-scale-up)

---

[← Back to Module 4](../../README.md#module-4-scaling-strategies)
