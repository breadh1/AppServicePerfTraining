# Memory Leak Training Lab

A .NET Web Application designed for Azure App Service performance troubleshooting training.

## Features

- 🧠 **Continuous Memory Leak**: Generate ~5MB/sec of memory leak (10MB every 2 seconds)
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
  --template-file azuredeploy.json
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
  -TemplateFile "azuredeploy.json"
```

## Lab Manual

📖 **[Open Lab Manual (manual02.html)](manual02.html)** - Step-by-step guide for Memory Leak Analysis training

The lab manual includes:
- Deploy the sample app to Azure
- Reproduce memory leak
- Capture memory dump via Kudu (procdump)
- Analyze dump with WinDbg
- Detailed 3-dump comparison methodology
- Root cause identification with `!gcroot`

## API Endpoints

| Endpoint | Description |
|----------|-------------|
| `GET /` | Dashboard - Visual UI with controls |
| `GET /start` | Start continuous memory leak (~5MB/sec) |
| `GET /stop` | Stop continuous memory leak |
| `GET /status` | Get current memory status (JSON) |
| `GET /clear` | Clear leaked memory and force GC |
| `GET /leak?mb=50` | One-time leak of specified MB |

## Training Overview

### Lab: Memory Leak Analysis
1. Deploy the app using "Deploy to Azure" button
2. Open the dashboard and click "Start Leak"
3. Let memory grow to 300-500MB
4. Capture dumps via Kudu using `procdump -ma -r -n 3 -s 10 [PID] C:\home\logfiles\`
5. Analyze with WinDbg (`!address -summary`, `!eeheap -gc`, `!dumpheap -stat`, `!gcroot`)
6. Identify `ConcurrentBag<byte[]>` as the root cause

## Recommended App Service SKU

- **S1 (Standard)**: Recommended for training - supports memory dump collection via portal
- **B1 (Basic)**: Limited - does NOT support dump collection via Azure Portal
- **P1v2 (Premium)**: Production-like environment

> ⚠️ **Important**: Use **S1 or higher** SKU to enable memory dump collection from Azure Portal/Kudu.

## Clean Up

```bash
# Delete the resource group to remove all resources
az group delete --name rg-memoryleak-training --yes --no-wait
```

> ⚠️ **Cost Warning**: S1 App Service Plan costs approximately $0.10/hour. Always cleanup after training!

## License

MIT License - Free to use for training purposes.
