# LiveKit on Hetzner: Cost and Performance Analysis (Audio-Only Solution)

## Executive Summary

This document provides a comprehensive cost and performance breakdown for deploying LiveKit on Hetzner infrastructure for **AUDIO-ONLY** conferencing sessions, specifically focused on container deployments. It includes detailed calculations for session costs, bandwidth requirements, capacity planning, and server sizing recommendations.

**Important Note:** This analysis is for audio-only conferencing with no video streams. Video conferencing would require significantly different bandwidth and resource calculations.

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
- **Audio:** Opus codec (64 kbps per stream)
- **Video:** None (audio-only conferencing)

### Bandwidth Requirements per Session

#### Per Participant Bandwidth

**Audio Stream Only:**
- Bitrate: 64 kbps per stream
- Each participant sends: 64 kbps
- Each participant receives: 7 × 64 kbps = 448 kbps
- Total per participant: 512 kbps (~0.5 Mbps)

**Note:** Video has been excluded from this analysis as this is an audio-only solution.

#### Server-Side Bandwidth (SFU Architecture)

In LiveKit's SFU (Selective Forwarding Unit) architecture for audio-only:

- **Ingress (receiving from participants):** 8 × 0.064 Mbps = 0.512 Mbps
- **Egress (sending to participants):** 8 × (7 × 0.064 Mbps) = 8 × 0.448 Mbps = 3.584 Mbps
- **Total Session Bandwidth:** ~4 Mbps

**Key Difference:** Audio-only requires approximately 25× less bandwidth than audio+video sessions.

#### Data Transfer per Session

For a 10-minute session:
- Duration: 600 seconds
- Total data: (4 Mbps × 600 seconds) / 8 = 300 MB = **~0.3 GB**

**Breakdown:**
- Ingress: ~0.038 GB
- Egress: ~0.262 GB
- Total: ~0.3 GB per 10-minute audio-only session

**Comparison:** This is 25× less data than video+audio sessions (which would use ~7.5 GB).

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

**Note:** For audio-only, the server time cost remains the same, but CPU/memory usage is significantly lower, allowing many more concurrent sessions per server.

### Total Single Session Cost

**Direct Costs:**
- Server time: €0.0083
- Bandwidth: €0 (included in free tier)
- **Total: €0.0083 (~$0.009 USD) per session**

**Note:** This assumes efficient utilization. Actual cost is significantly lower with multiple concurrent sessions. Audio-only sessions use minimal resources compared to video, allowing much higher concurrency.

---

## Bandwidth Analysis

### Monthly Bandwidth Capacity

#### Scenario: 100 Sessions per Day

**Daily bandwidth:**
- 100 sessions × 0.3 GB = 30 GB/day

**Monthly bandwidth:**
- 30 GB/day × 30 days = 900 GB/month (~0.9 TB/month)

**Hetzner bandwidth:**
- Included: 1 TB/month
- Required: 0.9 TB/month
- **Result: Fully covered by included bandwidth**

#### Impact to Host

**Network Utilization:**
- Peak concurrent sessions: Depends on timing
- Average network load: 0.9 TB / 730 hours = 1.23 GB/hour = **~2.7 Mbps average**
- Peak load (100 concurrent sessions): **~400 Mbps**

**Server Impact (Audio-Only):**
- CPU usage per session: ~2-5% of one core (8 participants, audio only)
- Memory per session: ~50-100 MB
- Network I/O: Minimal compared to video - no longer a bottleneck
- **Primary advantage: CPU and memory usage is dramatically lower without video encoding/decoding**

### Bandwidth Cost Scenarios

| Sessions/Day | Data/Month | Hetzner Cost | AWS Cost (estimate) | GCP Cost (estimate) |
|--------------|------------|--------------|---------------------|---------------------|
| 100 | 0.9 TB | €0 (included) | $82 | $74 |
| 500 | 4.5 TB | €0 (included) | $410 | $368 |
| 1,000 | 9 TB | €0 (included) | $820 | $736 |
| 5,000 | 45 TB | €0 (included) | $4,100 | $3,680 |
| 10,000 | 90 TB | €0 (included) | $8,200 | $7,360 |

**Note:** Hetzner's generous bandwidth policy significantly reduces costs compared to major cloud providers. Audio-only sessions use 25× less bandwidth than video sessions, making even high-volume deployments easily manageable.

---

## Server Capacity Planning

### Capacity per Server Configuration

#### Hetzner CCX23 (4 vCPU, 16GB RAM) - €36.51/month

**Theoretical Capacity (Audio-Only):**
- CPU-limited: ~80-100 concurrent sessions (at 2-5% CPU per session)
- Memory-limited: ~160 concurrent sessions (at 100MB per session)
- Network-limited: ~250 concurrent sessions (at 4 Mbps per session with 1 Gbps NIC)
- **Practical capacity: 60-80 concurrent sessions** (with safety margin)

**Daily Session Capacity (with headroom):**
- Average session duration: 10 minutes
- Sessions per hour: 60 sessions × 6 sessions = 360 sessions/hour
- Daily capacity: 360 × 24 = **8,640 sessions/day**

**With 30% Spike Headroom:**
- Reserved capacity: 70% × 80 = 56 concurrent sessions
- Spike capacity: 30% buffer = 24 sessions
- **Recommended limit: 50-60 concurrent sessions** to handle spikes

**Key Advantage:** Audio-only allows 6-8× more concurrent sessions than video due to minimal CPU, memory, and bandwidth requirements.

#### Adjusted Daily Capacity with Headroom

- Concurrent capacity: 60 sessions
- Sessions per hour: 60 × 6 = 360 sessions/hour
- Daily capacity: 360 × 24 = **8,640 sessions/day**
- Monthly capacity: 8,640 × 30 = **259,200 sessions/month**

### Cost per Session at Scale

**At 70% utilization:**
- Monthly cost: €36.51
- Monthly sessions: 259,200
- **Cost per session: €0.00014 (~$0.00015 USD)**

**Comparison:** Audio-only sessions cost approximately 12× less per session than video sessions at scale due to much higher server capacity.

---

## Multi-Host Scaling

### Expansion Requirements

#### Scenario: Scaling Beyond Single Host

**When to scale:**
- Consistently >60 concurrent sessions
- Peak usage >80 concurrent sessions
- Geographic distribution requirements
- Redundancy/high availability needs

**Note:** For audio-only, a single CCX23 server can handle significantly more load than video sessions, delaying the need for multi-host scaling.

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
- Concurrent sessions: 120-160 (with headroom)
- Daily sessions: ~17,280
- Monthly sessions: ~518,400

**Cost per session:** €0.00014-€0.00016 (~$0.00015-$0.00017 USD)

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
- Concurrent sessions: 180-240 (with headroom)
- Daily sessions: ~25,920
- Monthly sessions: ~777,600

**Cost per session:** €0.00014-€0.00017 (~$0.00015-$0.00018 USD)

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

### Hetzner Cloud Server Options (Audio-Only Performance)

| Server Type | vCPU | RAM | Price/Month | Concurrent Sessions* | Sessions/Day** | Monthly Sessions | Cost/Session |
|-------------|------|-----|-------------|---------------------|----------------|------------------|--------------|
| **CX21** | 2 | 4GB | €5.83 | 20-25 | 2,880-3,600 | 86,400-108,000 | €0.000054-€0.000067 |
| **CX31** | 2 | 8GB | €10.50 | 25-35 | 3,600-5,040 | 108,000-151,200 | €0.000069-€0.000097 |
| **CX41** | 4 | 16GB | €21.00 | 50-70 | 7,200-10,080 | 216,000-302,400 | €0.000069-€0.000097 |
| **CCX23** | 4 | 16GB | €36.51 | 60-80 | 8,640-11,520 | 259,200-345,600 | €0.000106-€0.000141 |
| **CCX33** | 8 | 32GB | €73.02 | 120-160 | 17,280-23,040 | 518,400-691,200 | €0.000106-€0.000141 |
| **CCX43** | 16 | 64GB | €146.04 | 240-320 | 34,560-46,080 | 1,036,800-1,382,400 | €0.000106-€0.000141 |
| **CCX53** | 32 | 128GB | €292.08 | 400-500 | 57,600-72,000 | 1,728,000-2,160,000 | €0.000135-€0.000169 |

*With 30% headroom for spikes (audio-only workload)
**Assuming 10-minute sessions, 6 per hour

**Key Insight:** Audio-only sessions allow 6-10× higher concurrency per server compared to video sessions, dramatically improving cost efficiency.

### Recommended Configurations by Use Case

#### Startup / MVP (< 1,000 sessions/day)

**Recommendation:** Hetzner CX21
- Cost: €5.83/month
- Capacity: 2,880 sessions/day
- Cost per session: €0.000054
- **Total monthly cost: ~€6-10** (including overhead)

#### Small Business (1,000-5,000 sessions/day)

**Recommendation:** Hetzner CX31 or CX41
- Cost: €10.50-21.00/month
- Capacity: 3,600-10,080 sessions/day
- Cost per session: €0.000069-€0.000097
- **Total monthly cost: ~€15-25**

#### Growing Business (5,000-10,000 sessions/day)

**Recommendation:** Hetzner CCX23
- Cost: €36.51/month
- Capacity: 8,640-11,520 sessions/day
- Cost per session: €0.000106-€0.000141
- **Total monthly cost: ~€40-50**

#### Medium Scale (10,000-30,000 sessions/day)

**Recommendation:** 1× Hetzner CCX33 or 2× CCX23
- Cost: €73.02-83/month
- Capacity: 17,280-23,040 sessions/day
- Cost per session: €0.000106-€0.000141
- **Total monthly cost: ~€75-100**

#### Large Scale (30,000+ sessions/day)

**Recommendation:** Multiple CCX33 or CCX43 servers
- Cost: Varies by configuration
- Capacity: 30,000+ sessions/day
- Requires proper load balancing and auto-scaling
- **Total monthly cost: €150-400+**

---

## Bandwidth vs Performance Trade-offs

### One Large Server vs Multiple Small Servers

#### Scenario Comparison: 20,000 Sessions/Day (Audio-Only)

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
- Limited geographic distribution
- No horizontal scalability

**Capacity Analysis:**
- Peak concurrent: ~250 sessions (audio-only)
- Peak bandwidth: 250 × 4 Mbps = 1,000 Mbps (at capacity)
- CPU utilization: ~60-70% with 250 concurrent sessions
- **Actual capacity: ~34,560 sessions/day**
- **Network is NOT a bottleneck** for audio-only sessions

---

**Option B: Three Smaller Servers (3× CCX23)**

**Specifications:**
- Each: 4 vCPU, 16GB RAM
- Total: 12 vCPU, 48GB RAM
- Bandwidth: 1 Gbps per server = 3 Gbps aggregate
- Cost: 3 × €36.51 = €109.53/month

**Advantages:**
- 3× bandwidth capacity (though not needed for audio)
- High availability (2+1 redundancy)
- Better fault isolation
- Can distribute geographically
- Horizontal scaling path
- **Lower total cost than single large server**

**Disadvantages:**
- More complex management
- Requires load balancer
- Need Redis for session sharing
- Higher operational overhead

**Capacity Analysis:**
- Peak concurrent: ~180 sessions (60 per server)
- Peak bandwidth: 180 × 4 Mbps = 720 Mbps (distributed)
- Each server uses ~240 Mbps
- **Actual capacity: ~25,920 sessions/day**

---

### Bandwidth Constraint Analysis

#### Network Throughput Limits (Audio-Only)

**Hetzner Server Network:**
- Shared 1 Gbps port per server
- Actual usable: ~800-900 Mbps (accounting for overhead)
- Recommended sustained: ~600-700 Mbps (70% of capacity)

**Session Capacity by Network (Audio-Only):**
- At 4 Mbps per session: 150-175 sessions per server
- At 6 Mbps per session (higher quality): 100-116 sessions per server
- At 2 Mbps per session (lower quality): 300-350 sessions per server

**Critical Difference:** Unlike video sessions, audio-only workloads are **CPU-limited**, not network-limited.

#### Bottleneck Comparison Table (Audio-Only)

| Configuration | Total vCPU | Total RAM | Network Capacity | CPU Limit* | Network Limit** | Bottleneck | Effective Capacity |
|---------------|------------|-----------|------------------|------------|-----------------|------------|-------------------|
| 1× CCX43 | 16 | 64GB | 1 Gbps | ~250 sessions | ~175 sessions | **CPU** | 175 sessions |
| 2× CCX23 | 8 | 32GB | 2 Gbps | ~120 sessions | ~350 sessions | **CPU** | 120 sessions |
| 3× CCX23 | 12 | 48GB | 3 Gbps | ~180 sessions | ~525 sessions | **CPU** | 180 sessions |
| 4× CX41 | 16 | 64GB | 4 Gbps | ~240 sessions | ~700 sessions | **CPU** | 240 sessions |

*Assuming 3-5% CPU per session with 30% headroom (audio-only)
**Assuming 4 Mbps per session at 70% network utilization

### Key Finding: CPU is the Primary Bottleneck for Audio-Only

**Critical Insight:** For audio-only sessions with 8 participants, CPU becomes the limiting factor, not network bandwidth. This is the opposite of video sessions.

**Recommendation for Audio-Only:** 
- **Single larger servers can be cost-effective** since network is not a constraint
- CPU cores are the primary resource to optimize
- Consider CCX series (dedicated CPU) over CX series for better performance
- Multiple smaller servers still provide redundancy benefits but less critical for bandwidth

---

## Recommendations

### Small Scale (< 5,000 sessions/day)

**Recommended Setup:**
- **Server:** Hetzner CX41 (4 vCPU, 16GB RAM)
- **Deployment:** Docker Compose (from guide 06)
- **Monitoring:** Basic (logs + Caddy)
- **Monthly Cost:** €25-30
- **Cost per session:** €0.003-0.004

**Rationale:**
- Single server simplicity
- Adequate capacity with headroom
- Easy to manage
- Cost-effective for audio-only workloads

### Medium Scale (5,000-20,000 sessions/day)

**Recommended Setup:**
- **Servers:** 1× Hetzner CCX23 or CCX33
- **Deployment:** Docker Compose
- **Monitoring:** Prometheus + Grafana (optional for single server)
- **Monthly Cost:** €40-75
- **Cost per session:** €0.002-0.004

**Rationale:**
- Single server can handle this load for audio-only
- No need for load balancing yet
- Simple management
- Excellent cost efficiency

### Large Scale (20,000+ sessions/day)

**Recommended Setup:**
- **Servers:** 2-4× Hetzner CCX33 or 1-2× CCX43
- **Load Balancer:** HAProxy/NGINX on small instance or integrated
- **State:** Redis for session sharing (if multi-server)
- **Deployment:** Docker Compose or Kubernetes for very large scale
- **Monitoring:** Full Prometheus + Grafana + alerting
- **Monthly Cost:** €150-350+
- **Cost per session:** €0.002-0.005

**Rationale:**
- High availability through redundancy
- Geographic distribution option
- CPU capacity for high concurrency
- Professional monitoring
- Room for growth

### Cost Efficiency Recommendations

1. **Start Small, Scale Vertically First:**
   - Begin with CX41 or CCX23
   - Upgrade to larger CPU instances as needed
   - Audio-only allows vertical scaling to be effective
   - Add multiple servers only for redundancy/geography

2. **CPU Optimization (Primary Concern for Audio):**
   - Choose dedicated CPU instances (CCX series) for predictable performance
   - Monitor CPU usage as primary capacity metric
   - Network bandwidth is rarely a concern for audio-only

3. **Session Optimization:**
   - Use Opus codec (64 kbps default is excellent)
   - Consider adaptive bitrate for varying network conditions
   - Audio quality settings have minimal impact on server resources

4. **Monitoring is Essential:**
   - Track concurrent sessions
   - Monitor CPU utilization per core
   - Set up alerts for 70% CPU capacity thresholds
   - Network monitoring is less critical than for video

5. **Reserved Capacity:**
   - Hetzner doesn't offer reserved pricing
   - Use auto-scaling for cost control (if using Kubernetes)
   - Consider time-based scaling for predictable loads
   - Single server can handle surprisingly high loads for audio

### Audio Quality Trade-offs

| Quality | Codec Settings | Bitrate | Bandwidth/Session (8p) | Sessions/Server* | Use Case |
|---------|---------------|---------|------------------------|------------------|----------|
| Low (Voice) | Opus @ 32 kbps | 32 kbps | ~2 Mbps | 300-350 | Basic voice calls |
| Standard | Opus @ 64 kbps | 64 kbps | ~4 Mbps | 150-175 | Default conferencing |
| High | Opus @ 96 kbps | 96 kbps | ~6 Mbps | 100-116 | High-quality audio |
| Premium | Opus @ 128 kbps | 128 kbps | ~8 Mbps | 75-87 | Music/professional |

*Based on CCX23 with CPU as primary constraint

**Note:** Audio quality has minimal impact on server resources. The differences are primarily in network bandwidth, which is rarely a bottleneck. Even premium quality audio uses far less bandwidth than video.

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

1. **Single Session Cost:** ~€0.008 (less than 1 cent) for raw compute, but amortized cost at scale is ~€0.00014
2. **Bandwidth is Essentially Free:** Hetzner's generous policy + audio-only's low bandwidth means no bandwidth concerns
3. **CPU is the Bottleneck:** Not network bandwidth (unlike video sessions)
4. **Single Larger Servers are Viable:** Network bottleneck eliminated for audio-only
5. **Cost per Session at Scale:** €0.00014-€0.00017 at 70% utilization
6. **Audio-Only Advantage:** 6-10× higher concurrency per server compared to video sessions

### Capacity Summary (Audio-Only)

| Configuration | Monthly Cost | Sessions/Day | Cost/Session | Best For |
|---------------|--------------|--------------|--------------|----------|
| 1× CX21 | €6-10 | 2,000-3,000 | €0.002-0.003 | Startup/Testing |
| 1× CX41 | €25-30 | 7,000-10,000 | €0.0025-0.003 | Small Business |
| 1× CCX23 | €40-50 | 8,000-11,000 | €0.0036-0.005 | Growing Business |
| 1× CCX33 | €75-85 | 17,000-23,000 | €0.0033-0.0044 | Medium Scale |
| 2× CCX33 | €150-165 | 34,000-46,000 | €0.0033-0.0044 | Large Scale |
| Kubernetes | €250-500 | 80,000+ | €0.003-0.005 | Enterprise |

### Final Recommendations

1. **Start with:** Hetzner CX41 or CCX23 + Docker Compose + Caddy (Guide 06)
2. **Scale to:** Single larger server first (CCX33/CCX43), then multiple servers for redundancy
3. **Monitor:** CPU utilization as primary constraint (not network)
4. **Optimize:** Use standard Opus codec (64 kbps), enable adaptive bitrate if needed
5. **Plan:** Upgrade server when >70% CPU utilization consistently

### Cost Advantages vs Cloud Providers

Compared to AWS/GCP for the same audio-only workload (20,000 sessions/day):
- **Hetzner:** €75-100/month
- **AWS:** ~€400-600/month (bandwidth + compute costs)
- **GCP:** ~€350-550/month (bandwidth + compute costs)

**Hetzner provides 5-7× cost savings for audio-only, with bandwidth being free and compute being significantly cheaper.**

### Audio-Only vs Video Comparison

**Key Differences:**
- **Bandwidth:** 25× less (4 Mbps vs 100 Mbps per session)
- **CPU Usage:** 5-10× less per session
- **Concurrency:** 6-10× more sessions per server
- **Cost per Session:** 10-15× lower at scale
- **Bottleneck:** CPU-limited instead of network-limited
- **Scaling Strategy:** Vertical scaling viable; horizontal for redundancy

**Recommendation:** Audio-only conferencing is dramatically more cost-effective and can handle significantly higher scale on modest hardware.
