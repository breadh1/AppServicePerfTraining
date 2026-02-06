# Lab 1: Setting up Monitoring for Azure App Service

## Objective

Learn how to set up comprehensive monitoring for an Azure App Service using Application Insights and Azure Monitor.

## Prerequisites

- Azure subscription
- Azure CLI installed
- Basic understanding of Azure App Service
- A deployed web application (or use our sample)

## Duration

45 minutes

## Lab Overview

In this lab, you will:
1. Enable Application Insights for your App Service
2. Configure custom metrics and telemetry
3. Set up alerts for key performance indicators
4. Create a performance dashboard
5. Analyze collected metrics

## Part 1: Enable Application Insights (10 minutes)

### Step 1: Enable Through Azure Portal

1. Navigate to your App Service in Azure Portal
2. Select **Application Insights** from the left menu
3. Click **Turn on Application Insights**
4. Choose or create a new Application Insights resource
5. Select your preferred region (same as App Service for best performance)
6. Click **Apply**

### Step 2: Enable Through Azure CLI

```bash
# Variables
RESOURCE_GROUP="myResourceGroup"
APP_NAME="myWebApp"
LOCATION="eastus"
APP_INSIGHTS_NAME="${APP_NAME}-insights"

# Create Application Insights
az monitor app-insights component create \
  --app $APP_INSIGHTS_NAME \
  --location $LOCATION \
  --resource-group $RESOURCE_GROUP \
  --application-type web

# Get Instrumentation Key
INSTRUMENTATION_KEY=$(az monitor app-insights component show \
  --app $APP_INSIGHTS_NAME \
  --resource-group $RESOURCE_GROUP \
  --query instrumentationKey -o tsv)

# Configure App Service
az webapp config appsettings set \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings APPINSIGHTS_INSTRUMENTATIONKEY=$INSTRUMENTATION_KEY
```

### Step 3: Verify Configuration

```bash
# Check if Application Insights is enabled
az webapp config appsettings list \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query "[?name=='APPINSIGHTS_INSTRUMENTATIONKEY']"
```

## Part 2: Configure Application Code (15 minutes)

### For ASP.NET Core Applications

1. **Install NuGet Package**

```bash
dotnet add package Microsoft.ApplicationInsights.AspNetCore
```

2. **Configure in Startup.cs or Program.cs**

```csharp
// Program.cs (ASP.NET Core 6+)
using Microsoft.ApplicationInsights;

var builder = WebApplication.CreateBuilder(args);

// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

3. **Add Custom Telemetry**

```csharp
public class ProductController : ControllerBase
{
    private readonly TelemetryClient _telemetryClient;
    
    public ProductController(TelemetryClient telemetryClient)
    {
        _telemetryClient = telemetryClient;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Your business logic
        var products = await _productService.GetAllAsync();
        
        stopwatch.Stop();
        
        // Track custom metric
        _telemetryClient.TrackMetric("ProductRetrievalTime", 
            stopwatch.ElapsedMilliseconds);
        _telemetryClient.TrackEvent("ProductsRetrieved", 
            new Dictionary<string, string> { { "Count", products.Count.ToString() } });
        
        return Ok(products);
    }
}
```

### For Node.js Applications

1. **Install Package**

```bash
npm install applicationinsights
```

2. **Configure in app.js**

```javascript
const appInsights = require('applicationinsights');

// Initialize Application Insights
appInsights.setup(process.env.APPINSIGHTS_INSTRUMENTATIONKEY)
    .setAutoDependencyCorrelation(true)
    .setAutoCollectRequests(true)
    .setAutoCollectPerformance(true, true)
    .setAutoCollectExceptions(true)
    .setAutoCollectDependencies(true)
    .setAutoCollectConsole(true)
    .setUseDiskRetryCaching(true)
    .start();

const client = appInsights.defaultClient;

// Track custom metrics
app.get('/api/products', async (req, res) => {
    const startTime = Date.now();
    
    // Your business logic
    const products = await getProducts();
    
    const duration = Date.now() - startTime;
    client.trackMetric({ name: "ProductRetrievalTime", value: duration });
    client.trackEvent({ name: "ProductsRetrieved", properties: { count: products.length } });
    
    res.json(products);
});
```

## Part 3: Set Up Alerts (10 minutes)

### Create CPU Alert

```bash
# Create action group first
az monitor action-group create \
  --name "PerformanceAlerts" \
  --resource-group $RESOURCE_GROUP \
  --short-name "PerfAlert" \
  --email-receiver admin admin@example.com

# Create CPU alert
az monitor metrics alert create \
  --name "HighCPUAlert" \
  --resource-group $RESOURCE_GROUP \
  --scopes "/subscriptions/{subscription-id}/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.Web/sites/$APP_NAME" \
  --condition "avg CpuPercentage > 80" \
  --description "Alert when CPU exceeds 80%" \
  --evaluation-frequency 1m \
  --window-size 5m \
  --action "PerformanceAlerts"
```

### Create Response Time Alert

```bash
az monitor metrics alert create \
  --name "SlowResponseAlert" \
  --resource-group $RESOURCE_GROUP \
  --scopes "/subscriptions/{subscription-id}/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.Web/sites/$APP_NAME" \
  --condition "avg ResponseTime > 2000" \
  --description "Alert when response time exceeds 2 seconds" \
  --evaluation-frequency 1m \
  --window-size 5m \
  --action "PerformanceAlerts"
```

### Create Availability Alert

```bash
# Create availability test first (in portal or via API)
# Then create alert on availability results
az monitor metrics alert create \
  --name "AvailabilityAlert" \
  --resource-group $RESOURCE_GROUP \
  --scopes "/subscriptions/{subscription-id}/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.Insights/components/$APP_INSIGHTS_NAME" \
  --condition "avg availabilityResults/availabilityPercentage < 99" \
  --description "Alert when availability drops below 99%" \
  --evaluation-frequency 5m \
  --window-size 15m \
  --action "PerformanceAlerts"
```

## Part 4: Create Performance Dashboard (10 minutes)

### Using Azure Portal

1. Navigate to your App Service
2. Click **Metrics** in the left menu
3. Add the following charts:
   - CPU Percentage
   - Memory Percentage
   - Response Time
   - Http Server Errors (5xx)
   - Requests
4. Click **Pin to dashboard** for each chart
5. Navigate to **Dashboard** to see all metrics

### Using Azure CLI (Create Dashboard JSON)

Create a file named `dashboard.json`:

```json
{
  "properties": {
    "lenses": {
      "0": {
        "order": 0,
        "parts": {
          "0": {
            "position": {
              "x": 0,
              "y": 0,
              "colSpan": 6,
              "rowSpan": 4
            },
            "metadata": {
              "type": "Extension/Microsoft_Azure_Monitoring/PartType/MetricsChartPart",
              "settings": {
                "content": {
                  "metrics": [
                    {
                      "resourceId": "/subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Web/sites/{app}",
                      "name": "CpuPercentage"
                    }
                  ]
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

## Part 5: Analyze Metrics (10 minutes)

### Query Application Insights

1. Navigate to Application Insights resource
2. Click **Logs** in the left menu
3. Run the following queries:

**Average Response Time by Operation**
```kusto
requests
| where timestamp > ago(1h)
| summarize avg(duration) by name
| order by avg_duration desc
```

**Request Rate Over Time**
```kusto
requests
| where timestamp > ago(24h)
| summarize count() by bin(timestamp, 5m)
| render timechart
```

**Failed Requests**
```kusto
requests
| where success == false
| summarize count() by resultCode, name
| order by count_ desc
```

**Dependency Performance**
```kusto
dependencies
| where timestamp > ago(1h)
| summarize avg(duration), max(duration), count() by name
| order by avg_duration desc
```

## Verification Steps

- [ ] Application Insights is enabled and collecting data
- [ ] Custom metrics are being tracked
- [ ] Alerts are configured and active
- [ ] Dashboard displays all key metrics
- [ ] Can query and analyze performance data

## Common Issues

### Issue: No data appearing in Application Insights
- **Solution**: Wait 5-10 minutes for initial data
- **Check**: Verify instrumentation key is correct
- **Check**: Ensure application has traffic

### Issue: Custom metrics not showing
- **Solution**: Verify SDK is installed correctly
- **Check**: Check application logs for errors
- **Check**: Ensure code is actually executing

### Issue: Alerts not firing
- **Solution**: Verify thresholds are set correctly
- **Check**: Ensure action group is configured
- **Check**: Check alert evaluation conditions

## Next Steps

- Explore Live Metrics Stream for real-time monitoring
- Set up availability tests
- Configure sampling for high-volume applications
- Integrate with Azure DevOps for deployment tracking

## Additional Resources

- [Application Insights Overview](https://docs.microsoft.com/azure/azure-monitor/app/app-insights-overview)
- [Custom Metrics](https://docs.microsoft.com/azure/azure-monitor/app/api-custom-events-metrics)
- [Kusto Query Language](https://docs.microsoft.com/azure/data-explorer/kusto/query/)

---

[← Back to Labs](../../README.md#hands-on-labs) | [Next Lab: Performance Baseline →](../lab-2-baseline/README.md)
