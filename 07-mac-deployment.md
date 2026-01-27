# Mac LiveKit Deployment Guide

## Overview

Complete end-to-end solution for deploying LiveKit media server on macOS with external storage, monitoring dashboard, and secure public access via reverse proxy tunneling.

## Features

- ✅ **Automated Installation**: Single-command setup with Homebrew
- ✅ **External Storage**: All data stored on external drive to save space
- ✅ **Monitoring Stack**: Grafana + Prometheus + Loki for comprehensive monitoring
- ✅ **Web Dashboard**: Access monitoring via `http://localhost:3000`
- ✅ **Secure Public Access**: Cloudflare Tunnel for exposure without router configuration
- ✅ **Log Management**: Automated log rotation and aggregation
- ✅ **Health Checks**: Automated monitoring and alerting
- ✅ **Cross-Platform Scripts**: Same scripts work on Mac and Linux (Hetzner)
- ✅ **Troubleshooting Tools**: Built-in diagnostic and repair scripts

## Prerequisites

- **macOS**: 11.0 (Big Sur) or later
- **External Storage**: Minimum 50GB available (SSD recommended)
- **RAM**: Minimum 8GB (16GB recommended)
- **Network**: Stable internet connection
- **Domain** (optional): For custom domain via Cloudflare Tunnel

## Architecture

```
┌─────────────────────────────────────────────────────┐
│                    Mac Host                         │
│  ┌──────────────────────────────────────────────┐   │
│  │        Cloudflare Tunnel                     │   │
│  │  - Public HTTPS access                       │   │
│  │  - No router configuration needed            │   │
│  │  - Auto-renewed SSL certificates             │   │
│  └──────────────┬───────────────────────────────┘   │
│                 │                                    │
│  ┌──────────────┴───────────────────────────────┐   │
│  │        Caddy (Local Reverse Proxy)           │   │
│  │  - Routes traffic to services                │   │
│  │  - WebSocket support for WebRTC              │   │
│  └──────────────┬───────────────────────────────┘   │
│                 │                                    │
│  ┌──────────────┴───────────────┬─────────────────┐ │
│  │      LiveKit Server          │   Monitoring    │ │
│  │  - SFU Media Router          │   - Grafana     │ │
│  │  - WebRTC signaling          │   - Prometheus  │ │
│  │  - Room management           │   - Loki        │ │
│  │                              │   - Promtail    │ │
│  └──────────────────────────────┴─────────────────┘ │
│                                                      │
│  External Storage: /Volumes/ExternalDrive/livekit/  │
│  ├── config/         (Configuration files)          │
│  ├── data/           (Persistent data)              │
│  ├── logs/           (Application logs)             │
│  ├── monitoring/     (Metrics & dashboards)         │
│  └── backups/        (Automated backups)            │
└─────────────────────────────────────────────────────┘
```

## Quick Start

### 1. Prepare External Storage

```bash
# Connect your external drive and verify it's mounted
diskutil list

# Note the mount point, typically:
# /Volumes/YourDriveName

# Set environment variable (add to ~/.zshrc for persistence)
export LIVEKIT_STORAGE="/Volumes/YourDriveName/livekit"
```

### 2. Run Automated Installation

```bash
# Clone the repository
git clone https://github.com/SimonDarksideJ/ResearchBucket.git
cd ResearchBucket/deployment-tools

# Run installation script
./scripts/install-mac.sh

# Or with custom storage location
./scripts/install-mac.sh --storage /Volumes/MyDrive/livekit
```

The installation script will:
1. Install Homebrew (if not present)
2. Install Docker Desktop for Mac
3. Install required tools (jq, yq, etc.)
4. Create directory structure on external storage
5. Generate LiveKit API keys
6. Configure all services
7. Start Docker containers
8. Set up monitoring dashboards
9. Display access URLs and credentials

### 3. Access Services

After installation completes:

```
✅ LiveKit Server: http://localhost:7880
✅ Monitoring Dashboard: http://localhost:3000
   Username: admin
   Password: (displayed during installation)

✅ Prometheus: http://localhost:9090
✅ Log Viewer: http://localhost:3000/explore (Loki)
```

### 4. Setup Public Access (Optional)

```bash
# Install and configure Cloudflare Tunnel
./scripts/setup-cloudflare-tunnel.sh

# Follow the prompts to authenticate
# You'll get a public URL like: https://livekit-random.trycloudflare.com
```

## Manual Installation

If you prefer step-by-step manual installation:

### 1. Install Dependencies

```bash
# Install Homebrew (if not installed)
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"

# Install required packages
brew install --cask docker
brew install jq yq cloudflared

# Start Docker Desktop
open -a Docker
```

### 2. Prepare Directory Structure

```bash
# Set your external storage path
export LIVEKIT_STORAGE="/Volumes/YourDrive/livekit"

# Create directory structure
mkdir -p "$LIVEKIT_STORAGE"/{config,data,logs,monitoring/{dashboards,prometheus,grafana,loki},backups}

# Create symlink to home directory for easy access
ln -s "$LIVEKIT_STORAGE" ~/livekit
```

### 3. Generate LiveKit Keys

```bash
# Generate API keys
docker run --rm livekit/livekit-server generate-keys

# Save the output - you'll need API_KEY and API_SECRET
```

### 4. Create Configuration Files

See the [Configuration Files](#configuration-files) section below for all necessary config files.

### 5. Start Services

```bash
cd ~/livekit
docker compose up -d

# Verify all services are running
docker compose ps

# View logs
docker compose logs -f
```

## Configuration Files

### Docker Compose Configuration

Location: `$LIVEKIT_STORAGE/docker-compose.yml`

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
      - "7882:7882/udp"
      - "50000-50200:50000-50200/udp"
    volumes:
      - ./config/livekit.yaml:/etc/livekit.yaml:ro
      - ./logs/livekit:/logs
      - ./data/livekit:/data
    command: --config /etc/livekit.yaml --bind 0.0.0.0
    environment:
      - LIVEKIT_CONFIG=/etc/livekit.yaml
    networks:
      - livekit-net
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"

  caddy:
    image: caddy:2-alpine
    container_name: caddy
    restart: unless-stopped
    ports:
      - "80:80"
      - "443:443"
      - "443:443/udp"
    volumes:
      - ./config/Caddyfile:/etc/caddy/Caddyfile:ro
      - ./data/caddy/data:/data
      - ./data/caddy/config:/config
    networks:
      - livekit-net
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"

  prometheus:
    image: prom/prometheus:latest
    container_name: prometheus
    restart: unless-stopped
    ports:
      - "9090:9090"
    volumes:
      - ./monitoring/prometheus/prometheus.yml:/etc/prometheus/prometheus.yml:ro
      - ./monitoring/prometheus/alerts.yml:/etc/prometheus/alerts.yml:ro
      - ./data/prometheus:/prometheus
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'
      - '--storage.tsdb.path=/prometheus'
      - '--storage.tsdb.retention.time=30d'
      - '--web.console.libraries=/usr/share/prometheus/console_libraries'
      - '--web.console.templates=/usr/share/prometheus/consoles'
      - '--web.enable-lifecycle'
    networks:
      - livekit-net
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"

  grafana:
    image: grafana/grafana:latest
    container_name: grafana
    restart: unless-stopped
    ports:
      - "3000:3000"
    volumes:
      - ./monitoring/grafana/datasources.yml:/etc/grafana/provisioning/datasources/datasources.yml:ro
      - ./monitoring/grafana/dashboards.yml:/etc/grafana/provisioning/dashboards/dashboards.yml:ro
      - ./monitoring/dashboards:/var/lib/grafana/dashboards:ro
      - ./data/grafana:/var/lib/grafana
    environment:
      - GF_SECURITY_ADMIN_USER=admin
      - GF_SECURITY_ADMIN_PASSWORD=${GRAFANA_PASSWORD:-changeme}
      - GF_USERS_ALLOW_SIGN_UP=false
      - GF_SERVER_ROOT_URL=http://localhost:3000
      - GF_INSTALL_PLUGINS=grafana-piechart-panel,grafana-clock-panel
    networks:
      - livekit-net
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"

  loki:
    image: grafana/loki:latest
    container_name: loki
    restart: unless-stopped
    ports:
      - "3100:3100"
    volumes:
      - ./monitoring/loki/loki-config.yml:/etc/loki/local-config.yaml:ro
      - ./data/loki:/loki
    command: -config.file=/etc/loki/local-config.yaml
    networks:
      - livekit-net
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"

  promtail:
    image: grafana/promtail:latest
    container_name: promtail
    restart: unless-stopped
    volumes:
      - ./logs:/var/log/livekit:ro
      - ./monitoring/loki/promtail-config.yml:/etc/promtail/config.yml:ro
      - /var/lib/docker/containers:/var/lib/docker/containers:ro
      - /var/log:/var/log:ro
    command: -config.file=/etc/promtail/config.yml
    networks:
      - livekit-net
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"

  node-exporter:
    image: prom/node-exporter:latest
    container_name: node-exporter
    restart: unless-stopped
    ports:
      - "9100:9100"
    command:
      - '--path.rootfs=/host'
    volumes:
      - '/:/host:ro,rslave'
    networks:
      - livekit-net
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"

  cadvisor:
    image: gcr.io/cadvisor/cadvisor:latest
    container_name: cadvisor
    restart: unless-stopped
    ports:
      - "8080:8080"
    volumes:
      - /:/rootfs:ro
      - /var/run:/var/run:ro
      - /sys:/sys:ro
      - /var/lib/docker:/var/lib/docker:ro
      - /dev/disk:/dev/disk:ro
    privileged: true
    networks:
      - livekit-net
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"

networks:
  livekit-net:
    driver: bridge
```

### LiveKit Configuration

Location: `$LIVEKIT_STORAGE/config/livekit.yaml`

```yaml
# Generated by deployment scripts
port: 7880
bind_addresses:
  - "0.0.0.0"

rtc:
  # TCP fallback port for WebRTC
  tcp_port: 7881
  # Port range for WebRTC UDP
  port_range_start: 50000
  port_range_end: 50200
  # Use external IP (important for Mac with NAT)
  use_external_ip: true
  # ICE server configuration
  ice_servers:
    - urls:
        - stun:stun.l.google.com:19302

# API Keys - REPLACE WITH YOUR GENERATED KEYS
keys:
  API_KEY: your_api_key_here
  API_SECRET: your_api_secret_here

# Logging configuration
logging:
  level: info
  sample: false
  # Write logs to file
  pion_level: warn
  file:
    filename: /logs/livekit.log
    max_size: 100
    max_backups: 3

# Room configuration
room:
  # Automatically create rooms when clients join
  auto_create: true
  # Maximum participants per room
  max_participants: 100
  # Timeout for empty rooms (seconds)
  empty_timeout: 300
  # Departure timeout
  departure_timeout: 20

# Region (optional)
region: "mac-local"

# Node ID for multi-node deployments
# node_id: "livekit-mac-1"

# Turn server (optional, uncomment if using external TURN)
# turn:
#   enabled: true
#   domain: turn.yourdomain.com
#   tls_port: 5349
#   udp_port: 3478
#   external_tls: true

# Development mode settings (disable in production)
development: false

# Webhook configuration (optional)
# webhook:
#   urls:
#     - http://your-app.com/livekit/webhook
#   api_key: webhook_api_key

# Metrics for Prometheus
prometheus:
  port: 6789
```

### Caddy Configuration

Location: `$LIVEKIT_STORAGE/config/Caddyfile`

```caddyfile
# Local reverse proxy configuration
{
    # Local serving only
    local_certs
    auto_https off
}

:80 {
    # Redirect root to Grafana
    redir / /grafana 302
    
    # LiveKit WebRTC endpoint
    handle_path /livekit/* {
        reverse_proxy livekit:7880 {
            header_up Host {host}
            header_up X-Real-IP {remote}
            header_up X-Forwarded-For {remote}
            header_up X-Forwarded-Proto {scheme}
        }
    }
    
    # Grafana dashboard
    handle_path /grafana/* {
        reverse_proxy grafana:3000 {
            header_up Host {host}
            header_up X-Real-IP {remote}
            header_up X-Forwarded-For {remote}
        }
    }
    
    # Prometheus metrics
    handle_path /prometheus/* {
        reverse_proxy prometheus:9090
    }
    
    # Direct LiveKit access
    handle /ws* {
        reverse_proxy livekit:7880 {
            header_up Upgrade {http.request.header.Upgrade}
            header_up Connection {http.request.header.Connection}
        }
    }
    
    # Health check endpoint
    handle /health {
        respond "OK" 200
    }
}
```

### Prometheus Configuration

Location: `$LIVEKIT_STORAGE/monitoring/prometheus/prometheus.yml`

```yaml
global:
  scrape_interval: 15s
  evaluation_interval: 15s
  external_labels:
    cluster: 'livekit-mac'
    environment: 'development'

# Alerting configuration
alerting:
  alertmanagers:
    - static_configs:
        - targets: []
          # - alertmanager:9093

# Load alert rules
rule_files:
  - "alerts.yml"

# Scrape configurations
scrape_configs:
  # LiveKit metrics
  - job_name: 'livekit'
    static_configs:
      - targets: ['livekit:6789']
        labels:
          service: 'livekit-server'
    
  # Prometheus self-monitoring
  - job_name: 'prometheus'
    static_configs:
      - targets: ['localhost:9090']
  
  # Node exporter (system metrics)
  - job_name: 'node'
    static_configs:
      - targets: ['node-exporter:9100']
        labels:
          service: 'system'
  
  # cAdvisor (container metrics)
  - job_name: 'cadvisor'
    static_configs:
      - targets: ['cadvisor:8080']
        labels:
          service: 'containers'
  
  # Caddy metrics (if enabled)
  - job_name: 'caddy'
    static_configs:
      - targets: ['caddy:2019']
        labels:
          service: 'reverse-proxy'
```

### Prometheus Alerts

Location: `$LIVEKIT_STORAGE/monitoring/prometheus/alerts.yml`

```yaml
groups:
  - name: livekit_alerts
    interval: 30s
    rules:
      # High CPU usage
      - alert: HighCPUUsage
        expr: rate(process_cpu_seconds_total{job="livekit"}[5m]) > 0.8
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "LiveKit high CPU usage"
          description: "LiveKit server CPU usage is above 80% for 5 minutes"
      
      # High memory usage
      - alert: HighMemoryUsage
        expr: process_resident_memory_bytes{job="livekit"} / 1024 / 1024 / 1024 > 6
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "LiveKit high memory usage"
          description: "LiveKit server using more than 6GB RAM"
      
      # Service down
      - alert: LiveKitDown
        expr: up{job="livekit"} == 0
        for: 1m
        labels:
          severity: critical
        annotations:
          summary: "LiveKit service is down"
          description: "LiveKit server is not responding"
      
      # High number of failed connections
      - alert: HighConnectionFailures
        expr: rate(livekit_connection_failures_total[5m]) > 0.1
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High connection failure rate"
          description: "LiveKit connection failure rate is high"

  - name: system_alerts
    interval: 30s
    rules:
      # Disk space low
      - alert: LowDiskSpace
        expr: (node_filesystem_avail_bytes{mountpoint=~"/.*"} / node_filesystem_size_bytes) * 100 < 15
        for: 10m
        labels:
          severity: warning
        annotations:
          summary: "Low disk space"
          description: "Disk space is below 15%"
      
      # Container down
      - alert: ContainerDown
        expr: up == 0
        for: 2m
        labels:
          severity: warning
        annotations:
          summary: "Container is down"
          description: "{{ $labels.job }} container is not responding"
```

### Grafana Data Sources

Location: `$LIVEKIT_STORAGE/monitoring/grafana/datasources.yml`

```yaml
apiVersion: 1

datasources:
  - name: Prometheus
    type: prometheus
    access: proxy
    url: http://prometheus:9090
    isDefault: true
    editable: true
    jsonData:
      httpMethod: GET
      timeInterval: 15s

  - name: Loki
    type: loki
    access: proxy
    url: http://loki:3100
    editable: true
    jsonData:
      maxLines: 1000
```

### Grafana Dashboards Provisioning

Location: `$LIVEKIT_STORAGE/monitoring/grafana/dashboards.yml`

```yaml
apiVersion: 1

providers:
  - name: 'LiveKit Dashboards'
    orgId: 1
    folder: 'LiveKit'
    type: file
    disableDeletion: false
    updateIntervalSeconds: 10
    allowUiUpdates: true
    options:
      path: /var/lib/grafana/dashboards
      foldersFromFilesStructure: true
```

### Loki Configuration

Location: `$LIVEKIT_STORAGE/monitoring/loki/loki-config.yml`

```yaml
auth_enabled: false

server:
  http_listen_port: 3100
  grpc_listen_port: 9096

common:
  path_prefix: /loki
  storage:
    filesystem:
      chunks_directory: /loki/chunks
      rules_directory: /loki/rules
  replication_factor: 1
  ring:
    instance_addr: 127.0.0.1
    kvstore:
      store: inmemory

schema_config:
  configs:
    - from: 2020-10-24
      store: boltdb-shipper
      object_store: filesystem
      schema: v11
      index:
        prefix: index_
        period: 24h

limits_config:
  retention_period: 168h  # 7 days
  reject_old_samples: true
  reject_old_samples_max_age: 168h
  ingestion_rate_mb: 10
  ingestion_burst_size_mb: 20

chunk_store_config:
  max_look_back_period: 168h

table_manager:
  retention_deletes_enabled: true
  retention_period: 168h

compactor:
  working_directory: /loki/compactor
  shared_store: filesystem
  compaction_interval: 10m
  retention_enabled: true
  retention_delete_delay: 2h
  retention_delete_worker_count: 150
```

### Promtail Configuration

Location: `$LIVEKIT_STORAGE/monitoring/loki/promtail-config.yml`

```yaml
server:
  http_listen_port: 9080
  grpc_listen_port: 0

positions:
  filename: /tmp/positions.yaml

clients:
  - url: http://loki:3100/loki/api/v1/push

scrape_configs:
  # LiveKit application logs
  - job_name: livekit-logs
    static_configs:
      - targets:
          - localhost
        labels:
          job: livekit-logs
          __path__: /var/log/livekit/*.log
    pipeline_stages:
      - regex:
          expression: '^(?P<timestamp>\S+)\s+(?P<level>\S+)\s+(?P<message>.*)$'
      - labels:
          level:
      - timestamp:
          source: timestamp
          format: RFC3339

  # Docker container logs
  - job_name: docker-logs
    docker_sd_configs:
      - host: unix:///var/run/docker.sock
        refresh_interval: 5s
    relabel_configs:
      - source_labels: ['__meta_docker_container_name']
        regex: '/(.*)'
        target_label: 'container'
      - source_labels: ['__meta_docker_container_log_stream']
        target_label: 'stream'
    pipeline_stages:
      - docker: {}

  # System logs
  - job_name: system-logs
    static_configs:
      - targets:
          - localhost
        labels:
          job: system-logs
          __path__: /var/log/*.log
```

## Deployment Scripts

All deployment scripts are located in `deployment-tools/scripts/` and work on both Mac and Linux.

### Main Installation Script

See: [`deployment-tools/scripts/install-mac.sh`](#)

Key features:
- Detects macOS version and architecture (Intel/Apple Silicon)
- Installs dependencies via Homebrew
- Configures external storage
- Generates secure API keys
- Sets up all configuration files
- Starts services and validates health
- Provides setup summary with access URLs

### Cloudflare Tunnel Setup

See: [`deployment-tools/scripts/setup-cloudflare-tunnel.sh`](#)

Exposes your local LiveKit server to the internet securely without opening ports on your router.

### Health Check Script

See: [`deployment-tools/scripts/health-check.sh`](#)

Monitors all services and provides detailed status report.

### Backup Script

See: [`deployment-tools/scripts/backup.sh`](#)

Automated backup of configurations and data.

### Update Script

See: [`deployment-tools/scripts/update.sh`](#)

Updates all Docker images and restarts services with zero downtime.

## Monitoring Dashboard

### Access Grafana

1. Open browser: `http://localhost:3000`
2. Login with credentials from installation
3. Navigate to **LiveKit Dashboard**

### Key Panels

1. **System Overview**
   - CPU, Memory, Disk, Network usage
   - Container health status
   - Uptime tracking

2. **LiveKit Metrics**
   - Active rooms and participants
   - WebRTC connection status
   - Bandwidth usage (upload/download)
   - Packet loss and jitter

3. **Performance**
   - Response times
   - Error rates
   - Queue depths

4. **Logs**
   - Real-time log streaming
   - Log levels and filtering
   - Search and analysis

### Setting Up Alerts

1. Go to **Alerting** → **Notification channels**
2. Add email/Slack/Discord webhook
3. Configure alert rules in dashboard panels
4. Test notifications

## Public Access via Cloudflare Tunnel

### Setup

```bash
cd ~/livekit
./scripts/setup-cloudflare-tunnel.sh
```

Follow the interactive prompts:
1. Authenticate with Cloudflare (browser opens)
2. Choose a subdomain or use random
3. Select services to expose
4. Get public URLs

### Example Output

```
✅ Cloudflare Tunnel configured successfully!

Public URLs:
  LiveKit Server: https://livekit-abc123.trycloudflare.com
  Grafana Dashboard: https://grafana-abc123.trycloudflare.com

Add to your LiveKit client configuration:
  url: "wss://livekit-abc123.trycloudflare.com"
```

### Custom Domain

```bash
# Configure custom domain
./scripts/setup-cloudflare-tunnel.sh --domain livekit.yourdomain.com

# Requires DNS configured in Cloudflare
```

## Troubleshooting

### Services Won't Start

```bash
# Check Docker Desktop is running
docker ps

# View all container logs
cd ~/livekit
docker compose logs

# Check specific service
docker compose logs livekit
docker compose logs grafana
```

### External Storage Issues

```bash
# Verify drive is mounted
ls -la /Volumes/

# Check permissions
ls -la "$LIVEKIT_STORAGE"

# If permission denied, fix ownership
sudo chown -R $(whoami) "$LIVEKIT_STORAGE"
```

### LiveKit Connection Failures

```bash
# Run diagnostic script
./scripts/diagnose.sh

# Common issues:
# 1. Firewall blocking ports
# 2. Docker network issues
# 3. Incorrect API keys

# Test LiveKit connectivity
curl http://localhost:7880/

# Check logs for errors
docker compose logs livekit | grep -i error
```

### Monitoring Dashboard Not Loading

```bash
# Restart Grafana
docker compose restart grafana

# Check Grafana logs
docker compose logs grafana

# Reset Grafana password
docker compose exec grafana grafana-cli admin reset-admin-password newpassword
```

### High CPU/Memory Usage

```bash
# Check resource usage
docker stats

# View top processes
docker compose top

# Restart problematic container
docker compose restart livekit
```

### Cloudflare Tunnel Issues

```bash
# Check tunnel status
docker compose logs cloudflared

# Restart tunnel
docker compose restart cloudflared

# Re-authenticate
cloudflared tunnel login
```

### Port Conflicts

```bash
# Check what's using ports
lsof -i :7880
lsof -i :3000
lsof -i :9090

# Kill conflicting process
kill -9 <PID>

# Or change ports in docker-compose.yml
```

### Disk Space Running Low

```bash
# Check disk usage
df -h "$LIVEKIT_STORAGE"

# Clean up old logs
./scripts/cleanup-logs.sh

# Prune Docker system
docker system prune -a --volumes

# Reduce log retention
# Edit monitoring/loki/loki-config.yml
# Set retention_period to lower value (e.g., 72h)
```

### Mac-Specific Issues

**Apple Silicon (M1/M2) Compatibility**
```bash
# Some images may need platform specification
# Edit docker-compose.yml, add to services:
platform: linux/amd64  # or linux/arm64
```

**Docker Desktop Resource Limits**
1. Open Docker Desktop preferences
2. Go to Resources
3. Increase CPU/Memory allocation
4. Apply & Restart

**Network Issues on Mac**
```bash
# Reset Docker network
docker compose down
docker network prune
docker compose up -d
```

## Maintenance

### Daily Operations

```bash
# Check health status
./scripts/health-check.sh

# View recent logs
docker compose logs --tail=100 -f

# Monitor resource usage
docker stats
```

### Weekly Tasks

```bash
# Backup configuration
./scripts/backup.sh

# Check for updates
./scripts/update.sh --check

# Review alerts in Grafana
open http://localhost:3000/alerting/list
```

### Monthly Tasks

```bash
# Update all images
./scripts/update.sh

# Clean up old logs and data
./scripts/cleanup.sh --older-than 30

# Review and optimize storage
du -sh "$LIVEKIT_STORAGE"/*

# Export Grafana dashboards
./scripts/export-dashboards.sh
```

### Automated Maintenance

Add to cron (or use LaunchAgent on Mac):

```bash
# Create LaunchAgent
cat > ~/Library/LaunchAgents/com.livekit.maintenance.plist <<'EOF'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key>
    <string>com.livekit.maintenance</string>
    <key>ProgramArguments</key>
    <array>
        <string>/Users/YOUR_USERNAME/livekit/scripts/daily-maintenance.sh</string>
    </array>
    <key>StartCalendarInterval</key>
    <dict>
        <key>Hour</key>
        <integer>2</integer>
        <key>Minute</key>
        <integer>0</integer>
    </dict>
    <key>StandardOutPath</key>
    <string>/tmp/livekit-maintenance.log</string>
    <key>StandardErrorPath</key>
    <string>/tmp/livekit-maintenance-error.log</string>
</dict>
</plist>
EOF

# Load LaunchAgent
launchctl load ~/Library/LaunchAgents/com.livekit.maintenance.plist
```

## Migrating to Hetzner Cloud

When you're ready to move to production on Hetzner:

```bash
# 1. Export your configuration
./scripts/export-config.sh --output hetzner-config.tar.gz

# 2. Copy to your Hetzner server
scp hetzner-config.tar.gz root@YOUR_HETZNER_IP:/opt/

# 3. On Hetzner server, extract and deploy
ssh root@YOUR_HETZNER_IP
cd /opt
tar -xzf hetzner-config.tar.gz
cd deployment-tools
./scripts/deploy.sh --platform linux --import /opt/config
```

The scripts automatically handle platform differences (paths, networking, etc.).

## Performance Tuning

### Mac-Specific Optimizations

**Docker Desktop Settings**
- CPUs: Allocate 50-75% of available cores
- Memory: Allocate 50-75% of available RAM
- Swap: 2GB minimum
- Disk image location: External SSD (if possible)

**Network Performance**
```bash
# Increase UDP buffer sizes (requires Docker restart)
# Add to Docker Desktop → Settings → Docker Engine:
{
  "dns": ["8.8.8.8", "8.8.4.4"],
  "experimental": true,
  "debug": false
}
```

### LiveKit Optimizations

Edit `config/livekit.yaml`:

```yaml
# For better performance on Mac
rtc:
  # Reduce UDP port range for better NAT traversal
  port_range_start: 50000
  port_range_end: 50100  # Reduced from 50200
  
  # Enable TCP fallback
  tcp_port: 7881
  
  # Optimize for local network
  use_external_ip: false  # Set to true if using tunnel

# Adjust based on your needs
room:
  max_participants: 50  # Reduce for better performance
  empty_timeout: 180    # Shorter timeout to free resources
```

## Security Considerations

### Local Network Security

```bash
# Use strong Grafana password
docker compose exec grafana grafana-cli admin reset-admin-password "YourStrongPasswordHere"

# Restrict access to monitoring
# Edit docker-compose.yml to bind Grafana to localhost only:
# ports:
#   - "127.0.0.1:3000:3000"
```

### Cloudflare Tunnel Security

- Tunnel traffic is encrypted end-to-end
- No ports opened on your router
- Cloudflare's DDoS protection included
- Access policies can be configured in Cloudflare dashboard

### API Key Rotation

```bash
# Generate new keys
docker run --rm livekit/livekit-server generate-keys

# Update config/livekit.yaml with new keys
# Restart LiveKit
docker compose restart livekit
```

### Backup Security

```bash
# Encrypt backups before offsite storage
tar -czf - "$LIVEKIT_STORAGE/backups" | \
  openssl enc -aes-256-cbc -salt -out backup-encrypted.tar.gz.enc

# Decrypt when needed
openssl enc -d -aes-256-cbc -in backup-encrypted.tar.gz.enc | tar -xzf -
```

## FAQ

### Q: Can I run this on an older Mac?

A: Minimum requirements are macOS 11.0+ with 8GB RAM. For M1/M2 Macs, all images support ARM64. For Intel Macs, most images work but some may need `platform: linux/amd64` specified.

### Q: What if my external drive disconnects?

A: Docker containers will stop gracefully. When you reconnect the drive, run `docker compose up -d` to restart services. Consider setting up alerts in Grafana for disk unavailability.

### Q: How much bandwidth does LiveKit use?

A: Approximately 2-4 Mbps per participant for 720p video. A 10-person call uses 20-40 Mbps. Ensure your internet connection can handle expected load.

### Q: Can I use this for production?

A: This setup is suitable for development, testing, and small-scale production (<20 concurrent users). For larger deployments, migrate to Hetzner Cloud or other production hosting.

### Q: How do I add custom TURN servers?

A: Edit `config/livekit.yaml` and add your TURN server configuration under the `turn:` section. Restart LiveKit after changes.

### Q: Can I record sessions?

A: Yes! Add Egress service to docker-compose.yml. See [LiveKit Egress documentation](https://docs.livekit.io/egress/overview/) for details.

### Q: How do I update LiveKit to latest version?

A: Run `./scripts/update.sh` which pulls latest images and restarts services with zero downtime.

## Additional Resources

- **LiveKit Documentation**: https://docs.livekit.io/
- **Docker Desktop for Mac**: https://docs.docker.com/desktop/mac/install/
- **Cloudflare Tunnel**: https://developers.cloudflare.com/cloudflare-one/connections/connect-apps/
- **Grafana Dashboards**: https://grafana.com/grafana/dashboards/
- **Prometheus Query Examples**: https://prometheus.io/docs/prometheus/latest/querying/examples/

## Support

For issues with:
- **LiveKit**: [LiveKit Community](https://livekit.io/community)
- **Deployment Scripts**: Open issue in this repository
- **Docker Desktop**: [Docker Forums](https://forums.docker.com/)
- **Cloudflare Tunnel**: [Cloudflare Community](https://community.cloudflare.com/)

---

**Next Steps**: Run `./scripts/install-mac.sh` to get started!
