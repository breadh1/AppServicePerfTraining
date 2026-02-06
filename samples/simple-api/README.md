# Simple Web API Sample

A basic ASP.NET Core Web API with Application Insights integration for performance monitoring and optimization exercises.

## Overview

This sample application demonstrates:
- Application Insights integration
- Custom telemetry tracking
- Performance monitoring
- Caching implementation
- Connection management

## Features

- RESTful API endpoints
- Application Insights telemetry
- In-memory caching example
- Database simulation
- Performance instrumentation

## Prerequisites

- .NET 8.0 SDK or later
- Azure subscription (for deployment)
- Visual Studio Code or Visual Studio

## Getting Started

### Local Development

1. **Clone or navigate to this directory**

```bash
cd samples/simple-api
```

2. **Restore dependencies**

```bash
dotnet restore
```

3. **Run the application**

```bash
dotnet run
```

4. **Access the API**

```
https://localhost:5001/api/products
https://localhost:5001/api/health
```

### Configuration

Update `appsettings.json` with your Application Insights key:

```json
{
  "ApplicationInsights": {
    "InstrumentationKey": "your-key-here"
  }
}
```

Or use environment variable:

```bash
export APPINSIGHTS_INSTRUMENTATIONKEY="your-key-here"
```

## API Endpoints

### GET /api/products
Returns a list of products with simulated database latency.

**Response:**
```json
[
  {
    "id": 1,
    "name": "Product 1",
    "price": 99.99,
    "category": "Electronics"
  }
]
```

### GET /api/products/{id}
Returns a specific product by ID.

### POST /api/products
Creates a new product (simulated).

### GET /api/health
Health check endpoint for monitoring.

### GET /api/stress/cpu
Generates CPU load for testing (development only).

### GET /api/stress/memory
Generates memory pressure for testing (development only).

## Performance Features

### Telemetry Tracking

The application tracks:
- Request duration
- Dependency calls
- Custom metrics
- Exception tracking

### Caching

Demonstrates in-memory caching for frequently accessed data:

```csharp
[HttpGet]
[ResponseCache(Duration = 60)]
public async Task<IActionResult> GetProducts()
{
    // Cached for 60 seconds
}
```

### Async Patterns

All database and external calls use async/await:

```csharp
public async Task<List<Product>> GetProductsAsync()
{
    await Task.Delay(100); // Simulated DB latency
    return _products;
}
```

## Deployment to Azure

### Using Azure CLI

```bash
# Variables
RESOURCE_GROUP="myResourceGroup"
APP_NAME="simple-api-$(date +%s)"
LOCATION="eastus"
APP_SERVICE_PLAN="myAppServicePlan"

# Create resource group
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create App Service Plan
az appservice plan create \
  --name $APP_SERVICE_PLAN \
  --resource-group $RESOURCE_GROUP \
  --sku B1 \
  --is-linux

# Create Web App
az webapp create \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --plan $APP_SERVICE_PLAN \
  --runtime "DOTNET|8.0"

# Deploy code
dotnet publish -c Release
cd bin/Release/net8.0/publish
zip -r ../deploy.zip .
az webapp deployment source config-zip \
  --resource-group $RESOURCE_GROUP \
  --name $APP_NAME \
  --src ../deploy.zip
```

## Performance Testing

### Using curl

```bash
# Test response time
time curl https://your-app.azurewebsites.net/api/products

# Load test (simple)
for i in {1..100}; do
  curl -s https://your-app.azurewebsites.net/api/products > /dev/null
done
```

### Using Apache Bench

```bash
ab -n 1000 -c 10 https://your-app.azurewebsites.net/api/products
```

## Monitoring

View telemetry in Application Insights:

1. Navigate to your Application Insights resource
2. View Live Metrics for real-time data
3. Use Logs to query performance data

Example queries:

```kusto
// Average response time
requests
| where timestamp > ago(1h)
| summarize avg(duration) by name

// Request rate
requests
| where timestamp > ago(1h)
| summarize count() by bin(timestamp, 1m)
| render timechart
```

## Project Structure

```
simple-api/
├── Controllers/
│   ├── ProductsController.cs
│   └── HealthController.cs
├── Models/
│   └── Product.cs
├── Services/
│   └── ProductService.cs
├── Program.cs
├── appsettings.json
└── README.md
```

## Learning Exercises

1. **Baseline Performance**: Measure initial performance metrics
2. **Add Caching**: Implement response caching and measure improvement
3. **Optimize Queries**: Reduce simulated DB latency
4. **Load Testing**: Test under various load levels
5. **Monitoring**: Set up alerts and dashboards

## Common Performance Patterns

### 1. Response Caching
```csharp
[ResponseCache(Duration = 60, VaryByQueryKeys = new[] { "category" })]
```

### 2. Memory Caching
```csharp
_cache.Set(cacheKey, data, TimeSpan.FromMinutes(5));
```

### 3. Async Operations
```csharp
await _service.GetDataAsync();
```

### 4. Custom Telemetry
```csharp
_telemetry.TrackMetric("DatabaseQueryTime", duration);
```

## Troubleshooting

### High Response Times
- Check Application Insights for slow dependencies
- Review database query performance
- Verify caching is working

### Memory Issues
- Monitor memory metrics in Azure
- Check for memory leaks in custom code
- Review object disposal

### CPU Usage
- Profile application under load
- Check for blocking operations
- Optimize hot code paths

## Additional Resources

- [ASP.NET Core Performance Best Practices](https://docs.microsoft.com/aspnet/core/performance/performance-best-practices)
- [Application Insights for ASP.NET Core](https://docs.microsoft.com/azure/azure-monitor/app/asp-net-core)
- [Caching in ASP.NET Core](https://docs.microsoft.com/aspnet/core/performance/caching/response)

---

[← Back to Samples](../../README.md#sample-applications)
