# LiveKit Mac Deployment Tools

Automated scripts for deploying, managing, and monitoring LiveKit on macOS (and Linux).

## Quick Start

```bash
# Run the setup script
./scripts/setup.sh

# Follow the interactive prompts to configure:
# - External storage location
# - API credentials  
# - Monitoring dashboard
# - Reverse proxy options
```

## Directory Structure

```
tools/mac-livekit/
├── scripts/                    # Deployment and management scripts
│   ├── setup.sh               # Main installation script (interactive)
│   ├── start.sh               # Start all services
│   ├── stop.sh                # Stop all services
│   ├── restart.sh             # Restart services
│   ├── status.sh              # Check service status
│   ├── logs.sh                # View logs
│   ├── backup.sh              # Backup configuration
│   ├── update.sh              # Update LiveKit
│   ├── health-check.sh        # Health monitoring
│   └── uninstall.sh           # Complete removal
├── config/                    # Configuration templates
│   ├── grafana-dashboards/    # Grafana dashboard JSONs
│   ├── grafana-datasources.yml
│   ├── loki-config.yml
│   └── promtail-config.yml
├── monitoring/                # Monitoring configurations
│   └── alerting-rules.yml
└── README.md                  # This file
```

## Scripts Reference

### setup.sh - Main Installation

Interactive setup script that handles complete deployment.

**Usage:**
```bash
./scripts/setup.sh [OPTIONS]

Options:
  --unattended              Run in non-interactive mode
  --storage-path PATH       Set external storage path
  --no-monitoring           Skip monitoring stack installation
  --proxy TYPE              Set reverse proxy (ngrok|cloudflare|tailscale|localtunnel|none)
  -h, --help                Show help message
```

**Examples:**
```bash
# Interactive setup (recommended)
./scripts/setup.sh

# Automated setup with defaults
./scripts/setup.sh --unattended --storage-path /Volumes/MyDrive

# Setup without monitoring
./scripts/setup.sh --no-monitoring

# Setup with specific reverse proxy
./scripts/setup.sh --proxy cloudflare
```

**What it does:**
1. ✅ Checks system requirements
2. ✅ Configures external storage
3. ✅ Generates secure API credentials
4. ✅ Creates LiveKit configuration
5. ✅ Sets up Docker Compose stack
6. ✅ Configures monitoring (Grafana/Prometheus/Loki)
7. ✅ Sets up reverse proxy
8. ✅ Starts all services
9. ✅ Verifies installation

### start.sh / stop.sh / restart.sh - Service Management

Control LiveKit services.

**Usage:**
```bash
./scripts/start.sh                    # Start all services
./scripts/stop.sh                     # Stop all services
./scripts/restart.sh                  # Restart all services
./scripts/restart.sh --graceful       # Wait for empty rooms before restart
```

### status.sh - Service Status

Check status of all services and display metrics.

**Usage:**
```bash
./scripts/status.sh
```

**Output includes:**
- Container status (up/down)
- Health checks
- Active rooms and participants
- Storage usage
- Container uptime

### logs.sh - Log Viewer

View logs from LiveKit and related services.

**Usage:**
```bash
./scripts/logs.sh [SERVICE] [OPTIONS]

Services:
  livekit       LiveKit server logs
  prometheus    Prometheus logs
  grafana       Grafana logs
  caddy         Caddy reverse proxy logs
  loki          Loki log aggregator logs
  all           All services (default)

Options:
  --follow, -f        Follow log output
  --tail N            Show last N lines (default: 100)
  --since TIME        Show logs since timestamp (e.g., "1h", "30m")
```

**Examples:**
```bash
# View last 100 lines from all services
./scripts/logs.sh

# Follow LiveKit logs in real-time
./scripts/logs.sh livekit --follow

# Show last 500 lines
./scripts/logs.sh livekit --tail 500

# Show logs from last hour
./scripts/logs.sh all --since 1h

# Search for errors
./scripts/logs.sh livekit | grep ERROR
```

### backup.sh - Backup Configuration

Backup LiveKit configuration and data.

**Usage:**
```bash
./scripts/backup.sh [OPTIONS]

Options:
  --list                List available backups
  --restore DATE        Restore backup from specific date
  --restore latest      Restore most recent backup
```

**What gets backed up:**
- LiveKit configuration files
- API keys (encrypted)
- Grafana dashboards
- Monitoring configurations

### update.sh - Update LiveKit

Update LiveKit to the latest version or specific version.

**Usage:**
```bash
./scripts/update.sh [OPTIONS]

Options:
  --check               Check for updates
  --version VERSION     Update to specific version
  --rollback            Rollback to previous version
  --history             Show version history
```

**Examples:**
```bash
# Check for updates
./scripts/update.sh --check

# Update to latest
./scripts/update.sh

# Update to specific version
./scripts/update.sh --version v1.6.0

# Rollback if issues
./scripts/update.sh --rollback
```

### health-check.sh - Health Monitoring

Run comprehensive health checks.

**Usage:**
```bash
./scripts/health-check.sh [OPTIONS]

Options:
  --test-alert          Test alert notifications
```

**Checks performed:**
- LiveKit API responsiveness
- WebSocket connectivity
- Prometheus metrics availability
- Grafana dashboard accessibility
- External storage mount status
- Disk space availability
- CPU and memory usage

### uninstall.sh - Complete Removal

Remove LiveKit and all components.

**Usage:**
```bash
./scripts/uninstall.sh [OPTIONS]

Options:
  --keep-data           Keep data files on external storage
  --force               Skip confirmation prompts
```

**What gets removed:**
- Docker containers and images
- Configuration files
- Service management files
- Optionally: data and logs

## Configuration Files

### LiveKit Configuration

Location: `$STORAGE_PATH/config/config.yaml`

Key settings:
```yaml
port: 7880
rtc:
  port_range_start: 50000
  port_range_end: 60000
room:
  auto_create: true
  max_participants: 50
```

### Docker Compose

Location: `$STORAGE_PATH/docker-compose.yml`

Services included:
- livekit - LiveKit server
- caddy - Reverse proxy
- prometheus - Metrics collection
- grafana - Monitoring dashboard
- loki - Log aggregation
- promtail - Log collection

### Monitoring

**Prometheus**: `$STORAGE_PATH/monitoring/prometheus/prometheus.yml`
**Grafana Datasources**: `config/grafana-datasources.yml`
**Loki**: `config/loki-config.yml`
**Promtail**: `config/promtail-config.yml`

## Storage Structure

When you run setup, the following structure is created on your external storage:

```
$STORAGE_PATH/
├── config/                    # Configuration files
│   ├── config.yaml           # LiveKit config
│   └── ngrok.yml             # ngrok config (if used)
├── data/                     # LiveKit data
├── logs/                     # Application logs
├── recordings/               # Recording files
├── backups/                  # Configuration backups
├── monitoring/               # Monitoring data
│   ├── prometheus/
│   │   ├── prometheus.yml
│   │   └── data/
│   ├── grafana/              # Grafana database
│   └── loki/                 # Loki database
├── caddy/                    # Caddy reverse proxy
│   ├── Caddyfile
│   ├── data/
│   └── config/
├── docker-compose.yml        # Docker Compose config
└── .env                      # Environment variables
```

## Reverse Proxy Options

### ngrok (Easiest)

**Setup:**
1. Create account at https://ngrok.com/signup
2. Get auth token from dashboard
3. Setup script will configure automatically

**Pros:**
- Quick setup
- HTTPS included
- Free tier available

**Cons:**
- Random URLs (free tier)
- Requires account

### Cloudflare Tunnel

**Setup:**
1. Install cloudflared: `brew install cloudflare/cloudflare/cloudflared`
2. Authenticate: `cloudflared tunnel login`
3. Create tunnel: `cloudflared tunnel create livekit`
4. Configure DNS

**Pros:**
- Free
- Custom domains
- DDoS protection

**Cons:**
- More complex setup
- Requires domain

### Tailscale (Private Network)

**Setup:**
1. Install: `brew install tailscale`
2. Start: `sudo tailscale up`
3. Access via Tailscale IP

**Pros:**
- Most secure
- No public exposure
- Free

**Cons:**
- Only for private network
- Not for public services

## Monitoring Dashboards

### Accessing Grafana

URL: http://localhost:3000
Default credentials: admin / [generated-password]

### Available Dashboards

1. **LiveKit Overview**
   - Server status
   - Active rooms/participants
   - CPU/memory usage
   - Network throughput

2. **System Resources**
   - Host metrics
   - Docker stats
   - Storage usage

3. **Logs Dashboard**
   - Log search/filtering
   - Error aggregation
   - Pattern detection

### Setting Up Alerts

1. Go to Grafana → Alerting → Alert Rules
2. Create new alert rule
3. Set conditions (e.g., CPU > 80%)
4. Configure notification channels

Example alert config in `monitoring/alerting-rules.yml`.

## Troubleshooting

### Docker Desktop Not Starting

```bash
# Check if Docker is running
pgrep -x "Docker"

# Restart Docker Desktop
killall Docker && open -a Docker

# Verify
docker ps
```

### External Storage Not Mounted

```bash
# Check if drive is mounted
ls /Volumes/

# Remount drive
diskutil mount YourDriveName

# Update storage path if needed
nano $STORAGE_PATH/docker-compose.yml
```

### Port Already in Use

```bash
# Find process using port
lsof -i :7880

# Kill process
kill -9 <PID>

# Or change port in config.yaml
```

### Services Not Starting

```bash
# Check logs for errors
./scripts/logs.sh livekit

# Check Docker resources
docker stats

# Increase Docker memory:
# Docker Desktop → Settings → Resources → Memory
```

## Cross-Platform Usage

These scripts work on both Mac and Linux (including Hetzner servers).

### Platform Detection

Scripts automatically detect the platform and adjust:

| Feature | Mac | Linux |
|---------|-----|-------|
| **Storage Path** | `/Volumes/LiveKitStorage` | `/opt/livekit-storage` |
| **Package Manager** | Homebrew | apt |
| **Service Manager** | launchd | systemd |

### Using on Linux/Hetzner

```bash
# Copy scripts to server
scp -r tools/mac-livekit user@server:/root/livekit-deploy

# SSH and run setup
ssh user@server
cd /root/livekit-deploy/scripts
./setup.sh
```

The scripts will auto-detect Linux and use appropriate commands.

## Best Practices

### Daily Operations

```bash
# Morning check
./scripts/status.sh

# View recent errors
./scripts/logs.sh livekit --since 1h | grep ERROR
```

### Weekly Maintenance

```bash
# Backup configuration
./scripts/backup.sh

# Check for updates
./scripts/update.sh --check

# Review storage usage
df -h $STORAGE_PATH
```

### Before Updating

```bash
# Backup current setup
./scripts/backup.sh

# Stop services during quiet period
./scripts/restart.sh --graceful

# Update
./scripts/update.sh
```

## Support

For issues:

1. Run diagnostic: `./scripts/health-check.sh`
2. Check logs: `./scripts/logs.sh all --tail 500`
3. Consult documentation: `docs/livekit-deployment/08-mac-deployment.md`
4. Community: https://livekit.io/community

## Advanced Configuration

### Custom Domain with SSL

Edit `$STORAGE_PATH/caddy/Caddyfile`:

```caddyfile
yourdomain.com {
    encode gzip
    reverse_proxy livekit:7880
}
```

### Multiple LiveKit Instances

Edit `$STORAGE_PATH/docker-compose.yml`:

```yaml
services:
  livekit:
    deploy:
      replicas: 3
```

### Recording to S3

Edit `$STORAGE_PATH/config/config.yaml`:

```yaml
recording:
  enabled: true
  storage:
    type: s3
    access_key: YOUR_KEY
    secret_key: YOUR_SECRET
    region: us-west-2
    bucket: recordings
```

## License

These scripts are provided as-is for use with LiveKit deployment.
