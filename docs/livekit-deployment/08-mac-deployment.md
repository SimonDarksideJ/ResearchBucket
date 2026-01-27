# LiveKit Deployment on macOS with External Storage & Monitoring

## Overview

This guide provides a **complete end-to-end solution** for deploying LiveKit on macOS using:

- **Docker Desktop** for containerization
- **External storage** for data/logs (to preserve main drive space)
- **Comprehensive monitoring** with Grafana + Prometheus dashboards
- **Reverse proxy** for secure external access without router configuration
- **Automated scripts** for deployment, maintenance, and monitoring
- **Cross-platform compatibility** - scripts designed to work on both Mac and Linux (Hetzner)

Perfect for development, testing, and small-scale production deployments on Mac hardware.

## Table of Contents

1. [What You'll Build](#what-youll-build)
2. [Prerequisites](#prerequisites)
3. [Quick Start (5 Minutes)](#quick-start-5-minutes)
4. [Detailed Setup Guide](#detailed-setup-guide)
5. [Monitoring Dashboard](#monitoring-dashboard)
6. [Reverse Proxy Setup](#reverse-proxy-setup)
7. [Maintenance & Operations](#maintenance--operations)
8. [Troubleshooting](#troubleshooting)
9. [Reusing Scripts for Hetzner](#reusing-scripts-for-hetzner)

---

## What You'll Build

By the end of this guide, you'll have:

✅ **LiveKit Server** running in Docker on your Mac
✅ **External storage** for all data, logs, and recordings  
✅ **Web-based monitoring dashboard** (Grafana) showing:
   - Server health and status
   - Active rooms and participants
   - CPU, memory, and network usage
   - Log aggregation and search
✅ **Secure external access** via reverse proxy (no router config needed)
✅ **Automated maintenance** scripts for:
   - Starting/stopping services
   - Log rotation and backup
   - Health checks
   - Updates
✅ **Cross-platform scripts** usable on Mac and Linux

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────┐
│                     Your Mac                            │
│                                                         │
│  ┌──────────────┐         ┌──────────────┐            │
│  │   LiveKit    │◄────────│    Caddy     │◄───────────┼─── Internet
│  │   Server     │         │ Reverse Proxy│            │    (via tunnel)
│  └──────────────┘         └──────────────┘            │
│         │                                              │
│         │ metrics                                      │
│         ▼                                              │
│  ┌──────────────┐         ┌──────────────┐            │
│  │  Prometheus  │────────►│   Grafana    │◄───────────┼─── http://localhost:3000
│  │   (metrics)  │         │  (dashboard) │            │    (monitoring)
│  └──────────────┘         └──────────────┘            │
│         │                                              │
│         │                                              │
│  ┌──────────────────────────────────────────┐         │
│  │      External Storage Drive              │         │
│  │  - LiveKit data & recordings             │         │
│  │  - Logs (with rotation)                  │         │
│  │  - Prometheus metrics DB                 │         │
│  │  - Configuration backups                 │         │
│  └──────────────────────────────────────────┘         │
└─────────────────────────────────────────────────────────┘
```

---

## Prerequisites

### Required

1. **macOS** 11 (Big Sur) or later
2. **Docker Desktop** for Mac installed
   - [Download Docker Desktop](https://www.docker.com/products/docker-desktop)
   - At least 4GB RAM allocated to Docker
3. **External Storage Drive**
   - USB 3.0+ or Thunderbolt external drive
   - At least 50GB free space recommended
   - Formatted as APFS or exFAT (APFS recommended for Mac)
4. **Homebrew** package manager
   - Install from [brew.sh](https://brew.sh)
5. **Internet connection** (for downloading images and packages)

### Optional

- A domain name (for custom URLs via reverse proxy)
- ngrok account (free tier works, for reverse proxy)
- Another Mac/Linux machine for remote monitoring dashboard

### System Requirements

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| CPU | 2 cores | 4+ cores |
| RAM | 8GB | 16GB+ |
| External Storage | 50GB | 100GB+ |
| Docker RAM | 4GB | 6GB+ |

---

## Quick Start (5 Minutes)

For those who want to get started immediately:

```bash
# 1. Clone or download the repository
cd /path/to/ResearchBucket

# 2. Run the automated setup script
./tools/mac-livekit/scripts/setup.sh

# 3. Follow the interactive prompts:
#    - Select your external storage drive
#    - Choose reverse proxy option
#    - Set API credentials

# 4. Access your services:
#    - Monitoring Dashboard: http://localhost:3000
#    - LiveKit endpoint: (URL provided by script)
```

That's it! The script handles:
- ✅ Installing dependencies
- ✅ Configuring external storage
- ✅ Starting LiveKit + monitoring
- ✅ Setting up reverse proxy
- ✅ Creating maintenance scripts

Continue reading for detailed explanations and customization options.

---

## Detailed Setup Guide

### Step 1: Prepare External Storage

#### 1.1 Connect and Format Drive (if needed)

1. Connect your external drive to your Mac
2. Open **Disk Utility** (Applications → Utilities)
3. Select your external drive
4. Click **Erase**:
   - Name: `LiveKitStorage` (or your preference)
   - Format: **APFS** (recommended) or **ExFAT**
   - Scheme: **GUID Partition Map**
5. Click **Erase**

#### 1.2 Create LiveKit Directory Structure

Run this script to set up the directory structure:

```bash
# Define your external storage path
STORAGE_PATH="/Volumes/LiveKitStorage"

# Create directory structure
mkdir -p "$STORAGE_PATH"/livekit/{data,logs,config,recordings,backups}
mkdir -p "$STORAGE_PATH"/monitoring/{prometheus,grafana,loki}
mkdir -p "$STORAGE_PATH"/caddy/{data,config}

# Set permissions
chmod -R 755 "$STORAGE_PATH"/livekit
chmod -R 755 "$STORAGE_PATH"/monitoring
chmod -R 755 "$STORAGE_PATH"/caddy

echo "✅ Directory structure created at $STORAGE_PATH"
```

#### 1.3 Verify Storage

```bash
# Check storage is writable
touch "$STORAGE_PATH"/livekit/.test && rm "$STORAGE_PATH"/livekit/.test
echo "✅ Storage is writable"

# Check available space
df -h "$STORAGE_PATH"
```

---

### Step 2: Install Docker Desktop

#### 2.1 Download and Install

1. Download [Docker Desktop for Mac](https://www.docker.com/products/docker-desktop)
2. Install the application
3. Open Docker Desktop
4. Go to **Settings** → **Resources**:
   - Set **Memory** to at least 4GB (6GB+ recommended)
   - Set **CPUs** to at least 2 (4+ recommended)
5. Click **Apply & Restart**

#### 2.2 Configure Docker for External Storage

Docker needs to access your external storage:

1. Open **Docker Desktop** → **Settings** → **Resources** → **File Sharing**
2. Add your external storage path: `/Volumes/LiveKitStorage`
3. Click **Apply & Restart**

#### 2.3 Verify Docker Installation

```bash
docker version
docker compose version

# Should see version information for both
```

---

### Step 3: Install Required Tools

Install additional tools via Homebrew:

```bash
# Update Homebrew
brew update

# Install required tools
brew install jq curl wget openssl

# Verify installations
jq --version
curl --version
openssl version
```

---

### Step 4: Download Deployment Scripts

The deployment scripts are located in `tools/mac-livekit/`. Let's review what's included:

```
tools/mac-livekit/
├── scripts/
│   ├── setup.sh              # Main installation script
│   ├── start.sh              # Start all services
│   ├── stop.sh               # Stop all services
│   ├── restart.sh            # Restart services
│   ├── status.sh             # Check service status
│   ├── logs.sh               # View logs
│   ├── backup.sh             # Backup configuration
│   ├── update.sh             # Update LiveKit
│   ├── health-check.sh       # Health monitoring
│   └── uninstall.sh          # Complete removal
├── config/
│   ├── livekit-config.yaml   # LiveKit configuration
│   ├── docker-compose.yml    # Docker services
│   ├── prometheus.yml        # Prometheus config
│   └── grafana-dashboards/   # Grafana dashboard JSONs
├── monitoring/
│   └── alerting-rules.yml    # Alert configurations
└── README.md                 # Scripts documentation
```

---

### Step 5: Run Automated Setup

The `setup.sh` script is interactive and handles the complete installation.

```bash
cd /path/to/ResearchBucket
./tools/mac-livekit/scripts/setup.sh
```

The script will:

1. ✅ Detect your system and external storage
2. ✅ Generate secure API keys
3. ✅ Create configuration files
4. ✅ Set up Docker Compose stack
5. ✅ Configure monitoring (Prometheus + Grafana)
6. ✅ Set up reverse proxy (multiple options)
7. ✅ Start all services
8. ✅ Verify connectivity
9. ✅ Display access URLs and credentials

#### Interactive Setup Prompts

```
🚀 LiveKit Mac Deployment Setup
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📋 Step 1: System Check
✅ macOS detected: macOS 13.5
✅ Docker Desktop installed: v24.0.6
✅ External storage available

📂 Step 2: Storage Configuration
Available external drives:
  1) /Volumes/LiveKitStorage (500GB free)
  2) /Volumes/Backup (1TB free)
  3) Enter custom path

Select external storage [1-3]: 1

🔐 Step 3: Generate API Credentials
Generating secure API key and secret...
✅ API_KEY: a1b2c3d4e5f6...
✅ API_SECRET: [hidden]

🌐 Step 4: Reverse Proxy Setup
How do you want to access LiveKit externally?

  1) ngrok (easiest, requires free account)
  2) Cloudflare Tunnel (free, requires domain)
  3) Tailscale (private network, free)
  4) LocalTunnel (free, random URLs)
  5) Skip (local network only)

Select option [1-5]: 1

📊 Step 5: Monitoring Dashboard
Install Grafana monitoring dashboard?
  - Real-time metrics
  - Log aggregation
  - Performance graphs

Install monitoring? [Y/n]: y

⚙️  Step 6: Starting Services
Starting Docker containers...
✅ LiveKit Server started
✅ Prometheus started
✅ Grafana started  
✅ Caddy reverse proxy started
✅ ngrok tunnel established

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🎉 Setup Complete!

📡 Access Points:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
LiveKit Server (local):  http://localhost:7880
LiveKit Server (public): https://abc123.ngrok-free.app
Monitoring Dashboard:     http://localhost:3000
  Username: admin
  Password: [shown once]

🔑 API Credentials:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
API_KEY:    a1b2c3d4e5f6...
API_SECRET: [saved in .env file]

📁 Data Location:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
All data stored at: /Volumes/LiveKitStorage/livekit

💡 Next Steps:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
1. Open monitoring dashboard: open http://localhost:3000
2. Test LiveKit health: curl http://localhost:7880/health
3. View logs: ./tools/mac-livekit/scripts/logs.sh
4. Check status: ./tools/mac-livekit/scripts/status.sh

📖 Documentation: docs/livekit-deployment/08-mac-deployment.md
```

---

### Step 6: Verify Installation

After setup completes, verify everything is working:

```bash
# Check service status
./tools/mac-livekit/scripts/status.sh

# Test LiveKit health endpoint
curl http://localhost:7880/health

# Expected output: {"healthy": true}

# Check Docker containers
docker ps

# Should see: livekit, prometheus, grafana, caddy containers
```

---

## Monitoring Dashboard

### Accessing Grafana

1. Open your browser to `http://localhost:3000`
2. Login with credentials from setup output
   - Default: `admin` / `admin` (change on first login)
3. Navigate to **Dashboards** → **LiveKit Overview**

### Pre-configured Dashboards

The setup includes three Grafana dashboards:

#### 1. LiveKit Overview Dashboard

Displays:
- 🟢 Server status (up/down)
- 👥 Active rooms and participants
- 📊 CPU and memory usage
- 🌐 Network throughput
- ⏱️ Response times
- 📈 Request rate

#### 2. System Resources Dashboard

Shows:
- 💻 Host system metrics
- 🐳 Docker container stats
- 💾 Storage usage (external drive)
- 🔥 Temperature (if available)
- 📊 Historical trends

#### 3. Logs Dashboard

Features:
- 🔍 Log search and filtering
- ⚠️ Error aggregation
- 📋 Recent log entries
- 🎯 Pattern detection
- 📉 Log volume over time

### Dashboard Screenshots

*(Screenshots would be included in actual documentation)*

### Customizing Dashboards

Edit dashboards in Grafana UI:
1. Click **Dashboard Settings** (gear icon)
2. Click **JSON Model** to export/edit
3. Save changes

Or edit JSON files directly:
```bash
# Dashboard files location
ls $STORAGE_PATH/monitoring/grafana/dashboards/
```

### Setting Up Alerts

Configure alerts for critical events:

1. In Grafana, go to **Alerting** → **Alert Rules**
2. Create new alert rule
3. Set conditions (e.g., CPU > 80%, rooms > 50)
4. Configure notification channels (email, Slack, etc.)

Example alert configuration:
```yaml
# File: tools/mac-livekit/monitoring/alerting-rules.yml
groups:
  - name: livekit_alerts
    interval: 30s
    rules:
      - alert: HighCPUUsage
        expr: rate(process_cpu_seconds_total[5m]) > 0.8
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High CPU usage detected"
          description: "CPU usage above 80% for 5 minutes"
      
      - alert: HighMemoryUsage
        expr: process_resident_memory_bytes > 6e9
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High memory usage"
          description: "Memory usage above 6GB"
      
      - alert: LiveKitDown
        expr: up{job="livekit"} == 0
        for: 1m
        labels:
          severity: critical
        annotations:
          summary: "LiveKit server is down"
          description: "LiveKit has been unreachable for 1 minute"
```

### Accessing Metrics Programmatically

Prometheus metrics endpoint:
```bash
# View raw metrics
curl http://localhost:7880/metrics

# Query specific metric
curl 'http://localhost:9090/api/v1/query?query=livekit_room_total'
```

---

## Reverse Proxy Setup

The setup script offers multiple reverse proxy options. Here's detailed information for each:

### Option 1: ngrok (Recommended for Quick Setup)

**Pros:**
- ✅ Easiest to set up
- ✅ Works instantly
- ✅ HTTPS included
- ✅ Free tier available

**Cons:**
- ❌ URLs change on restart (free tier)
- ❌ Requires account
- ❌ Rate limits on free tier

#### Setup Steps:

1. **Create ngrok account**: [ngrok.com/signup](https://ngrok.com/signup)

2. **Get auth token** from dashboard

3. **Configure ngrok:**
```bash
# Install ngrok
brew install ngrok

# Add auth token
ngrok config add-authtoken YOUR_AUTH_TOKEN

# Start tunnel (automated by scripts)
ngrok http 7880 --subdomain=mylivekit  # paid plan
# or
ngrok http 7880  # free (random URL)
```

4. **Access via URL** shown in ngrok dashboard

#### Persistent URLs (Paid Feature):

```bash
# With paid plan, get permanent subdomain
ngrok http 7880 --subdomain=mylivekit

# Your URL: https://mylivekit.ngrok-free.app
```

### Option 2: Cloudflare Tunnel

**Pros:**
- ✅ Free
- ✅ Custom domain support
- ✅ DDoS protection
- ✅ Persistent URLs

**Cons:**
- ❌ Requires Cloudflare account
- ❌ Need to own a domain
- ❌ More setup steps

#### Setup Steps:

1. **Install cloudflared:**
```bash
brew install cloudflare/cloudflare/cloudflared
```

2. **Authenticate:**
```bash
cloudflared tunnel login
```

3. **Create tunnel:**
```bash
cloudflared tunnel create livekit
```

4. **Configure tunnel:**
```bash
cat > ~/.cloudflared/config.yml << 'EOF'
tunnel: <TUNNEL_ID>
credentials-file: /Users/you/.cloudflared/<TUNNEL_ID>.json

ingress:
  - hostname: livekit.yourdomain.com
    service: http://localhost:7880
  - hostname: monitoring.yourdomain.com
    service: http://localhost:3000
  - service: http_status:404
EOF
```

5. **Create DNS record:**
```bash
cloudflared tunnel route dns livekit livekit.yourdomain.com
```

6. **Start tunnel:**
```bash
cloudflared tunnel run livekit
```

The setup script can run this as a background service.

### Option 3: Tailscale (Private Network)

**Pros:**
- ✅ Most secure (private network)
- ✅ Free for personal use
- ✅ No public exposure
- ✅ Excellent for development

**Cons:**
- ❌ Only accessible to Tailscale network members
- ❌ Not suitable for public services

#### Setup Steps:

1. **Install Tailscale:**
```bash
brew install tailscale
```

2. **Start Tailscale:**
```bash
sudo tailscale up
```

3. **Access via Tailscale IP:**
```bash
# Get your Mac's Tailscale IP
tailscale ip -4

# Access LiveKit from any device on your Tailscale network
# http://100.x.x.x:7880
```

### Option 4: LocalTunnel (Free, No Account)

**Pros:**
- ✅ No account needed
- ✅ Quick testing
- ✅ Free

**Cons:**
- ❌ Less reliable
- ❌ Random URLs
- ❌ Basic features

#### Setup:

```bash
# Install
npm install -g localtunnel

# Start tunnel
lt --port 7880 --subdomain mylivekit
```

### Comparison Matrix

| Feature | ngrok | Cloudflare | Tailscale | LocalTunnel |
|---------|-------|------------|-----------|-------------|
| **Free Tier** | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| **Custom Domain** | 💰 Paid | ✅ Yes | ❌ No | ❌ No |
| **Persistent URL** | 💰 Paid | ✅ Yes | ✅ Yes | ❌ No |
| **HTTPS** | ✅ Yes | ✅ Yes | ⚠️ Optional | ✅ Yes |
| **Setup Difficulty** | ⭐ Easy | ⭐⭐ Medium | ⭐ Easy | ⭐ Easy |
| **Public Access** | ✅ Yes | ✅ Yes | ❌ No | ✅ Yes |
| **Account Required** | ✅ Yes | ✅ Yes | ✅ Yes | ❌ No |
| **Rate Limits** | ⚠️ Yes | ✅ No | ✅ No | ⚠️ Yes |

### Recommendation

- **Development/Testing**: Tailscale or ngrok
- **Production (free)**: Cloudflare Tunnel
- **Production (paid)**: ngrok with custom domain
- **Quick testing**: LocalTunnel

---

## Maintenance & Operations

### Daily Operations

#### Starting Services

```bash
# Start all services
./tools/mac-livekit/scripts/start.sh

# Start specific service
docker compose -f $STORAGE_PATH/livekit/docker-compose.yml start livekit
```

#### Stopping Services

```bash
# Stop all services
./tools/mac-livekit/scripts/stop.sh

# Stop specific service
docker compose -f $STORAGE_PATH/livekit/docker-compose.yml stop livekit
```

#### Restarting Services

```bash
# Restart all services
./tools/mac-livekit/scripts/restart.sh

# Graceful restart (waits for empty rooms)
./tools/mac-livekit/scripts/restart.sh --graceful
```

#### Checking Status

```bash
# Comprehensive status check
./tools/mac-livekit/scripts/status.sh

# Output example:
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
# 📊 LiveKit Status Report
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
# ✅ LiveKit Server: Running
# ✅ Prometheus: Running
# ✅ Grafana: Running
# ✅ Caddy: Running
# 🌐 ngrok Tunnel: Active (https://abc123.ngrok-free.app)
# 
# 📈 Current Load:
# - Active Rooms: 3
# - Total Participants: 12
# - CPU Usage: 45%
# - Memory Usage: 2.1GB / 6GB
# - Storage Used: 15GB / 500GB
# 
# 🕐 Uptime: 3 days, 5 hours
# 📅 Last Restart: 2024-01-24 14:30:00
```

### Viewing Logs

#### Real-time Logs

```bash
# All services
./tools/mac-livekit/scripts/logs.sh

# Follow specific service
./tools/mac-livekit/scripts/logs.sh livekit --follow

# Last 100 lines
./tools/mac-livekit/scripts/logs.sh livekit --tail 100

# Search logs
./tools/mac-livekit/scripts/logs.sh livekit | grep ERROR
```

#### Log Files Location

```bash
# LiveKit logs
$STORAGE_PATH/livekit/logs/livekit.log

# Docker logs
docker logs livekit

# System logs
$STORAGE_PATH/livekit/logs/system.log
```

### Log Rotation

Automatic log rotation is configured:

```yaml
# Docker Compose log rotation (automated)
logging:
  driver: "json-file"
  options:
    max-size: "100m"
    max-file: "10"
```

Manual rotation:
```bash
# Rotate logs now
./tools/mac-livekit/scripts/rotate-logs.sh

# Configure rotation schedule (runs via launchd)
launchctl load ~/Library/LaunchAgents/com.livekit.log-rotate.plist
```

### Backups

#### Automatic Backups

Configured via launchd to run daily:

```bash
# View backup schedule
cat ~/Library/LaunchAgents/com.livekit.backup.plist

# Run backup manually
./tools/mac-livekit/scripts/backup.sh

# Backup includes:
# - Configuration files
# - API keys (encrypted)
# - Grafana dashboards
# - Database (if any)
```

#### Restore from Backup

```bash
# List available backups
./tools/mac-livekit/scripts/backup.sh --list

# Restore specific backup
./tools/mac-livekit/scripts/backup.sh --restore 2024-01-24

# Restore latest
./tools/mac-livekit/scripts/backup.sh --restore latest
```

### Updates

#### Update LiveKit

```bash
# Check for updates
./tools/mac-livekit/scripts/update.sh --check

# Update to latest version
./tools/mac-livekit/scripts/update.sh

# Update to specific version
./tools/mac-livekit/scripts/update.sh --version v1.6.0

# Update process:
# 1. Pulls new Docker image
# 2. Stops current container
# 3. Starts new container
# 4. Verifies health
# 5. Rolls back if issues detected
```

#### Rollback

```bash
# Rollback to previous version
./tools/mac-livekit/scripts/update.sh --rollback

# View version history
./tools/mac-livekit/scripts/update.sh --history
```

### Health Checks

#### Automatic Health Monitoring

```bash
# Automated health checks run every 5 minutes via launchd
cat ~/Library/LaunchAgents/com.livekit.health-check.plist

# Manual health check
./tools/mac-livekit/scripts/health-check.sh

# Output:
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
# 🏥 Health Check Results
# ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
# ✅ LiveKit API responding (200ms)
# ✅ WebSocket connection OK
# ✅ Prometheus metrics available
# ✅ Grafana dashboard accessible
# ✅ External storage mounted
# ✅ Disk space adequate (465GB free)
# ⚠️  CPU usage high (85%)
# ✅ Memory usage normal (55%)
```

#### Email/Slack Alerts

Configure notifications for health check failures:

```bash
# Edit alert configuration
nano $STORAGE_PATH/livekit/config/alerts.conf

# Configure email
ALERT_EMAIL="admin@example.com"
SMTP_SERVER="smtp.gmail.com"
SMTP_PORT="587"

# Configure Slack webhook
SLACK_WEBHOOK="https://hooks.slack.com/services/YOUR/WEBHOOK/URL"

# Test alerts
./tools/mac-livekit/scripts/health-check.sh --test-alert
```

### Performance Monitoring

#### Real-time Performance

```bash
# Watch live stats
./tools/mac-livekit/scripts/stats.sh

# Displays:
# - CPU/Memory usage
# - Network I/O
# - Active connections
# - Room statistics
# - Updates every 2 seconds
```

#### Performance Reports

```bash
# Generate daily performance report
./tools/mac-livekit/scripts/report.sh

# Generate custom range report
./tools/mac-livekit/scripts/report.sh --from "2024-01-20" --to "2024-01-24"

# Report includes:
# - Average response times
# - Peak usage times
# - Error rates
# - Storage growth
# - Exported as PDF/HTML
```

---

## Troubleshooting

### Common Issues

#### 1. Docker Desktop Not Starting

**Symptoms:**
- Docker commands fail
- "Docker daemon not running" error

**Solutions:**
```bash
# Check Docker status
pgrep -x "Docker"

# Restart Docker Desktop
killall Docker && open -a Docker

# Wait 30 seconds then verify
docker ps

# If still fails, reinstall Docker Desktop
```

#### 2. External Storage Not Mounted

**Symptoms:**
- Services fail to start
- "No such file or directory" errors
- Storage path not accessible

**Solutions:**
```bash
# Check if drive is mounted
ls /Volumes/

# Remount drive
diskutil mount LiveKitStorage

# Update storage path in config if drive name changed
nano $STORAGE_PATH/livekit/docker-compose.yml

# Restart services
./tools/mac-livekit/scripts/restart.sh
```

#### 3. Port Already in Use

**Symptoms:**
- "Port 7880 already in use"
- Services won't start

**Solutions:**
```bash
# Find process using port
lsof -i :7880

# Kill process
kill -9 <PID>

# Or change LiveKit port
nano $STORAGE_PATH/livekit/config/livekit-config.yaml
# Change: port: 7880 → port: 7881

# Restart services
./tools/mac-livekit/scripts/restart.sh
```

#### 4. Grafana Dashboard Not Loading

**Symptoms:**
- Dashboard shows "No Data"
- Metrics not appearing

**Solutions:**
```bash
# Check Prometheus is scraping
curl http://localhost:9090/targets

# Verify LiveKit metrics endpoint
curl http://localhost:7880/metrics

# Restart monitoring stack
docker restart prometheus grafana

# Re-import dashboards
./tools/mac-livekit/scripts/setup-dashboards.sh
```

#### 5. Reverse Proxy Connection Failed

**Symptoms:**
- Can't access via public URL
- Tunnel shows as disconnected

**Solutions:**

**For ngrok:**
```bash
# Check ngrok status
curl http://127.0.0.1:4040/api/tunnels

# Restart ngrok
pkill ngrok
ngrok http 7880

# Check auth token
ngrok config check
```

**For Cloudflare Tunnel:**
```bash
# Check tunnel status
cloudflared tunnel info livekit

# View logs
cloudflared tunnel run livekit --loglevel debug

# Restart tunnel
pkill cloudflared
cloudflared tunnel run livekit
```

#### 6. High CPU/Memory Usage

**Symptoms:**
- Mac becomes slow
- Fans running constantly
- Docker using excessive resources

**Solutions:**
```bash
# Check Docker resource usage
docker stats

# Adjust Docker Desktop limits:
# Docker Desktop → Settings → Resources
# - Reduce CPU limit: 4 → 2 cores
# - Reduce Memory: 6GB → 4GB

# Limit LiveKit container resources
# Edit docker-compose.yml:
services:
  livekit:
    deploy:
      resources:
        limits:
          cpus: '2'
          memory: 4G

# Restart
./tools/mac-livekit/scripts/restart.sh
```

#### 7. WebRTC Media Not Flowing

**Symptoms:**
- Calls connect but no audio/video
- "ICE connection failed"

**Solutions:**
```bash
# Check if UDP ports are accessible
# For ngrok, UDP is not supported - use TURN server

# Add TURN server to config
nano $STORAGE_PATH/livekit/config/livekit-config.yaml

# Add:
turn:
  enabled: true
  domain: turn.yourdomain.com
  udp_port: 3478

# For local testing, disable firewall temporarily
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --setglobalstate off

# Test and re-enable
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --setglobalstate on
```

#### 8. Storage Space Running Low

**Symptoms:**
- "No space left on device"
- Services crash randomly

**Solutions:**
```bash
# Check storage usage
df -h $STORAGE_PATH

# Clean up old logs
./tools/mac-livekit/scripts/cleanup.sh --logs

# Remove old recordings
./tools/mac-livekit/scripts/cleanup.sh --recordings --older-than 30

# Clean Docker images/containers
docker system prune -a --volumes
```

### Diagnostic Commands

```bash
# Complete system diagnostic
./tools/mac-livekit/scripts/diagnose.sh

# Generates report with:
# - System information
# - Docker status
# - Service logs
# - Configuration files
# - Network connectivity
# - Storage status
# - Saves to: diagnostics-YYYYMMDD-HHMMSS.tar.gz

# Share this file when asking for help
```

### Getting Help

If you're still stuck:

1. **Run diagnostic script:**
   ```bash
   ./tools/mac-livekit/scripts/diagnose.sh
   ```

2. **Check logs:**
   ```bash
   ./tools/mac-livekit/scripts/logs.sh --all > all-logs.txt
   ```

3. **Create GitHub issue** with:
   - Diagnostic report
   - Error messages
   - Steps to reproduce

4. **Community Support:**
   - [LiveKit Community Slack](https://livekit.io/community)
   - [GitHub Discussions](https://github.com/livekit/livekit/discussions)

---

## Reusing Scripts for Hetzner

These scripts are designed to work on both Mac and Linux (including Hetzner servers) with minimal modifications.

### Cross-Platform Compatibility

The scripts include automatic platform detection:

```bash
# Platform detection (from scripts)
if [[ "$OSTYPE" == "darwin"* ]]; then
    PLATFORM="mac"
    STORAGE_DEFAULT="/Volumes/LiveKitStorage"
elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
    PLATFORM="linux"
    STORAGE_DEFAULT="/opt/livekit-storage"
fi
```

### Using on Hetzner

To deploy on Hetzner using the same scripts:

#### 1. Copy Scripts to Hetzner Server

```bash
# From your Mac, copy scripts to Hetzner
scp -r tools/mac-livekit root@your-hetzner-ip:/root/

# SSH to server
ssh root@your-hetzner-ip

# Rename directory
mv /root/mac-livekit /root/livekit-deploy
```

#### 2. Run Setup on Hetzner

```bash
cd /root/livekit-deploy/scripts

# Run setup (auto-detects Linux)
./setup.sh

# Script will:
# - Detect Linux environment
# - Use /opt/livekit-storage instead of /Volumes/
# - Install Linux packages (apt instead of brew)
# - Set up systemd instead of launchd
# - Configure ufw instead of macOS firewall
# - Rest of the setup is identical
```

#### 3. Differences on Hetzner

| Feature | Mac | Hetzner (Linux) |
|---------|-----|-----------------|
| **Storage Path** | `/Volumes/LiveKitStorage` | `/opt/livekit-storage` |
| **Package Manager** | Homebrew | apt |
| **Service Manager** | launchd | systemd |
| **Firewall** | macOS firewall | ufw |
| **Reverse Proxy** | ngrok/Tailscale | Caddy + DNS |

#### 4. Hetzner-Specific Features

The scripts enable additional features on Hetzner:

- **Public IP detection**: Automatically configures `node_ip`
- **Caddy auto-SSL**: Let's Encrypt certificates
- **Direct UDP access**: No need for TURN server
- **systemd integration**: Auto-start on boot

### Script Adaptations

Key sections that adapt automatically:

```bash
# Storage path
if [[ $PLATFORM == "mac" ]]; then
    STORAGE="/Volumes/LiveKitStorage"
else
    STORAGE="/opt/livekit-storage"
fi

# Package installation
if [[ $PLATFORM == "mac" ]]; then
    brew install jq curl
else
    apt-get update && apt-get install -y jq curl
fi

# Service management
if [[ $PLATFORM == "mac" ]]; then
    # Use launchd
    launchctl load ~/Library/LaunchAgents/com.livekit.plist
else
    # Use systemd
    systemctl enable livekit
    systemctl start livekit
fi
```

### Testing Scripts on Both Platforms

```bash
# Mac testing
./tools/mac-livekit/scripts/setup.sh

# Linux/Hetzner testing (in VM)
multipass launch --name livekit-test
multipass shell livekit-test
# Run setup scripts
```

---

## Advanced Configuration

### Custom Domain Setup

If you want to use your own domain with Let's Encrypt SSL:

1. **Point DNS to your Mac's public IP** (requires port forwarding on router)

2. **Update configuration:**
```bash
nano $STORAGE_PATH/livekit/config/docker-compose.yml

# Add Caddy with custom domain
environment:
  - LIVEKIT_DOMAIN=livekit.yourdomain.com
```

3. **Configure Caddy for SSL:**
```bash
nano $STORAGE_PATH/caddy/Caddyfile

# Add:
livekit.yourdomain.com {
    encode gzip
    reverse_proxy livekit:7880
}
```

4. **Restart services:**
```bash
./tools/mac-livekit/scripts/restart.sh
```

### Load Balancing Multiple Instances

To run multiple LiveKit instances:

```bash
# Scale up LiveKit
docker compose up -d --scale livekit=3

# Configure load balancer (Caddy)
nano $STORAGE_PATH/caddy/Caddyfile

{$LIVEKIT_DOMAIN} {
    encode gzip
    reverse_proxy livekit-1:7880 livekit-2:7880 livekit-3:7880 {
        lb_policy least_conn
        health_path /health
        health_interval 30s
    }
}
```

### Redis for Multi-Instance Coordination

```bash
# Add Redis to docker-compose.yml
services:
  redis:
    image: redis:7-alpine
    container_name: livekit-redis
    restart: unless-stopped
    volumes:
      - $STORAGE_PATH/redis:/data

# Update LiveKit config
nano $STORAGE_PATH/livekit/config/livekit-config.yaml

# Add:
redis:
  address: redis:6379
  db: 0
```

### Recording Storage

Configure automatic recording with external storage:

```yaml
# In livekit-config.yaml
recording:
  enabled: true
  storage:
    type: filesystem
    path: /recordings
  # Or use S3
  # storage:
  #   type: s3
  #   access_key: YOUR_KEY
  #   secret_key: YOUR_SECRET
  #   region: us-west-2
  #   bucket: my-recordings
```

---

## Uninstallation

To completely remove LiveKit and all components:

```bash
# Stop all services
./tools/mac-livekit/scripts/stop.sh

# Run uninstall script
./tools/mac-livekit/scripts/uninstall.sh

# Script will:
# 1. Stop and remove all containers
# 2. Remove Docker images
# 3. Remove launchd agents
# 4. Optionally remove data (prompts for confirmation)
# 5. Remove scripts

# Manual cleanup (if needed)
# - External storage data: manually delete /Volumes/LiveKitStorage/livekit
# - Docker volumes: docker volume prune
# - Docker images: docker image prune -a
```

---

## Summary

This guide provided a complete solution for running LiveKit on macOS with:

✅ **External storage** to save space on main drive
✅ **Comprehensive monitoring** with Grafana dashboards  
✅ **Multiple reverse proxy options** (no router config needed)
✅ **Automated deployment scripts** for easy setup
✅ **Log management** with rotation and caching
✅ **Health monitoring** and alerting
✅ **Maintenance scripts** for daily operations
✅ **Troubleshooting guide** for common issues
✅ **Cross-platform scripts** reusable on Hetzner/Linux

### Quick Reference

```bash
# Start services
./tools/mac-livekit/scripts/start.sh

# Stop services
./tools/mac-livekit/scripts/stop.sh

# Check status
./tools/mac-livekit/scripts/status.sh

# View logs
./tools/mac-livekit/scripts/logs.sh

# Health check
./tools/mac-livekit/scripts/health-check.sh

# Backup
./tools/mac-livekit/scripts/backup.sh

# Update
./tools/mac-livekit/scripts/update.sh
```

### Resources

- **LiveKit Documentation**: https://docs.livekit.io
- **Docker Desktop**: https://www.docker.com/products/docker-desktop
- **Grafana Dashboards**: https://grafana.com/grafana/dashboards
- **ngrok Documentation**: https://ngrok.com/docs
- **Community Support**: https://livekit.io/community

---

**Next Steps:**

1. Run the setup script: `./tools/mac-livekit/scripts/setup.sh`
2. Open monitoring dashboard: `http://localhost:3000`
3. Test your first LiveKit connection
4. Explore the advanced features

Enjoy your LiveKit deployment! 🚀
