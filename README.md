# Memory Leak Training Lab

A .NET Web Application designed for Azure App Service performance troubleshooting training.

## Features

- 🧠 **Continuous Memory Leak**: Generate ~20MB/sec of memory leak
- 💉 **One-time Injection**: Inject 10-200MB of memory instantly
- 📊 **Real-time Monitoring**: View leaked memory, GC stats, and heap size
- 🎮 **Visual Dashboard**: Interactive web UI with buttons and sliders

## Quick Deploy to Azure

### Option 1: Deploy to Azure Button (Recommended)

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fbreadh1%2FAppServicePerfTraining%2Fmain%2Fazuredeploy.json)

> **Note**: Click the button above to deploy directly to your Azure subscription.

### Option 2: Azure CLI Deployment

```bash
# Login to Azure
az login

# Create a resource group
az group create --name rg-memoryleak-training --location eastasia

# Deploy the ARM template
az deployment group create \
  --resource-group rg-memoryleak-training \
  --template-file azuredeploy.json \
  --parameters azuredeploy.parameters.json
```

### Option 3: PowerShell Deployment

```powershell
# Login to Azure
Connect-AzAccount

# Create a resource group
New-AzResourceGroup -Name "rg-memoryleak-training" -Location "East Asia"

# Deploy the ARM template
New-AzResourceGroupDeployment `
  -ResourceGroupName "rg-memoryleak-training" `
  -TemplateFile "azuredeploy.json" `
  -TemplateParameterFile "azuredeploy.parameters.json"
```

## Manual Deployment Steps

### 1. Publish the application

```bash
cd Memoryleak
dotnet publish -c Release -o ./publish
```

### 2. Deploy to Azure App Service

**Using VS Code:**
1. Install the Azure App Service extension
2. Right-click on the `publish` folder
3. Select "Deploy to Web App..."
4. Follow the prompts

**Using Azure CLI:**
```bash
# Create a zip file
cd publish
zip -r ../app.zip .

# Deploy
az webapp deploy --resource-group rg-memoryleak-training --name YOUR_APP_NAME --src-path ../app.zip
```

## API Endpoints

| Endpoint | Description |
|----------|-------------|
| `GET /` | Dashboard - Visual UI with controls |
| `GET /start` | Start continuous memory leak (~20MB/sec) |
| `GET /stop` | Stop continuous memory leak |
| `GET /status` | Get current memory status (JSON) |
| `GET /clear` | Clear leaked memory and force GC |
| `GET /leak?mb=50` | One-time leak of specified MB |

## Training Exercises

### Exercise 1: Observe Memory Growth
1. Open the dashboard
2. Click "Start Leak"
3. Watch the memory grow in Azure Portal > App Service > Metrics

### Exercise 2: Capture Memory Dump
1. Leak ~500MB of memory using the injection feature
2. Go to Azure Portal > App Service > Diagnose and solve problems
3. Collect a memory dump
4. Analyze with dotnet-dump or Visual Studio

### Exercise 3: Identify the Leak
1. Analyze the memory dump
2. Find the `ConcurrentBag<byte[]>` holding the leaked objects
3. Trace back to the root cause

### Exercise 4: Monitor with Application Insights
1. Enable Application Insights on the App Service
2. Create alerts for memory threshold
3. Observe memory metrics during leak

## Recommended App Service SKU

- **B1 (Basic)**: Recommended for training - limited memory makes leak visible quickly
- **S1 (Standard)**: More memory, takes longer to see impact
- **P1v2 (Premium)**: Production-like environment

## Clean Up

```bash
# Delete the resource group to remove all resources
az group delete --name rg-memoryleak-training --yes --no-wait
```

## License

MIT License - Free to use for training purposes.
