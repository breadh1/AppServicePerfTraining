# Compute Resources and Tiers

## Understanding Compute Resources

Compute resources in Azure App Service directly impact application performance. This guide covers how resources are allocated and how to optimize their usage.

## Resource Allocation

### CPU
- **Measurement**: CPU time, percentage
- **Impact**: Processing speed, request handling capacity
- **Throttling**: Free/Shared tiers have CPU quotas

### Memory
- **Measurement**: RAM in GB
- **Impact**: Cache size, concurrent connections, session state
- **Limits**: Per-tier limits, automatic restarts if exceeded

### Network
- **Measurement**: Bandwidth, connections
- **Impact**: Response times, throughput
- **Limits**: Outbound data transfer limits (except Isolated)

### Storage
- **Measurement**: GB of disk space
- **Impact**: Application files, logs, temporary storage
- **Limits**: 10 GB (Standard) to 1 TB (Isolated)

## Tier Specifications

### Detailed Comparison

| Tier | Cores | RAM | Storage | Network |
|------|-------|-----|---------|---------|
| F1 | Shared | 1 GB | 1 GB | Shared |
| D1 | Shared | 1 GB | 1 GB | Shared |
| B1 | 1 | 1.75 GB | 10 GB | Medium |
| B2 | 2 | 3.5 GB | 10 GB | Medium |
| B3 | 4 | 7 GB | 10 GB | Medium |
| S1 | 1 | 1.75 GB | 50 GB | Medium |
| S2 | 2 | 3.5 GB | 50 GB | Medium |
| S3 | 4 | 7 GB | 50 GB | Medium |
| P1v3 | 2 | 8 GB | 250 GB | High |
| P2v3 | 4 | 16 GB | 250 GB | High |
| P3v3 | 8 | 32 GB | 250 GB | High |

## Performance Characteristics

### CPU-Bound Applications
Applications that require heavy computation:
- Image processing
- Data transformation
- Encryption/decryption
- Complex algorithms

**Recommendation**: Higher tier with more cores, Premium v3

### Memory-Bound Applications
Applications that require significant memory:
- Large object caches
- In-memory data structures
- Session state management
- Large file processing

**Recommendation**: Premium tiers with more RAM

### I/O-Bound Applications
Applications limited by disk or network:
- Database queries
- External API calls
- File operations
- Message queue operations

**Recommendation**: Focus on async patterns, connection pooling

## Resource Monitoring

### Key Metrics to Track

1. **CPU Percentage**
   - Target: < 70% average
   - Alert: > 85% sustained

2. **Memory Percentage**
   - Target: < 75% average
   - Alert: > 90% sustained

3. **Disk I/O**
   - Monitor read/write operations
   - Watch for throttling

4. **Network Bandwidth**
   - Track inbound/outbound traffic
   - Monitor connection limits

### Using Azure Monitor

```bash
# Get CPU metrics
az monitor metrics list \
  --resource <resource-id> \
  --metric "CpuPercentage" \
  --start-time 2024-01-01T00:00:00Z \
  --end-time 2024-01-01T23:59:59Z
```

## Resource Optimization Strategies

### 1. Efficient Resource Usage

**CPU Optimization**:
- Use async/await patterns
- Implement proper caching
- Optimize algorithms
- Profile and identify bottlenecks

**Memory Optimization**:
- Dispose of objects properly
- Use object pooling
- Implement memory-efficient data structures
- Monitor for memory leaks

**Network Optimization**:
- Use compression
- Implement connection pooling
- Batch operations where possible
- Use CDN for static content

### 2. Resource Quotas

Understand your limits:

```csharp
// Example: Monitor memory usage
var process = Process.GetCurrentProcess();
var memoryUsage = process.WorkingSet64 / (1024 * 1024); // MB
Console.WriteLine($"Memory usage: {memoryUsage} MB");
```

### 3. Scaling Decisions

**Scale Up When**:
- Consistent high CPU/memory usage
- Single instance can't handle load
- Need more features (deployment slots, etc.)

**Scale Out When**:
- Traffic spikes
- Geographical distribution needed
- High availability required

## Advanced Topics

### 1. App Service Environment (ASE)
- Dedicated compute resources
- Network isolation
- Large-scale deployments

### 2. Resource Limits
- Connection limits per instance
- Request timeout settings
- Thread pool configuration

### 3. Platform Limitations
- 32-bit vs 64-bit processes
- Windows vs Linux differences
- Runtime version implications

## Practical Exercise

### Exercise 1: Resource Profiling
1. Deploy a sample application
2. Generate load using a load testing tool
3. Monitor CPU and memory usage
4. Identify resource bottlenecks

### Exercise 2: Right-Sizing
1. Start with Basic B1 tier
2. Gradually increase load
3. Identify when to scale up
4. Compare performance across tiers

### Exercise 3: Memory Leak Detection
1. Create an app with a memory leak
2. Monitor memory growth
3. Use profiling tools to identify the leak
4. Fix and verify

## Best Practices Checklist

- [ ] Monitor resource usage regularly
- [ ] Set up alerts for high resource usage
- [ ] Profile application under load
- [ ] Choose appropriate tier for workload
- [ ] Implement resource-efficient code
- [ ] Use async patterns for I/O operations
- [ ] Enable Always On for production apps
- [ ] Configure appropriate timeout settings
- [ ] Implement proper error handling
- [ ] Regular performance testing

## Common Issues

### High CPU Usage
- **Symptoms**: Slow response times, timeouts
- **Causes**: Inefficient code, infinite loops, blocking operations
- **Solutions**: Profile code, optimize algorithms, use async patterns

### Memory Pressure
- **Symptoms**: Frequent restarts, OutOfMemoryException
- **Causes**: Memory leaks, large object retention, excessive caching
- **Solutions**: Fix leaks, optimize cache, increase memory

### Network Throttling
- **Symptoms**: Failed connections, timeouts
- **Causes**: Too many connections, bandwidth limits
- **Solutions**: Connection pooling, optimize payload sizes

## Additional Resources

- [App Service Limits](https://docs.microsoft.com/azure/azure-resource-manager/management/azure-subscription-service-limits#app-service-limits)
- [Performance Tuning](https://docs.microsoft.com/azure/app-service/overview-performance)
- [Resource Monitoring](https://docs.microsoft.com/azure/app-service/web-sites-monitor)

---

[← Previous: App Service Plans](./app-service-plans.md) | [Next: Request Pipeline →](./request-pipeline.md)
