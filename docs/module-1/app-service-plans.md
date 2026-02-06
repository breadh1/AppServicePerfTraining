# Understanding App Service Plans

## Overview

An App Service Plan defines the compute resources for your web app. Understanding App Service Plans is crucial for optimizing performance and cost.

## What is an App Service Plan?

An App Service Plan is essentially a set of compute resources (virtual machines) on which your App Service apps run. Multiple apps can share the same plan, and they all run on the same VM instances.

## Pricing Tiers

### Free and Shared (F1, D1)
- **Use Case**: Development and testing
- **Resources**: Limited CPU, memory (60 CPU minutes/day for Free)
- **Limitations**: No SLA, no custom domains (Free), no auto-scaling
- **Performance**: Not suitable for production workloads

### Basic (B1, B2, B3)
- **Use Case**: Low-traffic apps, development/staging
- **Resources**: Dedicated compute, 1-3 cores, 1.75-7 GB RAM
- **Features**: Custom domains, SSL, manual scaling
- **Limitations**: No auto-scaling, no deployment slots

### Standard (S1, S2, S3)
- **Use Case**: Production workloads
- **Resources**: Dedicated compute, 1-4 cores, 1.75-7 GB RAM
- **Features**: Auto-scaling (up to 10 instances), 5 deployment slots
- **Performance**: Good for most production scenarios

### Premium (P1v2, P2v2, P3v2, P1v3, P2v3, P3v3)
- **Use Case**: High-performance production workloads
- **Resources**: More powerful compute, 1-8 cores, 3.5-32 GB RAM
- **Features**: Auto-scaling (up to 30 instances), 20 deployment slots
- **Performance**: Enhanced storage, faster instances

### Isolated (I1, I2, I3)
- **Use Case**: Mission-critical workloads requiring network isolation
- **Resources**: Dedicated VMs in dedicated Virtual Network
- **Features**: 100 instances, complete isolation
- **Performance**: Maximum scale and isolation

## Performance Characteristics

### CPU Performance
- **Free/Shared**: Throttled, shared resources
- **Basic/Standard**: Dedicated CPU, consistent performance
- **Premium**: Higher CPU performance, newer hardware
- **Isolated**: Dedicated environment, predictable performance

### Memory
- More memory allows for:
  - Larger in-memory caches
  - More concurrent requests
  - Better performance under load

### Network
- Higher tiers offer better network throughput
- Isolated tier provides dedicated network infrastructure

## Choosing the Right Plan

### Consider:
1. **Traffic volume**: Expected requests per second
2. **Resource requirements**: CPU, memory needs
3. **Scaling needs**: Auto-scaling requirements
4. **Budget**: Cost vs. performance trade-offs
5. **Features**: Deployment slots, VNet integration, etc.

### Decision Matrix:

| Scenario | Recommended Tier |
|----------|-----------------|
| Development/Testing | Free or Basic B1 |
| Small production app (<10k req/day) | Basic B2 or Standard S1 |
| Medium production app | Standard S2-S3 |
| High-traffic app | Premium P2v3+ |
| Enterprise/Isolated | Isolated I1+ |

## Performance Best Practices

1. **Right-Size Your Plan**: Don't over-provision, but allow headroom for traffic spikes
2. **Monitor Usage**: Track CPU, memory, and network metrics
3. **Scale Out Over Up**: Horizontal scaling (more instances) often better than vertical (bigger tier)
4. **Use Premium for Production**: Premium V3 offers best price/performance
5. **Isolate Critical Apps**: Don't share plans between critical and non-critical apps

## Scaling Considerations

### Vertical Scaling (Scale Up)
- Move to higher tier for more resources
- Minimal downtime (brief restart)
- Use when single instance performance is insufficient

### Horizontal Scaling (Scale Out)
- Add more instances within same tier
- Better for handling traffic spikes
- Requires stateless application design

## Cost Optimization

1. **App Service Plan Density**: Multiple small apps can share a plan
2. **Scale Down Off-Hours**: Use auto-scaling to reduce instances during low traffic
3. **Use Appropriate Tier**: Don't use Premium if Standard suffices
4. **Reserved Instances**: Save up to 55% with 1 or 3-year commitments

## Lab Exercise

Try the following:
1. Create an App Service Plan in the Azure portal
2. Deploy a sample app
3. Monitor metrics for 24 hours
4. Scale up to a higher tier and observe performance differences
5. Scale out to multiple instances

## Additional Resources

- [App Service Pricing](https://azure.microsoft.com/pricing/details/app-service/)
- [App Service Plan Overview](https://docs.microsoft.com/azure/app-service/overview-hosting-plans)
- [Scaling in Azure App Service](https://docs.microsoft.com/azure/app-service/manage-scale-up)

## Quiz

1. What's the difference between scaling up and scaling out?
2. Which tier supports auto-scaling?
3. When should you use Premium over Standard?
4. Can you move an app between plans?

---

[← Back to Training Modules](../../README.md#training-modules) | [Next: Compute Resources →](./compute-resources.md)
