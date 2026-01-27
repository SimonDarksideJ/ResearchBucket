# LiveKit Deployment Documentation

Comprehensive guides for deploying LiveKit Server across various platforms and architectures.

## 🚀 Quick Start

**New to LiveKit deployment? Start here:**

### [⚡ Quick Reference Guide](10-quick-reference.md) **START HERE!**

Your personalized deployment guide based on your requirements:
- **Mac Development**: Tailscale setup (100% free, 5 minutes)
- **Hetzner Production**: Quick-start for existing domains (15 minutes)
- Cost summaries, common commands, troubleshooting tips

**Perfect for**: Getting started quickly with recommended setup

---

## Overview

LiveKit is an open-source platform for building real-time video, audio, and data applications. This documentation provides detailed deployment guides for multiple deployment strategies, from simple single-server setups to complex multi-cloud architectures.

## Documentation Structure

### [📋 Executive Summary](00-executive-summary.md)

**Start here!** Comprehensive comparison matrix, cost analysis, and recommendations.

**Contents**:

- Comparison matrix across all deployment approaches
- Detailed cost analysis by scale
- Use case recommendations
- Decision framework and decision tree
- Migration paths

**Best for**: Decision makers, architects planning deployment strategy

---

### [💰 Cost & Configuration Guide](09-cost-and-configuration-guide.md)

**Essential reading!** Complete cost breakdown and external configuration requirements.

**Contents**:

- Detailed cost analysis for all deployment options
- External service requirements (DNS, domains, SSL)
- Setup links and step-by-step instructions
- Cost-saving tips and recommendations
- Comparison tables and use case examples

**Best for**: Anyone concerned about costs or unclear about external requirements

---

### [🐧 Linux Host Deployment](01-linux-host-deployment.md)

Native deployment on Linux using systemd for service management.

**Contents**:

- Complete installation steps
- systemd service configuration
- Monitoring and logging setup
- Security hardening
- Troubleshooting guide
- Performance tuning

**Best for**:

- Small to medium deployments (< 1000 concurrent users)
- Budget-conscious projects
- Maximum performance requirements
- Teams with strong Linux expertise

**Pros**: Lowest latency, highest performance, minimal overhead, low cost
**Cons**: Manual scaling, limited auto-recovery, higher operational burden

---

### [🐳 Docker Container Deployment](02-docker-container-deployment.md)

Containerized deployment using Docker and Docker Compose.

**Contents**:

- Docker installation and configuration
- Docker Compose multi-container setup
- Volume management and persistence
- Container networking
- Health checks and monitoring
- Custom Dockerfile creation

**Best for**:

- Development and testing
- Small production deployments
- Teams familiar with containers
- CI/CD integration

**Pros**: Portable, easy rollback, reproducible, version management
**Cons**: Small performance overhead, requires Docker knowledge, manual orchestration

---

### [☸️ Kubernetes Deployment](03-kubernetes-deployment.md)

Production-scale deployment on Kubernetes with auto-scaling and high availability.

**Contents**:

- Helm chart installation
- Manual YAML manifests
- Horizontal Pod Autoscaler configuration
- Ingress and load balancing
- Multi-zone deployment
- Service mesh integration
- Monitoring with Prometheus

**Best for**:

- Large-scale deployments (10,000+ users)
- Auto-scaling requirements
- High availability needs
- Multi-region deployments
- Teams with Kubernetes expertise

**Pros**: Auto-scaling, self-healing, cloud-agnostic, declarative
**Cons**: Complex, steep learning curve, higher overhead, requires expertise

---

### [☁️ Cloud-Native Deployment](04-cloud-native-deployment.md)

Deployment using cloud provider managed services (AWS, GCP, Azure).

**Contents**:

- **AWS**: ECS Fargate, EKS, CloudFormation, Terraform
- **GCP**: Cloud Run, GKE, Cloud Build
- **Azure**: ACI, AKS, ARM templates
- Multi-cloud considerations
- Cost optimization strategies
- Disaster recovery patterns

**Best for**:

- Enterprise deployments
- Global user base
- Managed infrastructure preference
- Compliance requirements
- Teams leveraging cloud ecosystems

**Pros**: Fully managed, auto-scaling, global reach, integrated monitoring
**Cons**: Vendor lock-in, potentially higher cost, learning curve per platform

---

### [⚖️ Load Balancing & Auto-Scaling](05-load-balancing-autoscaling.md)

Advanced architectural patterns for dynamic scaling and intelligent routing.

**Contents**:

- Layer 4 and Layer 7 load balancing
- HAProxy and NGINX configurations
- Metric-based auto-scaling
- Predictive scaling algorithms
- Per-room server isolation
- Dynamic server provisioning
- Intelligent room placement
- Cost optimization strategies

**Best for**:

- High-traffic applications
- Privacy-sensitive deployments
- Dynamic workload patterns
- Advanced scalability requirements

**Pros**: Optimal resource use, maximum flexibility, sophisticated routing
**Cons**: High complexity, requires programming, extensive monitoring

---

### [🛠️ Hetzner Deployment (Docker + Caddy)](06-hetzner-deployment.md)

Step-by-step LiveKit deployment on Hetzner, optimized for the fastest path to a secure HTTPS/WSS setup.

**Contents**:

- VPS provisioning checklist
- Docker Compose LiveKit setup
- API key/secret configuration patterns
- Automatic TLS (Let’s Encrypt) with Caddy for WSS
- Public IP and DNS setup
- Log access and troubleshooting

**Best for**:

- Hetzner Cloud VPS deployments
- Quick production pilots and MVPs
- Teams wanting simple upgrades and easy TLS

**Pros**: Fastest secure setup, easy upgrades, simple TLS
**Cons**: Slight container overhead, still manual scaling

---
### [💰 Hetzner Cost & Performance Analysis](07-hetzner-cost-performance-analysis.md)

Comprehensive cost and performance breakdown for LiveKit on Hetzner infrastructure with detailed capacity planning.

**Contents**:

- Single session cost analysis (8 participants, 10 minutes)
- Bandwidth requirements and costs
- Server capacity planning with headroom for spikes
- Multi-host scaling requirements and costs
- Server sizing matrix with Hetzner options
- Large server vs multiple small servers comparison
- Network bandwidth constraint analysis
- Detailed recommendations by scale

**Best for**:

- Financial planning and budgeting
- Capacity planning decisions
- Server sizing selection
- Cost optimization strategies
- Understanding bandwidth constraints

**Key Insights**: Network bandwidth is the primary bottleneck, multiple smaller servers provide better value than one large server, Hetzner's free bandwidth offers 15-20× cost savings vs AWS/GCP

---


## Quick Start Guide

### For First-Time Users

1. **Read the [Executive Summary](00-executive-summary.md)** first
2. Use the decision tree to find your ideal approach
3. Follow the specific guide for your chosen approach
4. Implement monitoring from day one
5. Plan your scaling strategy

### For Different Use Cases

#### 🚀 Startup / MVP

→ Start with [Docker Deployment](02-docker-container-deployment.md)

- Quick setup (1-2 days)
- Low cost ($100-300/month)
- Easy to iterate

#### 🏢 Small Business / SaaS

→ Use [Cloud-Native Deployment](04-cloud-native-deployment.md) (ECS or Cloud Run)

- Managed infrastructure
- Auto-scaling included
- Professional setup

#### 🏛️ Enterprise

→ Deploy with [Kubernetes](03-kubernetes-deployment.md) + [Load Balancing](05-load-balancing-autoscaling.md)

- Full control and flexibility
- Enterprise-grade features
- SLA-ready

#### 🔒 Privacy-First (Healthcare/Finance)

→ Implement [Per-Room Isolation](05-load-balancing-autoscaling.md#per-room-server-isolation)

- Maximum privacy guarantees
- Compliance friendly
- Audit capabilities

#### 🌍 Global Consumer App

→ Multi-cloud with [Cloud-Native](04-cloud-native-deployment.md) + [Auto-Scaling](05-load-balancing-autoscaling.md)

- Global low latency
- Handle viral growth
- Multi-cloud redundancy

---

## Feature Comparison at a Glance

| Feature | Linux Host | Docker | Kubernetes | Cloud-Native | Auto-Scaling |
|---------|------------|--------|------------|--------------|--------------|
| **Setup Time** | 4-8 hours | 2-4 hours | 40-80 hours | 16-32 hours | 80-160 hours |
| **Cost (1K users/month)** | $2,700 | $2,300 | $5,800 | $5,200 | $6,600* |
| **Auto-Scaling** | ❌ | ❌ | ✅ | ✅ | ✅✅ |
| **High Availability** | ❌ | Manual | ✅ | ✅ | ✅ |
| **Multi-Region** | Manual | Manual | ✅ | ✅ | ✅ |
| **Latency** | Lowest | +2-5ms | +5-10ms | +10-20ms | Variable |
| **Operational Overhead** | High | Medium | Medium | Low | Medium |
| **Learning Curve** | Low | Low-Medium | High | Medium | Very High |

*Cost varies significantly with load patterns

---

## Prerequisites by Deployment Type

### Linux Host

- Ubuntu 20.04+ or RHEL 8+
- Root or sudo access
- Basic Linux administration skills
- Static IP or DNS hostname

### Docker

- Docker 20.10+
- Docker Compose 2.0+
- Basic container knowledge
- 4GB+ RAM, 2+ CPU cores

### Kubernetes

- Kubernetes 1.24+
- kubectl CLI
- Helm 3.0+
- Strong K8s knowledge
- Kubernetes cluster (cloud or on-prem)

### Cloud-Native

- Cloud provider account (AWS/GCP/Azure)
- Cloud CLI tools
- Understanding of cloud services
- IAM/RBAC knowledge

### Auto-Scaling

- All of the above
- Programming skills (Python/Go/JavaScript)
- Monitoring expertise
- Advanced networking knowledge

---

## Common Operations

### Deployment

```bash
# Linux Host
sudo systemctl start livekit

# Docker
docker-compose up -d

# Kubernetes
helm install livekit livekit/livekit-server

# Cloud-Native (AWS)
aws ecs update-service --cluster livekit --service livekit --desired-count 3
```

### Scaling

```bash
# Linux Host
# Manual - deploy additional servers

# Docker
docker-compose up -d --scale livekit=3

# Kubernetes
kubectl scale deployment livekit-server --replicas=5

# Cloud-Native (AWS)
aws ecs update-service --cluster livekit --service livekit --desired-count 5
```

### Monitoring

```bash
# Linux Host
sudo journalctl -u livekit -f

# Docker
docker logs -f livekit-server

# Kubernetes
kubectl logs -f deployment/livekit-server

# Cloud-Native (AWS)
aws logs tail /ecs/livekit --follow
```

### Updates

```bash
# Linux Host
sudo systemctl stop livekit
# Replace binary
sudo systemctl start livekit

# Docker
docker-compose pull
docker-compose up -d

# Kubernetes
helm upgrade livekit livekit/livekit-server

# Cloud-Native (AWS)
aws ecs update-service --force-new-deployment
```

---

## Architecture Patterns

### Single Server

```
Internet → [LiveKit Server] → Users
```

**Use for**: MVP, testing, small deployments

### Load Balanced

```
Internet → [Load Balancer] → [LiveKit Servers (3+)] → Users
                            ↓
                         [Redis]
```

**Use for**: Medium deployments, need HA

### Multi-Region

```
Users (US) → [US Region] → LiveKit Cluster
                              ↓
Users (EU) → [EU Region] → LiveKit Cluster → [Global Redis]
                              ↓
Users (Asia) → [Asia Region] → LiveKit Cluster
```

**Use for**: Global applications, low latency requirements

### Per-Room Isolation

```
Room 1 → [Dedicated Server 1]
Room 2 → [Dedicated Server 2]
Room 3 → [Dedicated Server 3]
   ↓
[Central Orchestrator]
```

**Use for**: Privacy-sensitive applications, compliance

---

## Monitoring & Observability

All deployment guides include monitoring setup, but key tools include:

### Metrics

- **Prometheus**: Metrics collection
- **Grafana**: Visualization
- **CloudWatch/Cloud Monitoring**: Cloud-native metrics

### Logging

- **ELK Stack**: Elasticsearch, Logstash, Kibana
- **Loki**: Lightweight log aggregation
- **CloudWatch Logs**: Cloud-native logging

### Tracing

- **Jaeger**: Distributed tracing
- **Zipkin**: Request tracing
- **X-Ray**: AWS tracing

### Alerting

- **Alertmanager**: Prometheus alerting
- **PagerDuty**: Incident management
- **SNS/SQS**: Cloud-native alerting

---

## Security Best Practices

Covered in detail in each guide:

1. **API Key Management**: Use secrets management (Vault, Cloud KMS)
2. **Network Security**: Firewalls, security groups, network policies
3. **TLS/SSL**: Always use HTTPS (Let's Encrypt recommended)
4. **User Isolation**: Run as non-root, use dedicated users
5. **Updates**: Regular security updates and patches
6. **Monitoring**: Security event logging and alerting
7. **Backups**: Regular configuration and data backups
8. **Compliance**: HIPAA, GDPR, SOC 2 considerations

---

## Troubleshooting

Each guide includes detailed troubleshooting sections. Common issues:

### WebRTC Connection Failures

- Check firewall rules (UDP ports 50000-60000)
- Verify public IP configuration
- Test STUN/TURN servers
- Check NAT traversal

### High CPU Usage

- Scale horizontally
- Optimize codec settings
- Check for runaway rooms
- Review participant limits

### Memory Leaks

- Enable room auto-cleanup
- Update to latest version
- Monitor room lifecycle
- Check for zombie connections

### Service Won't Start

- Check configuration syntax
- Verify port availability
- Review logs for errors
- Validate permissions

---

## Performance Tuning

Key areas for optimization:

### Network

- Increase UDP buffer sizes
- Optimize MTU settings
- Enable TCP BBR
- Tune connection tracking

### System

- Increase file descriptors
- Optimize scheduler
- Disable CPU throttling
- Configure NUMA

### Application

- Adjust worker threads
- Optimize codec selection
- Configure bitrate limits
- Room size optimization

---

## Cost Optimization

Strategies across all deployments:

1. **Right-Sizing**: Match resources to actual needs
2. **Auto-Scaling**: Scale down during low traffic
3. **Reserved Capacity**: Use reserved instances/capacity for base load
4. **Spot Instances**: Use spot/preemptible for non-critical workloads
5. **Bandwidth Optimization**: Use CDN for recordings, optimize bitrates
6. **Region Selection**: Choose cost-effective regions
7. **Monitoring**: Track costs and optimize continuously

---

## Support & Community

### Official Resources

- [LiveKit Documentation](https://docs.livekit.io/)
- [LiveKit GitHub](https://github.com/livekit/livekit)
- [LiveKit Community](https://livekit.io/community)

### Getting Help

- GitHub Issues: Bug reports and feature requests
- Slack Community: Real-time help and discussions
- Stack Overflow: Tag `livekit`

### Contributing

- Documentation improvements welcome
- Share your deployment experiences
- Contribute deployment patterns

---

## License

This documentation is provided as-is for informational purposes. LiveKit is licensed under the Apache License 2.0.

---

## Changelog

### Version 1.0.0 (January 2024)

- Initial comprehensive deployment guide
- Coverage of all major deployment approaches
- Detailed cost analysis and comparisons
- Use case recommendations
- Migration paths

---

## Acknowledgments

Based on:

- [Official LiveKit Documentation](https://docs.livekit.io/deploy/)
- [LiveKit GitHub Repository](https://github.com/livekit/livekit)
- Community deployment experiences
- Production deployment best practices

---

## Quick Reference

### Recommended Starting Points

| Your Situation | Start Here | Expected Cost/Month |
|----------------|------------|---------------------|
| Just testing | Docker | $100-300 |
| Small business | Cloud-Native (ECS/Cloud Run) | $500-1,500 |
| Growing startup | Kubernetes | $2,000-10,000 |
| Enterprise | Kubernetes + Auto-Scaling | $8,000-25,000+ |
| Global scale | Multi-Cloud | $30,000+ |

### Next Steps

1. ✅ Read the [Executive Summary](00-executive-summary.md)
2. ✅ Choose your deployment approach
3. ✅ Follow the specific guide
4. ✅ Set up monitoring
5. ✅ Test thoroughly
6. ✅ Document your deployment
7. ✅ Plan for scaling

---

**Ready to deploy? Start with the [Executive Summary](00-executive-summary.md) to find your ideal approach!**
