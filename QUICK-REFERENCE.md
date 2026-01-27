# Quick Reference: Costs & External Setup

This document provides a quick reference for costs and external service setup for LiveKit deployments.

## 💰 Cost Summary

### Mac Deployment: $0/month

| Component | Cost | Required |
|-----------|------|----------|
| macOS, Docker, LiveKit, Monitoring | **FREE** | Yes |
| Cloudflare Tunnel (Public Access) | **FREE** | Optional |
| Domain Name | $6-15/year | Optional |
| External Storage | $30-70 one-time | Recommended |
| **Total Monthly Cost** | **$0** | - |

### Hetzner Deployment: ~$13/month

| Component | Cost | Required |
|-----------|------|----------|
| Hetzner CX31 Server | €11/month (~$12) | Yes |
| Domain Name | $6-15/year (~$1/month) | Yes |
| Cloudflare DNS | **FREE** | Yes |
| Let's Encrypt SSL | **FREE** (via Caddy) | Yes |
| **Total Monthly Cost** | **~$13** | - |

**What's Included in Hetzner:**
- 2 vCPU, 8GB RAM, 80GB NVMe
- 20TB bandwidth
- IPv4 + IPv6
- DDoS protection
- Cloud firewall

**What's NOT Included:**
- DNS hosting (use Cloudflare - free)
- Domain name (buy separately)
- SSL certificates (use Let's Encrypt - free)

## 🔗 External Services Quick Links

### Required for Mac Deployment

1. **Homebrew** (Package Manager)
   - Cost: FREE
   - Link: [brew.sh](https://brew.sh)
   - Install: `/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"`

2. **Docker Desktop**
   - Cost: FREE (personal use)
   - Link: [docker.com/products/docker-desktop](https://www.docker.com/products/docker-desktop)
   - Download: Choose Apple Silicon or Intel version

3. **Cloudflare Tunnel** (Optional - for public access)
   - Cost: FREE unlimited
   - Link: [developers.cloudflare.com/cloudflare-one/connections/connect-apps](https://developers.cloudflare.com/cloudflare-one/connections/connect-apps)
   - Install: `brew install cloudflare/cloudflare/cloudflared`

### Required for Hetzner Deployment

1. **Hetzner Cloud Account**
   - Cost: FREE to create
   - Link: [console.hetzner.cloud](https://console.hetzner.cloud)
   - Bonus: €20 credit for new accounts (if available)

2. **Domain Registrar** (Choose one)
   - **Cloudflare Registrar**: ~$9/year - [cloudflare.com/products/registrar](https://www.cloudflare.com/products/registrar/)
   - **Namecheap**: ~$9-13/year - [namecheap.com](https://www.namecheap.com)
   - **Porkbun**: ~$6-10/year - [porkbun.com](https://porkbun.com)

3. **DNS Provider** (Choose one - all FREE)
   - **Cloudflare DNS**: FREE - [cloudflare.com](https://www.cloudflare.com) (Recommended)
   - **Your Registrar**: Usually FREE with domain
   - **Hetzner DNS**: FREE - [docs.hetzner.com/dns-console](https://docs.hetzner.com/dns-console/)

4. **SSL Certificate**
   - Let's Encrypt: FREE (automated by Caddy)
   - No signup or configuration needed

## 📋 DNS Setup Cheat Sheet

### For Hetzner Deployment

**Step 1: Get Your Server IP**
```bash
# After creating Hetzner server, note the IP
# Example: 95.217.123.456
```

**Step 2: Add DNS Records**

Using Cloudflare (Recommended):
```
Type: A
Name: livekit
IPv4: YOUR_HETZNER_IP
Proxy: OFF (gray cloud) ← Important for WebRTC!
TTL: Auto
```

```
Type: A
Name: grafana
IPv4: YOUR_HETZNER_IP
Proxy: OFF (gray cloud)
TTL: Auto
```

**Step 3: Verify DNS**
```bash
dig livekit.yourdomain.com +short
# Should output: YOUR_HETZNER_IP

nslookup livekit.yourdomain.com
# Should show your IP
```

**Step 4: Wait for SSL**
- After deployment, Caddy automatically requests SSL certificates
- Takes 2-3 minutes if DNS is configured correctly
- Access via: `https://livekit.yourdomain.com`

### For Mac with Cloudflare Tunnel

**Quick Tunnel (No DNS needed):**
```bash
cloudflared tunnel --url http://localhost:7880
# Gives you: https://random-subdomain.trycloudflare.com
```

**Custom Domain (Requires DNS):**
```bash
# 1. Create tunnel
cloudflared tunnel create my-livekit

# 2. Add CNAME in Cloudflare DNS
# Name: livekit
# Target: [TUNNEL-ID].cfargotunnel.com
# Proxy: ON (orange cloud)

# 3. Route and run
cloudflared tunnel route dns my-livekit livekit.yourdomain.com
cloudflared tunnel run my-livekit
```

## 🆚 When to Use Mac vs Hetzner

### Use Mac When:
- 💻 Developing and testing
- 👥 Supporting 5-20 concurrent users
- 💰 Want zero ongoing costs
- 🏠 Have reliable home internet (50+ Mbps upload)
- ⏰ Don't need 24/7 uptime

### Use Hetzner When:
- 🚀 Running production service
- 👥 Supporting 50-200+ concurrent users
- ⏰ Need 24/7 reliability
- 🌍 Need EU datacenter location
- 📊 Want professional SLA guarantees
- 📈 Need to scale easily

### Hybrid Approach:
1. **Develop on Mac** (FREE)
2. **Test on Mac** with Cloudflare Tunnel (FREE)
3. **Deploy to Hetzner** for production ($13/month)
4. Use our scripts to migrate: `./scripts/backup.sh` → transfer → `./scripts/deploy.sh --import`

## 💡 Cost Optimization Tips

### Mac Deployment
- ✅ Use existing external drive → Save $30-70
- ✅ Skip custom domain → Save $6-15/year
- ✅ Use Cloudflare Tunnel random URL → FREE
- ✅ All software components → FREE
- **Total possible cost: $0**

### Hetzner Deployment
- ✅ Use Cloudflare DNS → FREE (vs $2-5/month)
- ✅ Let's Encrypt SSL → FREE (vs $10-50/year)
- ✅ Self-hosted monitoring → FREE (vs $10-30/month)
- ✅ 20TB bandwidth included → Save $100s vs AWS
- ✅ Start with CX31 → Scale up only when needed
- **Total optimized cost: ~$13/month**

## 📊 Bandwidth Costs Comparison

For media servers, bandwidth is crucial:

| Provider | Included | Overage Cost | Cost for 30TB |
|----------|----------|--------------|---------------|
| **Hetzner** | 20TB | €1.19/TB | €11.90 (~$13) |
| AWS | 1TB | $90/TB | $2,610 |
| DigitalOcean | 4TB | $10/TB | $260 |
| Azure | 100GB | $87/TB | $2,523 |
| **Your Home ISP** | Unlimited* | $0 | $0 |

*Typical home internet is "unlimited" but may throttle heavy usage

**Key Insight:** For bandwidth-heavy apps like LiveKit:
- Mac (home) = Best for testing (unlimited, free)
- Hetzner = Best for production (20TB included, cheap overages)
- AWS/Azure = Very expensive (avoid for media servers)

## 🔒 Security & Compliance

### SSL/TLS Certificates
- **Let's Encrypt**: FREE, automatic, trusted by all browsers
- Caddy handles everything: request, install, renew
- Valid for 90 days, auto-renews at 60 days
- No action required from you

### DNS Security
- **Cloudflare**: DDoS protection included (FREE)
- **DNSSEC**: Supported on most providers (FREE)
- **Privacy Protection**: Included with most registrars

### Firewall
- **Mac**: macOS firewall (built-in, FREE)
- **Hetzner**: Cloud Firewall (FREE) or UFW on server (FREE)

## 📞 Support Options

### Mac Deployment
- Docker Desktop: [docs.docker.com/desktop/mac](https://docs.docker.com/desktop/mac/)
- Cloudflare: [community.cloudflare.com](https://community.cloudflare.com)
- LiveKit: [livekit.io/community](https://livekit.io/community)
- This repo: GitHub Issues

### Hetzner Deployment
- Hetzner: [docs.hetzner.com](https://docs.hetzner.com) + email support
- LiveKit: [livekit.io/community](https://livekit.io/community)
- Cloudflare: [community.cloudflare.com](https://community.cloudflare.com)
- This repo: GitHub Issues

## 🎯 Quick Decision Matrix

| Priority | Mac | Hetzner | AWS/Azure |
|----------|-----|---------|-----------|
| Lowest cost | ✅ $0/mo | ⚠️ $13/mo | ❌ $40+/mo |
| Production ready | ❌ | ✅ | ✅ |
| Easy setup | ✅ | ✅ | ❌ |
| Scalability | ❌ | ✅ | ✅ |
| EU datacenter | N/A | ✅ | ✅ |
| Best bandwidth value | ✅ Unlimited | ✅ 20TB | ❌ Expensive |

**Recommendation:**
- **Start:** Mac (development) → $0/month
- **Grow:** Hetzner (production) → $13/month
- **Scale:** Multi-region Hetzner or cloud → $50+/month

---

**Need More Details?**
- Mac guide: [07-mac-deployment.md](07-mac-deployment.md)
- Hetzner guide: [06-hetzner-deployment.md](06-hetzner-deployment.md)
- Hosting research: [Linux-Hosting-Research-LiveKit.md](Linux-Hosting-Research-LiveKit.md)
