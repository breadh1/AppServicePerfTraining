# Quick Start Guide

Welcome to Azure App Service Performance Training! This guide will help you get started quickly.

## 🚀 Getting Started

### For Beginners

If you're new to Azure App Service performance optimization:

1. **Start with Module 1**: [App Service Architecture & Performance Fundamentals](./docs/module-1/app-service-plans.md)
   - Understand App Service Plans
   - Learn about compute resources
   - Study the request processing pipeline
   - Master performance metrics

2. **Set Up Monitoring**: Complete [Lab 1: Setting up Monitoring](./labs/lab-1-monitoring/README.md)
   - Enable Application Insights
   - Configure custom telemetry
   - Create dashboards
   - Set up alerts

3. **Learn Caching**: Read [Module 3: Caching Strategies](./docs/module-3/caching.md)
   - Understand different cache types
   - Learn when to use each strategy
   - Implement cache invalidation

4. **Practice**: Complete [Lab 3: Implementing Caching](./labs/lab-3-caching/README.md)
   - Measure baseline performance
   - Implement caching
   - Compare results

### For Intermediate Users

If you already understand the basics:

1. **Review** [Module 4: Auto-scaling](./docs/module-4/auto-scaling.md)
   - Configure auto-scale rules
   - Optimize scaling strategy
   - Monitor scale events

2. **Troubleshoot** with [Module 5: Common Issues](./docs/module-5/common-issues.md)
   - Diagnose performance problems
   - Fix high CPU and memory usage
   - Resolve slow response times

### For Advanced Users

If you're looking for advanced topics:

1. Check the README for Module 6: Advanced Topics (coming soon)
2. Review best practices in each module
3. Contribute your own experiences to the repository

## 📋 Prerequisites

### Azure Resources
- Azure subscription ([Get a free trial](https://azure.microsoft.com/free/))
- Basic familiarity with Azure Portal

### Development Environment
- **For .NET samples**: .NET 8.0 SDK
- **For Node.js samples**: Node.js 18+
- **For load testing**: Apache Bench or k6
- **IDE**: Visual Studio Code or Visual Studio

### Knowledge Prerequisites
- Basic web development
- HTTP protocol understanding
- Familiarity with cloud concepts

## 🎯 Learning Paths

### Path 1: Performance Fundamentals (2-3 hours)
```
1. Module 1: App Service Plans → 30 min
2. Module 1: Compute Resources → 30 min
3. Module 1: Request Pipeline → 30 min
4. Module 1: Performance Metrics → 45 min
5. Lab 1: Setup Monitoring → 45 min
```

### Path 2: Optimization Techniques (3-4 hours)
```
1. Module 3: Caching Strategies → 45 min
2. Lab 3: Implementing Caching → 60 min
3. Module 4: Auto-scaling → 45 min
4. Best Practices Review → 30 min
5. Practice exercises → 60 min
```

### Path 3: Troubleshooting (2-3 hours)
```
1. Module 5: Common Issues → 60 min
2. Practice diagnostics → 60 min
3. Real-world scenarios → 60 min
```

## 🛠️ Quick Setup

### Deploy a Sample App (5 minutes)

```bash
# Variables
RESOURCE_GROUP="my-training-rg"
LOCATION="eastus"
APP_NAME="my-training-app-$RANDOM"

# Create resource group
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create App Service Plan
az appservice plan create \
  --name "${APP_NAME}-plan" \
  --resource-group $RESOURCE_GROUP \
  --sku B1 \
  --is-linux

# Create Web App
az webapp create \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --plan "${APP_NAME}-plan" \
  --runtime "DOTNET|8.0"

echo "App created: https://${APP_NAME}.azurewebsites.net"
```

### Enable Monitoring (2 minutes)

```bash
# Create Application Insights
az monitor app-insights component create \
  --app "${APP_NAME}-insights" \
  --location $LOCATION \
  --resource-group $RESOURCE_GROUP \
  --application-type web

# Link to Web App (done automatically in portal, or set manually)
```

## 📚 Key Resources

### Official Documentation
- [Azure App Service Docs](https://docs.microsoft.com/azure/app-service/)
- [Performance Best Practices](https://docs.microsoft.com/azure/app-service/overview-performance)
- [Application Insights](https://docs.microsoft.com/azure/azure-monitor/app/app-insights-overview)

### Tools
- [Azure Portal](https://portal.azure.com)
- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli)
- [Visual Studio Code](https://code.visualstudio.com/)

### Community
- [Azure App Service Blog](https://azure.github.io/AppService/)
- [Stack Overflow](https://stackoverflow.com/questions/tagged/azure-app-service)

## 💡 Tips for Success

1. **Hands-On Learning**: Don't just read - deploy and test!
2. **Start Simple**: Begin with basic concepts before advanced topics
3. **Measure Everything**: Always establish baselines before optimizing
4. **Document Your Findings**: Keep notes on what works for your scenarios
5. **Join the Community**: Share your experiences and learn from others

## 🎓 Certification Path

While this training doesn't directly map to certifications, it helps with:
- AZ-204: Developing Solutions for Microsoft Azure
- AZ-400: Designing and Implementing Microsoft DevOps Solutions

## 📞 Getting Help

- **Issues**: Open an issue in this repository
- **Questions**: Use GitHub Discussions
- **Contributions**: See [CONTRIBUTING.md](./CONTRIBUTING.md)

## 🔄 What's Next?

After completing the training:

1. **Apply to Your Apps**: Implement learnings in your projects
2. **Share Knowledge**: Train your team
3. **Contribute**: Add your own examples and scenarios
4. **Stay Updated**: Watch for new modules and labs

## ✅ Quick Checklist

Before you start:
- [ ] Azure subscription ready
- [ ] Azure CLI installed
- [ ] Development environment set up
- [ ] Sample app deployed (optional)
- [ ] Application Insights enabled (optional)

After Module 1:
- [ ] Understand App Service tiers
- [ ] Can explain request pipeline
- [ ] Know key performance metrics
- [ ] Have monitoring configured

After optimization modules:
- [ ] Implemented caching
- [ ] Configured auto-scaling
- [ ] Can diagnose performance issues
- [ ] Following best practices

## 📈 Track Your Progress

Create a simple progress tracker:

```markdown
## My Training Progress

### Week 1
- [x] Module 1 completed
- [x] Lab 1 completed
- [ ] Module 3 in progress

### Goals
- [ ] Deploy production app with monitoring
- [ ] Implement caching strategy
- [ ] Configure auto-scaling
```

---

**Ready to begin?** Start with [Module 1: App Service Plans](./docs/module-1/app-service-plans.md)

**Need help?** Check the [main README](./README.md) or open an issue.

**Happy Learning! 🚀**
