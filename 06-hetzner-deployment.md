# Hetzner Cloud LiveKit Deployment Guide

## Overview

This guide covers deploying LiveKit media server on Hetzner Cloud infrastructure with monitoring and management capabilities.

## 💰 Cost Breakdown - Hetzner Deployment

### What Hetzner Provides (Included in Server Price)

| Feature | Included | Notes |
|---------|----------|-------|
| **Server Instance** | ✅ Yes | CX31: €11/month (~$12) |
| **20TB Bandwidth** | ✅ Yes | Generous allowance |
| **IPv4 & IPv6** | ✅ Yes | Public IP addresses |
| **DDoS Protection** | ✅ Yes | Basic protection included |
| **Snapshots** | ✅ Yes | Manual backups (€0.013/GB/month) |
| **Firewall** | ✅ Yes | Free, managed via console |
| **Monitoring** | ✅ Yes | Basic CPU/RAM/disk metrics |
| **API Access** | ✅ Yes | For automation |

### What Hetzner Does NOT Provide (You Must Configure)

| Service | Hetzner Provides? | You Need | Cost |
|---------|------------------|----------|------|
| **DNS Hosting** | ❌ No | External DNS provider | $0-20/year |
| **Domain Name** | ❌ No | Domain registrar | $6-15/year |
| **SSL Certificates** | ❌ No | Let's Encrypt (via Caddy) | **FREE** |
| **Email Notifications** | ❌ No | SendGrid/Mailgun/SMTP | $0-15/month |
| **External Monitoring** | ❌ No | UptimeRobot/Pingdom | $0-10/month |
| **CDN** | ❌ No | Cloudflare (optional) | **FREE** |

### Total Monthly Cost Breakdown

| Scenario | Hetzner Server | Domain | DNS | Monitoring | Total/Month |
|----------|----------------|--------|-----|------------|-------------|
| **Minimal (Recommended)** | €11 ($12) | $1 | **FREE** | **FREE** | **~$13/month** |
| **With Paid DNS** | €11 ($12) | $1 | $2 | **FREE** | ~$15/month |
| **With All Options** | €11 ($12) | $1 | $2 | $10 | ~$25/month |

**Best Value Setup:**
- Hetzner CX31: €11/month
- Domain via Cloudflare: ~$9/year ($0.75/month)
- Cloudflare DNS: FREE
- Let's Encrypt SSL: FREE
- Self-hosted monitoring (Grafana): FREE
- **Total: ~$13/month**

### Bandwidth Cost Comparison

| Provider | Included | Overage Cost |
|----------|----------|--------------|
| **Hetzner** | 20TB | €1.19/TB (~$1.31) |
| AWS | 1TB | $0.09/GB ($90/TB) |
| DigitalOcean | 4TB | $0.01/GB ($10/TB) |
| Azure | 100GB | $0.087/GB ($87/TB) |

**Hetzner Advantage:** 20TB bandwidth makes it ideal for media servers. At 2GB/hour per participant, you can serve 10,000+ participant-hours per month!

## Prerequisites

### What You Need Before Starting

#### 1. Hetzner Cloud Account
**Cost:** Free to create | **Required:** Yes

**Setup:**
1. Visit [https://www.hetzner.com/cloud](https://www.hetzner.com/cloud)
2. Click "Sign Up"
3. Verify email
4. Add payment method (no charge until you create server)
5. **First-time bonus:** €20 credit (if available)

#### 2. Domain Name
**Cost:** $6-15/year | **Required:** Yes (for HTTPS/production)

**Where Hetzner Fits:**
- ❌ Hetzner does NOT sell domains
- ✅ Hetzner gives you IP address
- ✅ YOU point your domain to that IP

**Recommended Domain Registrars:**

| Registrar | Cost/Year | DNS Included | Notes |
|-----------|-----------|--------------|-------|
| **Cloudflare** | ~$9 | ✅ FREE DNS | Best integration, at-cost pricing |
| **Namecheap** | ~$9-13 | ✅ FREE DNS | Reliable, good support |
| **Porkbun** | ~$6-10 | ✅ FREE DNS | Budget-friendly |
| **Google Domains** | ~$12 | ✅ FREE DNS | Simple interface |

**Setup Process:**
1. Search for available domain (e.g., "mylivekit.com")
2. Purchase domain
3. Keep registrar's DNS or transfer to Cloudflare (recommended)
4. You'll configure DNS records after server creation

#### 3. SSH Key Pair
**Cost:** Free | **Required:** Yes (for secure access)

**Generate SSH Key (if you don't have one):**

**On Mac/Linux:**
```bash
# Generate new SSH key
ssh-keygen -t ed25519 -C "your_email@example.com"

# Save to default location (~/.ssh/id_ed25519)
# Set a passphrase (recommended)

# View public key (you'll need this for Hetzner)
cat ~/.ssh/id_ed25519.pub
```

**On Windows:**
```powershell
# Use PowerShell
ssh-keygen -t ed25519 -C "your_email@example.com"

# View public key
type $env:USERPROFILE\.ssh\id_ed25519.pub
```

#### 4. Basic Linux Knowledge
**Cost:** Free | **Required:** Helpful

**Essential commands you'll use:**
- `ssh` - Connect to server
- `apt` - Install packages
- `docker` - Manage containers
- `nano` or `vim` - Edit files

**Learning resources (free):**
- [Linux Journey](https://linuxjourney.com/) - Interactive tutorial
- [Ubuntu Server Guide](https://ubuntu.com/server/docs) - Official docs

## DNS Configuration Guide

### Understanding DNS for Hetzner Deployment

**What Hetzner Provides:**
- Public IPv4 address (e.g., `95.217.123.456`)
- Public IPv6 address
- Reverse DNS configuration (optional)

**What Hetzner Does NOT Provide:**
- DNS hosting for your domain
- Domain registration
- Automatic DNS setup

**YOU Must Configure DNS** to point your domain to Hetzner's IP address.

### DNS Setup Options

#### Option 1: Cloudflare DNS (Recommended - FREE)

**Why Cloudflare:**
- ✅ Completely FREE
- ✅ Fast global DNS (1.1.1.1)
- ✅ DDoS protection included
- ✅ Flexible SSL/TLS options
- ✅ Can buy domain through them
- ✅ Easy integration with Caddy

**Setup Steps:**

1. **Sign up for Cloudflare**
   - Visit [https://dash.cloudflare.com/sign-up](https://dash.cloudflare.com/sign-up)
   - Create free account
   - No credit card required

2. **Add Your Domain**
   - Click "Add a Site"
   - Enter your domain (e.g., "example.com")
   - Choose FREE plan
   - Click "Add Site"

3. **Update Nameservers**
   - Cloudflare will show you 2 nameservers:
     ```
     nova.ns.cloudflare.com
     sid.ns.cloudflare.com
     ```
   - Go to your domain registrar (where you bought domain)
   - Find "Nameservers" or "DNS Settings"
   - Replace existing nameservers with Cloudflare's
   - Save changes (propagation takes 5 min - 48 hours)

4. **Add DNS Records** (After server creation)
   
   In Cloudflare dashboard → DNS → Records:
   
   ```
   Type: A
   Name: livekit (or @, for root domain)
   IPv4: YOUR_HETZNER_IP
   Proxy: OFF (gray cloud) - Important for LiveKit WebRTC!
   TTL: Auto
   ```
   
   ```
   Type: A
   Name: grafana
   IPv4: YOUR_HETZNER_IP
   Proxy: ON (orange cloud) - OK for Grafana
   TTL: Auto
   ```
   
   **Important:** Keep "Proxy" OFF for LiveKit (WebRTC needs direct connection)

5. **Verify DNS Propagation**
   ```bash
   # Check if DNS is working
   dig livekit.yourdomain.com
   
   # Should show your Hetzner IP
   nslookup livekit.yourdomain.com
   ```

**Documentation:** [Cloudflare DNS Setup](https://developers.cloudflare.com/dns/zone-setups/full-setup/)

#### Option 2: Registrar's DNS (Usually FREE)

Most domain registrars include free DNS hosting.

**Setup Steps:**

1. **Login to your registrar** (Namecheap, Google Domains, etc.)

2. **Find DNS Management** section

3. **Add A Records:**
   ```
   Host: livekit
   Type: A
   Value: YOUR_HETZNER_IP
   TTL: 300 (5 minutes)
   
   Host: grafana
   Type: A  
   Value: YOUR_HETZNER_IP
   TTL: 300
   ```

4. **Wait for propagation** (usually 5-30 minutes)

5. **Verify:**
   ```bash
   dig livekit.yourdomain.com
   ```

#### Option 3: Hetzner DNS (FREE)

Hetzner offers free DNS hosting for domains pointed to Hetzner servers.

**Setup Steps:**

1. **Login to Hetzner Cloud Console**
   - Visit [https://console.hetzner.cloud](https://console.hetzner.cloud)

2. **Go to DNS Section**
   - Click "DNS" in left sidebar
   - Click "Add Zone"

3. **Add Your Domain**
   - Enter domain name
   - Click "Add Zone"

4. **Update Nameservers at Registrar**
   
   Point to Hetzner nameservers:
   ```
   hydrogen.ns.hetzner.com
   oxygen.ns.hetzner.com
   helium.ns.hetzner.de
   ```

5. **Add DNS Records in Hetzner Console:**
   ```
   Name: livekit
   Type: A
   Value: YOUR_HETZNER_IP
   TTL: 300
   
   Name: grafana
   Type: A
   Value: YOUR_HETZNER_IP  
   TTL: 300
   ```

**Documentation:** [Hetzner DNS Console](https://docs.hetzner.com/dns-console/)

### SSL/TLS Certificates (Let's Encrypt via Caddy)

**Good News:** SSL certificates are FREE and automatic!

**How it works:**
1. Caddy web server is included in deployment
2. Caddy automatically requests SSL from Let's Encrypt
3. Certificates auto-renew every 90 days
4. Zero configuration needed (if DNS is correct)

**Requirements:**
- DNS must point to your server (A records configured)
- Ports 80 and 443 must be open (firewall configured)
- Domain must be publicly accessible

**Caddy handles:**
- ✅ Certificate request
- ✅ Certificate installation
- ✅ HTTPS redirect (HTTP → HTTPS)
- ✅ Certificate renewal
- ✅ OCSP stapling

**No cost, no manual work required!**

### Complete DNS Configuration Example

**Scenario:** You own `livekit.example.com` and created Hetzner server with IP `95.217.123.456`

**DNS Records to Add:**

| Type | Name | Value | TTL | Purpose |
|------|------|-------|-----|---------|
| A | livekit | 95.217.123.456 | 300 | LiveKit server |
| A | grafana | 95.217.123.456 | 300 | Monitoring dashboard |
| A | @ or root | 95.217.123.456 | 300 | Main domain (optional) |

**After DNS setup:**
- `https://livekit.example.com` → LiveKit WebRTC server
- `https://grafana.example.com` → Grafana monitoring
- Caddy auto-configures SSL for both

**Verification:**
```bash
# Check DNS resolution
dig livekit.example.com +short
# Should output: 95.217.123.456

dig grafana.example.com +short
# Should output: 95.217.123.456

# Test HTTPS (after deployment)
curl -I https://livekit.example.com
# Should show: HTTP/2 200
```

## Server Specifications

### Recommended Starting Configuration
- **Instance**: CX31 (2 vCPU, 8GB RAM, 80GB NVMe)
- **Location**: Falkenstein, Nuremberg, or Helsinki
- **OS**: Ubuntu 22.04 LTS
- **Monthly Cost**: ~€11 ($12)
- **Bandwidth**: 20TB included

### Scaling Options
- CX41 for 50-100 concurrent users (4 vCPU, 16GB RAM)
- CX51 for 100-200 concurrent users (8 vCPU, 32GB RAM)
- Dedicated CCX line for high-performance needs

## Deployment Architecture

```
┌─────────────────────────────────────────┐
│         Hetzner Cloud Server            │
│  ┌───────────────────────────────────┐  │
│  │     Caddy (Reverse Proxy)         │  │
│  │  - Auto HTTPS (Let's Encrypt)     │  │
│  │  - WebRTC endpoints               │  │
│  └───────────────────────────────────┘  │
│                  │                       │
│  ┌───────────────┴───────────────────┐  │
│  │        LiveKit Server             │  │
│  │  - SFU Media Router               │  │
│  │  - WebRTC signaling               │  │
│  │  - Room management                │  │
│  └───────────────┬───────────────────┘  │
│                  │                       │
│  ┌───────────────┴───────────────────┐  │
│  │      Monitoring Stack             │  │
│  │  - Prometheus (metrics)           │  │
│  │  - Grafana (dashboard)            │  │
│  │  - Loki (log aggregation)         │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

## Network Configuration

### Firewall Rules
```bash
# HTTP/HTTPS for Caddy
Allow: TCP 80, 443

# LiveKit WebRTC
Allow: TCP 7880, 7881
Allow: UDP 443, 50000-60000

# Monitoring (restricted to your IP)
Allow: TCP 3000 (Grafana) - from YOUR_IP only
Allow: TCP 9090 (Prometheus) - from YOUR_IP only

# SSH
Allow: TCP 22 - from YOUR_IP only
```

### DNS Configuration
```
livekit.yourdomain.com    A     SERVER_IP
grafana.yourdomain.com    A     SERVER_IP
```

## Quick Start Deployment

**Prerequisites Checklist:**
- ✅ Hetzner Cloud account created
- ✅ Domain name purchased (see recommendations above)
- ✅ DNS provider chosen (Cloudflare recommended)
- ✅ SSH key generated and added to Hetzner
- ✅ Credit card/payment method added to Hetzner

### Step-by-Step Deployment

### 1. Create Hetzner Server

**Option A: Via Web Console (Easiest)**

1. Login to [https://console.hetzner.cloud](https://console.hetzner.cloud)
2. Click "New Project" → Name it (e.g., "LiveKit Production")
3. Click "Add Server"
4. Choose options:
   - **Location:** Falkenstein (Germany) - Recommended for EU
   - **Image:** Ubuntu 22.04
   - **Type:** Standard - CX31 (2 vCPU, 8GB RAM)
   - **Networking:** IPv4 + IPv6
   - **SSH Keys:** Select your key (or add new one)
   - **Volumes:** None (not needed)
   - **Firewalls:** Create new (configure after server creation)
5. **Name:** livekit-prod
6. Click "Create & Buy Now"
7. **Note the IP address** shown (e.g., 95.217.123.456)

**Cost:** €11/month (~$12) billed hourly (~€0.015/hour)

**Option B: Via CLI (Advanced)**

```bash
# Install Hetzner CLI
brew install hcloud  # Mac
# or
wget https://github.com/hetznercloud/cli/releases/latest/download/hcloud-linux-amd64.tar.gz
tar -xvf hcloud-linux-amd64.tar.gz

# Authenticate (get token from console.hetzner.cloud → Security → API Tokens)
hcloud context create livekit-project

# Create server
hcloud server create \
  --name livekit-prod \
  --type cx31 \
  --image ubuntu-22.04 \
  --location fsn1 \
  --ssh-key YOUR_KEY_NAME

# Note the IP address from output
```

### 2. Configure DNS (Do This Now!)

**Before proceeding, configure DNS to point to your Hetzner IP.**

**Using Cloudflare (Recommended):**

1. Go to Cloudflare DNS dashboard
2. Add A records:
   ```
   Type: A | Name: livekit | IPv4: YOUR_HETZNER_IP | Proxy: OFF
   Type: A | Name: grafana | IPv4: YOUR_HETZNER_IP | Proxy: OFF
   ```
3. Wait 2-5 minutes for propagation

**Using Other DNS:**
- Add same A records in your DNS provider
- Wait for DNS propagation (check with `dig livekit.yourdomain.com`)

**Verify DNS:**
```bash
# Check DNS resolution
dig livekit.yourdomain.com +short

# Should output your Hetzner IP
# If not, wait a few minutes and try again
```

**Why Now?** Caddy needs DNS to be working to issue SSL certificates. Setting up DNS now means SSL will work immediately after deployment.

### 3. Initial Server Setup
```bash
# Connect to server
ssh root@YOUR_HETZNER_IP

# Update system
apt update && apt upgrade -y

# Install required packages
apt install -y curl git ufw

# Configure firewall
ufw allow 22/tcp
ufw allow 80/tcp
ufw allow 443/tcp
ufw allow 7880:7881/tcp
ufw allow 443/udp
ufw allow 50000:60000/udp
ufw --force enable

# Verify firewall
ufw status
```

### 4. Install Docker
```bash
# Install Docker using official script
curl -fsSL https://get.docker.com -o get-docker.sh
sh get-docker.sh

# Install Docker Compose
apt install -y docker-compose-plugin

# Verify installations
docker --version
docker compose version
```

### 5. Deploy Using Deployment Scripts

**Option A: Automated Deployment (Recommended)**

```bash
# Clone deployment repository
git clone https://github.com/SimonDarksideJ/ResearchBucket.git
cd ResearchBucket/deployment-tools

# Run deployment script
./scripts/deploy.sh --platform linux --env production

# Follow prompts to enter:
# - Your domain name (e.g., livekit.yourdomain.com)
# - Email for Let's Encrypt notifications
```

**Option B: Manual Deployment**

See "Manual Deployment Steps" section below for detailed instructions.

### 6. Verify Deployment

**Check Services:**
```bash
cd /opt/livekit
docker compose ps

# All services should show "Up"
```

**Test Endpoints:**
```bash
# Test LiveKit
curl http://localhost:7880

# Test SSL (wait 2-3 minutes after deployment)
curl -I https://livekit.yourdomain.com
# Should show: HTTP/2 200

# Test Grafana
curl -I https://grafana.yourdomain.com
# Should show: HTTP/2 200
```

**Access Services:**
- LiveKit: `https://livekit.yourdomain.com`
- Grafana: `https://grafana.yourdomain.com` (login: admin / check deployment output)
- Prometheus: `http://YOUR_IP:9090` (only accessible from your IP if firewall configured)

### 7. Post-Deployment Security

```bash
# Change Grafana password immediately
docker compose exec grafana grafana-cli admin reset-admin-password YOUR_NEW_PASSWORD

# Restrict monitoring access to your IP only
ufw delete allow 3000/tcp
ufw delete allow 9090/tcp
ufw allow from YOUR_HOME_IP to any port 3000 proto tcp
ufw allow from YOUR_HOME_IP to any port 9090 proto tcp
ufw reload

# Create regular backups
./scripts/backup.sh --compress --destination /root/backups
```

## What Hetzner Manages vs What You Configure

### ✅ Hetzner Manages (Automatic)

| Feature | Hetzner Responsibility | Your Action |
|---------|----------------------|-------------|
| **Hardware** | Physical servers, networking | None - it just works |
| **Network** | 1Gbps uplink, 20TB bandwidth | None - included |
| **IP Addresses** | IPv4 + IPv6 assignment | Use provided IPs |
| **DDoS Protection** | Basic protection | None - automatic |
| **Data Center** | Power, cooling, security | Choose location at creation |
| **Backups (Snapshots)** | Storage infrastructure | Enable/schedule in console |

### ⚙️ You Must Configure

| Feature | Your Responsibility | Tools/Method |
|---------|-------------------|--------------|
| **Operating System** | Install, update, patch | Ubuntu via apt |
| **Firewall Rules** | Configure ports | UFW or Hetzner Cloud Firewall |
| **DNS Records** | Point domain to IP | Your DNS provider |
| **SSL Certificates** | Setup (automated by Caddy) | Let's Encrypt via Caddy |
| **Applications** | Install, configure | Docker Compose |
| **Monitoring** | Setup dashboards | Grafana/Prometheus |
| **Backups** | Schedule and verify | Scripts or Hetzner Snapshots |
| **Security Updates** | Apply regularly | `apt update && apt upgrade` |

### 🔄 Hybrid (Hetzner Provides Tools, You Execute)

| Feature | Hetzner Provides | You Do |
|---------|-----------------|---------|
| **Firewall** | Cloud Firewall service (free) | Configure rules in console |
| **Load Balancer** | Service (€5.90/month) | Create and configure |
| **Volumes** | Block storage (€0.05/GB/month) | Attach and mount |
| **Networks** | Private networking (free) | Create and attach |
| **Snapshots** | Snapshot functionality (€0.013/GB/month) | Create and manage |

### Summary: Hetzner is IaaS (Infrastructure as a Service)

**What this means:**
- Hetzner gives you: Server, IP, bandwidth, network
- You handle: Everything else (OS, apps, DNS, SSL, monitoring)
- **But:** Our deployment scripts automate 90% of the setup!

**Think of it like:**
- 🏢 **Hetzner** = Building owner (provides space, utilities)
- 👨‍💻 **You** = Tenant (furnish, decorate, maintain)
- 📦 **Our Scripts** = Moving company (automates setup)

**Comparison:**

| Service Type | Example | What They Manage | What You Manage |
|--------------|---------|------------------|-----------------|
| **IaaS** | Hetzner, AWS EC2 | Hardware, network | OS, apps, everything |
| **PaaS** | Heroku, Railway | Hardware, OS, runtime | Just your app |
| **SaaS** | Zoom, Slack | Everything | Just use it |

**Hetzner = IaaS** → Most control, most responsibility, lowest cost

## Manual Deployment Steps

### 1. Create Directory Structure
```bash
mkdir -p /opt/livekit/{config,data,logs,monitoring}
cd /opt/livekit
```

### 2. Configure LiveKit

Create `/opt/livekit/config/livekit.yaml`:
```yaml
port: 7880
bind_addresses:
  - "0.0.0.0"

rtc:
  tcp_port: 7881
  port_range_start: 50000
  port_range_end: 60000
  use_external_ip: true
  
keys:
  # Generate with: docker run --rm livekit/livekit-server generate-keys
  API_KEY: your_api_key
  API_SECRET: your_api_secret

logging:
  level: info
  sample: false

room:
  auto_create: true
  max_participants: 50
  empty_timeout: 300
  
webhook:
  urls:
    - http://your-app.com/livekit/webhook
```

### 3. Create Docker Compose File

Create `/opt/livekit/docker-compose.yml`:
```yaml
version: '3.9'

services:
  livekit:
    image: livekit/livekit-server:latest
    container_name: livekit
    restart: unless-stopped
    ports:
      - "7880:7880"
      - "7881:7881"
      - "443:443/udp"
      - "50000-60000:50000-60000/udp"
    volumes:
      - ./config/livekit.yaml:/etc/livekit.yaml
      - ./logs:/logs
    command: --config /etc/livekit.yaml --bind 0.0.0.0
    networks:
      - livekit-network

  caddy:
    image: caddy:latest
    container_name: caddy
    restart: unless-stopped
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./config/Caddyfile:/etc/caddy/Caddyfile
      - caddy_data:/data
      - caddy_config:/config
    networks:
      - livekit-network

  prometheus:
    image: prom/prometheus:latest
    container_name: prometheus
    restart: unless-stopped
    ports:
      - "9090:9090"
    volumes:
      - ./monitoring/prometheus.yml:/etc/prometheus/prometheus.yml
      - prometheus_data:/prometheus
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'
      - '--storage.tsdb.path=/prometheus'
    networks:
      - livekit-network

  grafana:
    image: grafana/grafana:latest
    container_name: grafana
    restart: unless-stopped
    ports:
      - "3000:3000"
    volumes:
      - ./monitoring/grafana-datasources.yml:/etc/grafana/provisioning/datasources/datasources.yml
      - ./monitoring/grafana-dashboards.yml:/etc/grafana/provisioning/dashboards/dashboards.yml
      - ./monitoring/dashboards:/var/lib/grafana/dashboards
      - grafana_data:/var/lib/grafana
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=changeme
      - GF_INSTALL_PLUGINS=grafana-piechart-panel
    networks:
      - livekit-network

  loki:
    image: grafana/loki:latest
    container_name: loki
    restart: unless-stopped
    ports:
      - "3100:3100"
    volumes:
      - ./monitoring/loki-config.yml:/etc/loki/local-config.yaml
      - loki_data:/loki
    command: -config.file=/etc/loki/local-config.yaml
    networks:
      - livekit-network

  promtail:
    image: grafana/promtail:latest
    container_name: promtail
    restart: unless-stopped
    volumes:
      - ./logs:/var/log/livekit
      - ./monitoring/promtail-config.yml:/etc/promtail/config.yml
      - /var/lib/docker/containers:/var/lib/docker/containers:ro
    command: -config.file=/etc/promtail/config.yml
    networks:
      - livekit-network

volumes:
  caddy_data:
  caddy_config:
  prometheus_data:
  grafana_data:
  loki_data:

networks:
  livekit-network:
    driver: bridge
```

### 4. Configure Caddy

Create `/opt/livekit/config/Caddyfile`:
```
livekit.yourdomain.com {
    reverse_proxy livekit:7880
    
    header {
        Access-Control-Allow-Origin *
        Access-Control-Allow-Methods "GET, POST, OPTIONS"
        Access-Control-Allow-Headers *
    }
}

grafana.yourdomain.com {
    reverse_proxy grafana:3000
}
```

### 5. Start Services
```bash
cd /opt/livekit
docker compose up -d

# View logs
docker compose logs -f

# Check status
docker compose ps
```

## Monitoring Setup

### Access Grafana
1. Navigate to `https://grafana.yourdomain.com`
2. Login with admin/changeme
3. Change password immediately
4. Import LiveKit dashboard (ID: 15180)

### Key Metrics to Monitor
- **CPU/Memory Usage**: Container resource utilization
- **Active Connections**: Number of WebRTC connections
- **Bandwidth Usage**: Upload/download rates
- **Packet Loss**: Network quality indicator
- **Room Count**: Active rooms on server

## Backup Strategy

### Configuration Backup
```bash
# Backup configs
tar -czf livekit-config-$(date +%Y%m%d).tar.gz /opt/livekit/config

# Backup to remote storage
rsync -avz /opt/livekit/config/ backup-server:/backups/livekit/
```

### Automated Backups
Create `/opt/livekit/scripts/backup.sh`:
```bash
#!/bin/bash
BACKUP_DIR="/opt/livekit/backups"
DATE=$(date +%Y%m%d-%H%M%S)

mkdir -p $BACKUP_DIR

# Backup configuration
tar -czf $BACKUP_DIR/config-$DATE.tar.gz /opt/livekit/config

# Backup Grafana dashboards
docker exec grafana grafana-cli admin export-dashboard > $BACKUP_DIR/grafana-$DATE.json

# Keep only last 7 days
find $BACKUP_DIR -type f -mtime +7 -delete
```

Add to crontab:
```bash
0 2 * * * /opt/livekit/scripts/backup.sh
```

## Maintenance

### Update LiveKit
```bash
cd /opt/livekit
docker compose pull livekit
docker compose up -d livekit
```

### Log Rotation
Create `/etc/logrotate.d/livekit`:
```
/opt/livekit/logs/*.log {
    daily
    rotate 7
    compress
    delaycompress
    missingok
    notifempty
    create 0640 root root
}
```

### Health Checks
```bash
# Check LiveKit status
curl http://localhost:7880/

# Check Docker containers
docker compose ps

# View resource usage
docker stats

# Check logs
docker compose logs --tail=50 livekit
```

## Troubleshooting

### LiveKit Won't Start
```bash
# Check logs
docker compose logs livekit

# Verify configuration
docker compose config

# Check port conflicts
netstat -tulpn | grep -E '7880|7881'
```

### WebRTC Connection Issues
```bash
# Verify UDP ports are open
nc -uz YOUR_SERVER_IP 443
nc -uz YOUR_SERVER_IP 50000

# Check firewall
ufw status

# Verify external IP detection
docker compose logs livekit | grep "external"
```

### High CPU Usage
```bash
# Check active rooms/participants
docker exec livekit livekit-cli list-rooms

# Monitor real-time metrics
docker stats livekit

# Check for memory leaks
docker compose restart livekit
```

### SSL Certificate Issues
```bash
# Check Caddy logs
docker compose logs caddy

# Verify DNS propagation
nslookup livekit.yourdomain.com

# Manual certificate request
docker exec caddy caddy reload
```

## Performance Optimization

### Kernel Tuning
Add to `/etc/sysctl.conf`:
```
# Increase UDP buffer sizes
net.core.rmem_max = 134217728
net.core.rmem_default = 134217728
net.core.wmem_max = 134217728
net.core.wmem_default = 134217728

# Increase connection tracking
net.netfilter.nf_conntrack_max = 1048576
net.nf_conntrack_max = 1048576

# TCP optimization
net.ipv4.tcp_rmem = 4096 87380 134217728
net.ipv4.tcp_wmem = 4096 65536 134217728
```

Apply changes:
```bash
sysctl -p
```

### Docker Resource Limits
Update `docker-compose.yml`:
```yaml
services:
  livekit:
    # ... other config
    deploy:
      resources:
        limits:
          cpus: '2'
          memory: 4G
        reservations:
          cpus: '1'
          memory: 2G
```

## Security Hardening

### Firewall Best Practices
```bash
# Lock down Prometheus to VPN/specific IPs
ufw delete allow 9090/tcp
ufw allow from YOUR_VPN_IP to any port 9090 proto tcp

# Same for Grafana
ufw delete allow 3000/tcp
ufw allow from YOUR_VPN_IP to any port 3000 proto tcp
```

### SSL/TLS Configuration
Caddy handles this automatically with Let's Encrypt, but verify:
```bash
# Test SSL configuration
openssl s_client -connect livekit.yourdomain.com:443 -showcerts
```

### Regular Updates
```bash
#!/bin/bash
# /opt/livekit/scripts/security-update.sh

apt update
apt upgrade -y

docker compose pull
docker compose up -d

# Restart if needed
docker compose restart
```

## Cost Optimization

### Right-Sizing
- Start with CX31 (€11/month)
- Monitor resource usage for 1-2 weeks
- Scale up only if consistently >70% CPU or memory
- Hetzner's 20TB bandwidth is generous - unlikely to exceed

### Monitoring Alerts
Set up Grafana alerts for:
- CPU > 80% for 10 minutes → Consider scaling
- Memory > 85% → Add RAM or optimize
- Bandwidth approaching 18TB → Plan for next month

## Next Steps

1. Review and customize configuration files
2. Set up DNS records for your domain
3. Deploy using automated scripts
4. Configure monitoring dashboards
5. Set up automated backups
6. Document your specific setup
7. Plan for scaling based on usage

## Additional Resources

- [LiveKit Documentation](https://docs.livekit.io/)
- [Hetzner Cloud Docs](https://docs.hetzner.com/cloud/)
- [Caddy Documentation](https://caddyserver.com/docs/)
- [Grafana Dashboard Gallery](https://grafana.com/grafana/dashboards/)

---

**Note**: This document provides the foundation for the Mac deployment guide. The deployment scripts in this repository work for both Hetzner Cloud (Linux) and Mac environments with automatic platform detection.
