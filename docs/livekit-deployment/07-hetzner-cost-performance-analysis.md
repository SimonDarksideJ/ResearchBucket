# LiveKit on Hetzner: Cost and Performance Analysis

## Executive Summary

This document provides a comprehensive cost and performance breakdown for deploying LiveKit on Hetzner infrastructure, specifically focused on container deployments. It includes detailed calculations for session costs, bandwidth requirements, capacity planning, and server sizing recommendations.

## Table of Contents

1. [Session Cost Analysis](#session-cost-analysis)
2. [Bandwidth Analysis](#bandwidth-analysis)
3. [Server Capacity Planning](#server-capacity-planning)
4. [Multi-Host Scaling](#multi-host-scaling)
5. [Server Sizing Matrix](#server-sizing-matrix)
6. [Bandwidth vs Performance Trade-offs](#bandwidth-vs-performance-trade-offs)
7. [Recommendations](#recommendations)

---

## Session Cost Analysis

### Single Session Specifications

**Session Parameters:**
- **Participants:** 8 people
- **Duration:** 10 minutes
- **Video Quality:** Standard (720p @ 30fps)
- **Audio:** Opus codec (64 kbps per stream)

### Bandwidth Requirements per Session

#### Per Participant Bandwidth

**Video Stream (720p):**
- Bitrate: ~1.5 Mbps (typical for 720p @ 30fps with VP8/H.264)
- Each participant sends: 1.5 Mbps
- Each participant receives: 7 × 1.5 Mbps = 10.5 Mbps (all other participants)
- Total per participant: 12 Mbps

**Audio Stream:**
- Bitrate: 64 kbps per stream
- Each participant sends: 64 kbps
- Each participant receives: 7 × 64 kbps = 448 kbps
- Total per participant: 512 kbps (~0.5 Mbps)

**Total Bandwidth per Participant:** ~12.5 Mbps

#### Server-Side Bandwidth (SFU Architecture)

In LiveKit's SFU (Selective Forwarding Unit) architecture:

- **Ingress (receiving from participants):** 8 × (1.5 Mbps video + 0.064 Mbps audio) = 8 × 1.564 Mbps = 12.512 Mbps
- **Egress (sending to participants):** 8 × (7 × 1.5 Mbps video + 7 × 0.064 Mbps audio) = 8 × 10.948 Mbps = 87.584 Mbps
- **Total Session Bandwidth:** ~100 Mbps

#### Data Transfer per Session

For a 10-minute session:
- Duration: 600 seconds
- Total data: (100 Mbps × 600 seconds) / 8 = 7,500 MB = **~7.5 GB**

**Breakdown:**
- Ingress: ~0.94 GB
- Egress: ~6.56 GB
- Total: ~7.5 GB per 10-minute session

### Cost per Session

#### Hetzner Bandwidth Costs

Hetzner Cloud includes:
- **Free bandwidth:** 1 TB per month for most instance types
- **Additional bandwidth:** Free for now (Hetzner typically includes generous bandwidth)
- **Overage charges:** Generally not charged for reasonable usage

#### Server Cost Allocation

For a **Hetzner CCX23** server (4 vCPU, 16GB RAM, €36.51/month):

**Assumptions:**
- Server runs 24/7
- Total hours per month: 730 hours
- Cost per hour: €0.05

**Cost per 10-minute session:**
- Time fraction: 10 minutes / 43,800 minutes = 0.0228%
- Session cost: €36.51 × 0.000228 = **€0.0083** (~$0.009 USD)

### Total Single Session Cost

**Direct Costs:**
- Server time: €0.0083
- Bandwidth: €0 (included in free tier)
- **Total: €0.0083 (~$0.009 USD) per session**

**Note:** This assumes efficient utilization. Actual cost is lower with multiple concurrent sessions.

---

## Bandwidth Analysis

### Monthly Bandwidth Capacity

#### Scenario: 100 Sessions per Day

**Daily bandwidth:**
- 100 sessions × 7.5 GB = 750 GB/day

**Monthly bandwidth:**
- 750 GB/day × 30 days = 22.5 TB/month

**Hetzner bandwidth:**
- Included: 1 TB/month
- Required: 22.5 TB/month
- **Overage: 21.5 TB** (typically included for free by Hetzner)

#### Impact to Host

**Network Utilization:**
- Peak concurrent sessions: Depends on timing
- Average network load: 22.5 TB / 730 hours = 31 GB/hour = **~69 Mbps average**
- Peak load (10 concurrent sessions): **~1 Gbps**

**Server Impact:**
- CPU usage per session: ~15-25% of one core (8 participants)
- Memory per session: ~200-400 MB
- Network I/O: Primary bottleneck at scale

### Bandwidth Cost Scenarios

| Sessions/Day | Data/Month | Hetzner Cost | AWS Cost (estimate) | GCP Cost (estimate) |
|--------------|------------|--------------|---------------------|---------------------|
| 10 | 2.25 TB | €0 (included) | $205 | $184 |
| 50 | 11.25 TB | €0 (included) | $1,025 | $920 |
| 100 | 22.5 TB | €0 (included) | $2,050 | $1,840 |
| 500 | 112.5 TB | €0 (included) | $10,250 | $9,200 |

**Note:** Hetzner's generous bandwidth policy significantly reduces costs compared to major cloud providers.

---

## Server Capacity Planning

### Capacity per Server Configuration

#### Hetzner CCX23 (4 vCPU, 16GB RAM) - €36.51/month

**Theoretical Capacity:**
- CPU-limited: ~12-16 concurrent sessions (at 25% CPU per session)
- Memory-limited: ~40 concurrent sessions (at 400MB per session)
- Network-limited: ~10 concurrent sessions (at 100 Mbps per session with 1 Gbps NIC)
- **Practical capacity: 8-10 concurrent sessions**

**Daily Session Capacity (with headroom):**
- Average session duration: 10 minutes
- Sessions per hour: 8 servers × 6 sessions = 48 sessions/hour
- Daily capacity: 48 × 24 = **1,152 sessions/day**

**With 30% Spike Headroom:**
- Reserved capacity: 70% × 8 = 5.6 concurrent sessions
- Spike capacity: 30% buffer = 2.4 sessions
- **Recommended limit: 5-6 concurrent sessions** to handle spikes

#### Adjusted Daily Capacity with Headroom

- Concurrent capacity: 5 sessions
- Sessions per hour: 5 × 6 = 30 sessions/hour
- Daily capacity: 30 × 24 = **720 sessions/day**
- Monthly capacity: 720 × 30 = **21,600 sessions/month**

### Cost per Session at Scale

**At 70% utilization:**
- Monthly cost: €36.51
- Monthly sessions: 21,600
- **Cost per session: €0.00169 (~$0.0018 USD)**

---

## Multi-Host Scaling

### Expansion Requirements

#### Scenario: Scaling Beyond Single Host

**When to scale:**
- Consistently >6 concurrent sessions
- Peak usage >8 concurrent sessions
- Geographic distribution requirements
- Redundancy/high availability needs

#### Two-Server Architecture

**Configuration:**
- 2× Hetzner CCX23 servers
- Load balancer: HAProxy or NGINX
- Shared Redis for session state
- DNS-based failover or active-active

**Costs:**
- Servers: 2 × €36.51 = €73.02/month
- Redis (managed): €10-20/month or self-hosted €5/month
- Load balancer: Can run on separate small instance €5/month or co-located
- **Total: ~€83-98/month**

**Capacity:**
- Concurrent sessions: 10-12 (with headroom)
- Daily sessions: ~1,440
- Monthly sessions: ~43,200

**Cost per session:** €0.00192-€0.00227 (~$0.002-$0.0025 USD)

#### Three-Server Architecture

**Configuration:**
- 3× Hetzner CCX23 servers
- Load balancer + Redis
- True high availability
- Geographic distribution option

**Costs:**
- Servers: 3 × €36.51 = €109.53/month
- Redis + LB: €15-25/month
- **Total: ~€125-135/month**

**Capacity:**
- Concurrent sessions: 15-18 (with headroom)
- Daily sessions: ~2,160
- Monthly sessions: ~64,800

**Cost per session:** €0.00193-€0.00208 (~$0.002-$0.0023 USD)

### Additional Requirements for Multi-Host

1. **Load Balancer:**
   - Can use smallest Hetzner instance (CX11: €3.79/month)
   - Or run on one of the LiveKit servers
   - HAProxy or NGINX configuration (see guide 05)

2. **Session State Management:**
   - Redis required for multi-server deployments
   - Options:
     - Self-hosted on small instance: €5/month
     - Managed Redis (Upstash): ~€10-20/month
     - Can run on same server initially

3. **Networking:**
   - Private networking: Free on Hetzner Cloud
   - No data transfer charges between servers in same datacenter
   - SSL certificates: Let's Encrypt (free)

4. **Monitoring:**
   - Essential for multi-server deployments
   - Prometheus + Grafana: Free (self-hosted)
   - Estimated overhead: €5-10/month for monitoring server

### Session Affinity Considerations

**Important:** LiveKit sessions must remain on the same server (room stickiness).

**Approaches:**
1. **Client IP-based stickiness** (HAProxy/NGINX)
2. **Room ID-based routing** (application-level)
3. **Subdomain-based routing** (DNS)

**Limitations:**
- Cannot split a single session across multiple servers
- All participants in a room must connect to same server
- Load balancer must maintain sticky sessions

---

## Server Sizing Matrix

### Hetzner Cloud Server Options

| Server Type | vCPU | RAM | Price/Month | Concurrent Sessions* | Sessions/Day** | Monthly Sessions | Cost/Session |
|-------------|------|-----|-------------|---------------------|----------------|------------------|--------------|
| **CX21** | 2 | 4GB | €5.83 | 2-3 | 288-432 | 8,640-12,960 | €0.00045-€0.00067 |
| **CX31** | 2 | 8GB | €10.50 | 3-4 | 432-576 | 12,960-17,280 | €0.00061-€0.00081 |
| **CX41** | 4 | 16GB | €21.00 | 6-8 | 864-1,152 | 25,920-34,560 | €0.00061-€0.00081 |
| **CCX23** | 4 | 16GB | €36.51 | 8-10 | 1,152-1,440 | 34,560-43,200 | €0.00085-€0.00106 |
| **CCX33** | 8 | 32GB | €73.02 | 16-20 | 2,304-2,880 | 69,120-86,400 | €0.00085-€0.00106 |
| **CCX43** | 16 | 64GB | €146.04 | 30-35 | 4,320-5,040 | 129,600-151,200 | €0.00097-€0.00113 |
| **CCX53** | 32 | 128GB | €292.08 | 50-60 | 7,200-8,640 | 216,000-259,200 | €0.00113-€0.00135 |

*With 30% headroom for spikes  
**Assuming 10-minute sessions, 6 per hour

### Recommended Configurations by Use Case

#### Startup / MVP (< 100 sessions/day)

**Recommendation:** Hetzner CX21
- Cost: €5.83/month
- Capacity: 288 sessions/day
- Cost per session: €0.00045
- **Total monthly cost: ~€6-10** (including overhead)

#### Small Business (100-500 sessions/day)

**Recommendation:** Hetzner CX41
- Cost: €21.00/month
- Capacity: 864 sessions/day
- Cost per session: €0.00061
- **Total monthly cost: ~€25-30**

#### Growing Business (500-1,000 sessions/day)

**Recommendation:** Hetzner CCX23
- Cost: €36.51/month
- Capacity: 1,152 sessions/day
- Cost per session: €0.00085
- **Total monthly cost: ~€40-50**

#### Medium Scale (1,000-3,000 sessions/day)

**Recommendation:** 2× Hetzner CCX23 or 1× CCX33
- Cost: €73.02-83/month
- Capacity: 2,304-2,880 sessions/day
- Cost per session: €0.00085-€0.00192
- **Total monthly cost: ~€80-100**

#### Large Scale (3,000+ sessions/day)

**Recommendation:** Multiple CCX33 or CCX43 servers
- Cost: Varies by configuration
- Capacity: 4,000+ sessions/day
- Requires proper load balancing and auto-scaling
- **Total monthly cost: €150-500+**

---

## Bandwidth vs Performance Trade-offs

### One Large Server vs Multiple Small Servers

#### Scenario Comparison: 2,000 Sessions/Day

**Option A: One Large Server (CCX43)**

**Specifications:**
- 16 vCPU, 64GB RAM
- Bandwidth: 1 Gbps shared port
- Cost: €146.04/month

**Advantages:**
- Lower complexity
- No load balancing needed
- Single point of management
- Better cost per session initially

**Disadvantages:**
- Single point of failure
- Bandwidth bottleneck: 1 Gbps shared
- Limited geographic distribution
- No horizontal scalability
- Network becomes bottleneck before CPU

**Bandwidth Analysis:**
- Peak concurrent: 30 sessions
- Peak bandwidth: 30 × 100 Mbps = 3 Gbps required
- **Network limited: Maximum ~10 concurrent sessions reliably**
- Actual capacity: ~1,440 sessions/day (not 4,320)

---

**Option B: Three Smaller Servers (3× CCX23)**

**Specifications:**
- Each: 4 vCPU, 16GB RAM
- Total: 12 vCPU, 48GB RAM
- Bandwidth: 1 Gbps per server = 3 Gbps aggregate
- Cost: 3 × €36.51 = €109.53/month

**Advantages:**
- 3× bandwidth capacity
- High availability (2+1 redundancy)
- Better fault isolation
- Can distribute geographically
- Horizontal scaling path
- Each server handles its own 1 Gbps

**Disadvantages:**
- More complex management
- Requires load balancer
- Need Redis for session sharing
- Higher operational overhead

**Bandwidth Analysis:**
- Peak concurrent: 15 sessions (5 per server)
- Peak bandwidth: 15 × 100 Mbps = 1.5 Gbps (distributed)
- **Network optimized: Each server uses ~500 Mbps**
- Actual capacity: ~2,160 sessions/day

---

### Bandwidth Constraint Analysis

#### Network Throughput Limits

**Hetzner Server Network:**
- Shared 1 Gbps port per server
- Actual usable: ~800-900 Mbps (accounting for overhead)
- Recommended sustained: ~600-700 Mbps (70% of capacity)

**Session Capacity by Network:**
- At 100 Mbps per session: 6-7 sessions per server
- At 150 Mbps per session (1080p): 4-5 sessions per server
- At 50 Mbps per session (480p): 12-14 sessions per server

#### Bandwidth Bottleneck Comparison Table

| Configuration | Total vCPU | Total RAM | Network Capacity | CPU Limit* | Network Limit** | Bottleneck | Effective Capacity |
|---------------|------------|-----------|------------------|------------|-----------------|------------|-------------------|
| 1× CCX43 | 16 | 64GB | 1 Gbps | ~30 sessions | ~8 sessions | **Network** | 8 sessions |
| 2× CCX23 | 8 | 32GB | 2 Gbps | ~16 sessions | ~16 sessions | Balanced | 16 sessions |
| 3× CCX23 | 12 | 48GB | 3 Gbps | ~24 sessions | ~24 sessions | Balanced | 24 sessions |
| 4× CX41 | 16 | 64GB | 4 Gbps | ~24 sessions | ~32 sessions | **CPU** | 24 sessions |

*Assuming 25% CPU per session with 30% headroom  
**Assuming 100 Mbps per session at 70% network utilization

### Key Finding: Network is the Primary Bottleneck

**Critical Insight:** For video sessions with 8 participants, network bandwidth becomes the limiting factor before CPU or memory on larger servers.

**Recommendation:** 
- **Multiple smaller servers provide better price/performance**
- Better network utilization (3× 1Gbps > 1× 1Gbps)
- Built-in redundancy
- Horizontal scaling capability
- More cost-effective at scale

---

## Recommendations

### Small Scale (< 500 sessions/day)

**Recommended Setup:**
- **Server:** Hetzner CX41 (4 vCPU, 16GB RAM)
- **Deployment:** Docker Compose (from guide 06)
- **Monitoring:** Basic (logs + Caddy)
- **Monthly Cost:** €25-30
- **Cost per session:** €0.06-0.08

**Rationale:**
- Single server simplicity
- Adequate capacity with headroom
- Easy to manage
- Cost-effective

### Medium Scale (500-2,000 sessions/day)

**Recommended Setup:**
- **Servers:** 2× Hetzner CCX23
- **Load Balancer:** HAProxy on CX11
- **State:** Redis (self-hosted on one server)
- **Deployment:** Docker Compose + HAProxy
- **Monitoring:** Prometheus + Grafana
- **Monthly Cost:** €80-100
- **Cost per session:** €0.05-0.06

**Rationale:**
- Addresses network bottleneck
- High availability
- Room for growth
- Better than single large server

### Large Scale (2,000+ sessions/day)

**Recommended Setup:**
- **Servers:** 3-5× Hetzner CCX23 or 2-3× CCX33
- **Load Balancer:** Dedicated HAProxy/NGINX
- **State:** Managed Redis or dedicated instance
- **Deployment:** Kubernetes or advanced Docker setup
- **Monitoring:** Full Prometheus + Grafana + alerting
- **Monthly Cost:** €150-300+
- **Cost per session:** €0.04-0.05

**Rationale:**
- Kubernetes for orchestration
- Multi-region option
- Auto-scaling capability
- Professional monitoring
- Optimal network utilization

### Cost Efficiency Recommendations

1. **Start Small, Scale Horizontally:**
   - Begin with CX41 or CCX23
   - Add servers as needed
   - Avoid over-provisioning

2. **Network Optimization:**
   - Use multiple smaller servers vs one large
   - Distribute load for better bandwidth utilization
   - Monitor network saturation

3. **Session Optimization:**
   - Default to 720p video (can handle 1080p for smaller groups)
   - Enable simulcast for larger rooms
   - Use adaptive bitrate

4. **Monitoring is Essential:**
   - Track concurrent sessions
   - Monitor network utilization
   - Set up alerts for capacity thresholds

5. **Reserved Capacity:**
   - Hetzner doesn't offer reserved pricing
   - Use auto-scaling for cost control
   - Consider time-based scaling for predictable loads

### Video Quality Trade-offs

| Quality | Resolution | Bitrate | Bandwidth/Session (8p) | Sessions/Server* |
|---------|-----------|---------|------------------------|------------------|
| Low (Mobile) | 480p @ 15fps | 0.5 Mbps | ~40 Mbps | 15-18 |
| Standard | 720p @ 30fps | 1.5 Mbps | ~100 Mbps | 6-8 |
| High | 1080p @ 30fps | 3 Mbps | ~200 Mbps | 3-4 |
| Ultra | 1080p @ 60fps | 6 Mbps | ~400 Mbps | 1-2 |

*Based on CCX23 with network bottleneck consideration

### Deployment Environment Options

#### Docker Compose (Recommended for Most)

**Pros:**
- Simple deployment
- Easy updates (docker-compose pull && up -d)
- Automatic TLS with Caddy
- Good for 1-3 servers

**Cons:**
- Manual scaling
- Limited orchestration

**Best for:** Small to medium deployments

#### Kubernetes

**Pros:**
- Auto-scaling
- Self-healing
- Advanced orchestration
- Multi-region support

**Cons:**
- Complex setup
- Higher overhead
- Requires expertise

**Best for:** Large scale, >5 servers, enterprise

#### Bare Metal (systemd)

**Pros:**
- Maximum performance
- No container overhead
- Lowest latency

**Cons:**
- Manual management
- Complex updates
- No containerization benefits

**Best for:** Specific performance requirements, experienced teams

### Monitoring Requirements

#### Essential Metrics

1. **System Metrics:**
   - CPU usage per core
   - Memory utilization
   - Network throughput (in/out)
   - Disk I/O

2. **LiveKit Metrics:**
   - Active rooms
   - Active participants
   - Concurrent sessions
   - Bitrate per participant

3. **Network Metrics:**
   - Bandwidth utilization
   - Packet loss
   - Jitter
   - RTT (round-trip time)

#### Alerting Thresholds

- CPU usage > 70% sustained
- Memory usage > 80%
- Network utilization > 70%
- Packet loss > 1%
- Active sessions > 70% capacity

---

## Summary

### Key Findings

1. **Single Session Cost:** ~€0.008 (less than 1 cent) for raw compute
2. **Bandwidth is Free:** Hetzner's generous policy eliminates bandwidth costs
3. **Network is the Bottleneck:** Not CPU or RAM for video sessions
4. **Multiple Small Servers > One Large:** Better bandwidth utilization
5. **Cost per Session at Scale:** €0.04-0.06 at 70% utilization

### Capacity Summary

| Configuration | Monthly Cost | Sessions/Day | Cost/Session | Best For |
|---------------|--------------|--------------|--------------|----------|
| 1× CX41 | €25-30 | 500-800 | €0.06-0.08 | Startup/Testing |
| 1× CCX23 | €40-50 | 800-1,200 | €0.05-0.06 | Small Business |
| 2× CCX23 | €80-100 | 1,500-2,000 | €0.05-0.06 | Growing Business |
| 3× CCX23 | €120-140 | 2,000-3,000 | €0.04-0.05 | Medium Scale |
| Kubernetes | €200-400 | 5,000+ | €0.04-0.05 | Enterprise |

### Final Recommendations

1. **Start with:** Hetzner CCX23 + Docker Compose + Caddy (Guide 06)
2. **Scale to:** Multiple CCX23 servers with load balancing
3. **Monitor:** Network bandwidth as primary constraint
4. **Optimize:** Use 720p default, enable simulcast, adaptive bitrate
5. **Plan:** Add server capacity when >70% network utilization

### Cost Advantages vs Cloud Providers

Compared to AWS/GCP for the same workload:
- **Hetzner:** €100/month (2,000 sessions/day)
- **AWS:** ~€1,500-2,000/month (bandwidth costs)
- **GCP:** ~€1,200-1,800/month (bandwidth costs)

**Hetzner provides 15-20× cost savings primarily due to free bandwidth.**
