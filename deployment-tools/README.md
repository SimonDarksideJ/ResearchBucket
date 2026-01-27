# LiveKit Deployment Tools

Automated deployment scripts and configurations for LiveKit media server on Mac and Linux (Hetzner Cloud).

## 📁 Directory Structure

```
deployment-tools/
├── scripts/                    # Deployment and management scripts
│   ├── install-mac.sh         # Mac installation (main script)
│   ├── deploy.sh              # Universal deployment (auto-detects platform)
│   ├── health-check.sh        # Health monitoring script
│   ├── setup-cloudflare-tunnel.sh  # Public access setup
│   └── backup.sh              # Backup configuration and data
├── monitoring/                 # Monitoring stack configurations
│   ├── prometheus/            # Prometheus metrics
│   │   ├── prometheus.yml     # Scrape configuration
│   │   └── alerts.yml         # Alert rules
│   ├── grafana/               # Grafana dashboards
│   │   ├── datasources.yml    # Data source provisioning
│   │   └── dashboards.yml     # Dashboard provisioning
│   ├── loki/                  # Log aggregation
│   │   ├── loki-config.yml    # Loki configuration
│   │   └── promtail-config.yml # Log collection
│   └── dashboards/            # Grafana dashboard JSON files
└── README.md                   # This file
```

## 🚀 Quick Start

### Mac Installation

```bash
# Basic installation
./scripts/install-mac.sh

# With custom storage location
./scripts/install-mac.sh --storage /Volumes/MyDrive/livekit

# Skip dependency installation (if already installed)
./scripts/install-mac.sh --skip-deps

# Set Grafana password
./scripts/install-mac.sh --grafana-pass "MySecurePassword123"
```

### Universal Deployment (Auto-detect Platform)

```bash
# Automatically detects Mac or Linux
./scripts/deploy.sh

# Force specific platform
./scripts/deploy.sh --platform mac
./scripts/deploy.sh --platform linux

# Specify environment
./scripts/deploy.sh --env production

# Custom storage path
./scripts/deploy.sh --storage /custom/path
```

## 📋 Script Descriptions

### install-mac.sh

Main installation script for macOS:

**Features:**
- Detects macOS version and architecture (Intel/Apple Silicon)
- Installs Homebrew and dependencies (Docker, jq, yq, cloudflared)
- Configures external storage for data persistence
- Generates secure LiveKit API keys
- Creates all configuration files
- Deploys Docker Compose stack
- Sets up monitoring (Prometheus + Grafana + Loki)
- Creates helper scripts for management
- Provides comprehensive setup summary

**Options:**
- `--storage PATH` - Specify external storage path
- `--skip-deps` - Skip dependency installation
- `--grafana-pass PASSWORD` - Set Grafana admin password
- `--help` - Show help message

### deploy.sh

Universal deployment script with platform auto-detection:

**Features:**
- Automatically detects Mac vs Linux
- Delegates to platform-specific installers
- Supports configuration import/export
- Environment-aware deployment

**Options:**
- `--platform [mac|linux]` - Force specific platform
- `--env [dev|prod]` - Deployment environment
- `--storage PATH` - External storage path
- `--import PATH` - Import configuration from backup

### health-check.sh

Comprehensive health monitoring:

**Checks:**
- Docker status
- All service containers (LiveKit, Caddy, Prometheus, Grafana, Loki, Promtail)
- HTTP endpoint availability
- Resource usage (CPU, memory)
- Disk space
- Recent error logs

**Options:**
- `--verbose` - Show detailed output
- `--json` - Output in JSON format

**Usage:**
```bash
./scripts/health-check.sh              # Basic health check
./scripts/health-check.sh --verbose    # Detailed output
./scripts/health-check.sh --json       # JSON output for automation
```

### setup-cloudflare-tunnel.sh

Secure public access without router configuration:

**Features:**
- No port forwarding required
- Automatic HTTPS/SSL via Cloudflare
- DDoS protection included
- Supports custom domains
- Quick tunnels with random subdomains

**Options:**
- `--domain DOMAIN` - Use custom domain (requires Cloudflare DNS)

**Usage:**
```bash
# Quick tunnel (random subdomain)
./scripts/setup-cloudflare-tunnel.sh

# Custom domain (requires Cloudflare account)
./scripts/setup-cloudflare-tunnel.sh --domain livekit.yourdomain.com
```

### backup.sh

Automated backup solution:

**Backs up:**
- Configuration files (livekit.yaml, Caddyfile, etc.)
- Monitoring configurations
- Docker Compose files
- Grafana dashboards
- API credentials

**Options:**
- `--destination PATH` - Backup destination (default: ./backups)
- `--compress` - Create compressed archive
- `--encrypt` - Encrypt backup (password protected)
- `--remote HOST` - Copy to remote host via rsync

**Usage:**
```bash
./scripts/backup.sh                                # Basic backup
./scripts/backup.sh --compress                     # Compressed
./scripts/backup.sh --compress --encrypt           # Encrypted
./scripts/backup.sh --remote user@backup-server    # Remote backup
```

## 🔧 Configuration Files

### Monitoring Stack

**Prometheus (`monitoring/prometheus/prometheus.yml`):**
- Scrapes LiveKit metrics on port 6789
- Collects system metrics via node-exporter
- Monitors all container services
- 15-second scrape interval
- Alert rules enabled

**Grafana (`monitoring/grafana/`):**
- Pre-configured data sources (Prometheus, Loki)
- Dashboard auto-provisioning
- Admin credentials set during installation
- LiveKit-specific dashboards

**Loki (`monitoring/loki/loki-config.yml`):**
- 7-day log retention
- Filesystem storage
- Automatic log compaction
- 10MB ingestion rate limit

**Promtail (`monitoring/loki/promtail-config.yml`):**
- Collects LiveKit application logs
- Aggregates Docker container logs
- Log parsing and label extraction
- Automatic timestamp parsing

### Alert Rules

Located in `monitoring/prometheus/alerts.yml`:

**LiveKit Alerts:**
- Service down detection (1min threshold)
- High CPU usage (>80% for 5min)
- High memory usage (>6GB for 5min)
- Connection failure rate monitoring
- Room count saturation alerts

**System Alerts:**
- Low disk space (<15%)
- Critical disk space (<5%)
- High system CPU (>85%)
- High system memory (>90%)
- Container availability

**Monitoring Alerts:**
- Prometheus scrape failures
- High scrape duration
- Data source unavailability

## 🔍 Helper Scripts

After installation, these scripts are created in your LiveKit directory:

### start.sh
```bash
cd ~/livekit && ./start.sh
```
Starts all Docker containers.

### stop.sh
```bash
cd ~/livekit && ./stop.sh
```
Stops all Docker containers gracefully.

### status.sh
```bash
cd ~/livekit && ./status.sh
```
Shows service status and resource usage.

### logs.sh
```bash
cd ~/livekit && ./logs.sh          # All logs
cd ~/livekit && ./logs.sh livekit  # Specific service
```
Streams logs from services.

## 🌐 Service Access

After installation:

| Service | URL | Purpose |
|---------|-----|---------|
| LiveKit Server | http://localhost:7880 | WebRTC media server |
| Grafana Dashboard | http://localhost:3000 | Monitoring & visualization |
| Prometheus | http://localhost:9090 | Metrics database |
| Loki | http://localhost:3100 | Log aggregation |
| Caddy Health | http://localhost/health | Proxy health check |

## 📦 External Storage Structure

When using external storage (recommended for Mac):

```
/Volumes/YourDrive/livekit/
├── config/                      # Configuration files
│   ├── livekit.yaml            # LiveKit config
│   └── Caddyfile               # Proxy config
├── data/                        # Persistent data
│   ├── livekit/                # LiveKit data
│   ├── prometheus/             # Metrics storage
│   ├── grafana/                # Dashboard data
│   └── loki/                   # Log storage
├── logs/                        # Application logs
│   └── livekit/                # LiveKit logs
├── monitoring/                  # Monitoring configs
│   ├── prometheus/
│   ├── grafana/
│   └── loki/
├── backups/                     # Automated backups
├── docker-compose.yml           # Container orchestration
└── .credentials                 # API keys (secure)
```

## 🔐 Security

### Credentials Storage

All credentials are saved to `.credentials` file with restricted permissions (600):
```bash
cat ~/livekit/.credentials
```

**Never commit this file to version control!**

### Generated Credentials

During installation, the following are generated:
- LiveKit API Key (random)
- LiveKit API Secret (random)
- Grafana admin password (auto-generated or custom)

### Firewall Considerations

**Mac (Development):**
- All services listen on localhost by default
- Use Cloudflare Tunnel for public access (no firewall changes needed)

**Linux (Production):**
- Configure UFW or iptables
- Allow ports: 22 (SSH), 80/443 (HTTP/HTTPS), 7880-7881 (LiveKit), 50000-50200 (WebRTC UDP)
- Restrict monitoring ports (3000, 9090) to VPN/trusted IPs

## 🔄 Migration: Mac → Hetzner

When ready to deploy to production:

### 1. Export Configuration

```bash
cd ~/livekit
tar -czf livekit-config.tar.gz config/ monitoring/ docker-compose.yml .credentials
```

### 2. Copy to Hetzner Server

```bash
scp livekit-config.tar.gz root@YOUR_HETZNER_IP:/opt/
```

### 3. Deploy on Hetzner

```bash
ssh root@YOUR_HETZNER_IP
cd /opt
tar -xzf livekit-config.tar.gz
mv config monitoring docker-compose.yml .credentials /opt/livekit/

# Use deployment script
cd /path/to/deployment-tools
./scripts/deploy.sh --platform linux --storage /opt/livekit
```

## 🐛 Troubleshooting

### Services Won't Start

```bash
# Check Docker
docker ps

# View logs
cd ~/livekit
docker compose logs

# Check specific service
docker compose logs livekit
```

### External Drive Issues

```bash
# Check if mounted
ls -la /Volumes/

# Fix permissions
sudo chown -R $(whoami) /Volumes/YourDrive/livekit
```

### Port Conflicts

```bash
# Check what's using ports
lsof -i :7880
lsof -i :3000

# Kill conflicting process
kill -9 <PID>
```

### Health Check Failed

```bash
# Run comprehensive check
./scripts/health-check.sh --verbose

# Check individual services
curl http://localhost:7880/
curl http://localhost:3000/api/health
curl http://localhost:9090/-/healthy
```

## 📚 Additional Resources

### Documentation
- [07-mac-deployment.md](../07-mac-deployment.md) - Complete Mac deployment guide
- [06-hetzner-deployment.md](../06-hetzner-deployment.md) - Hetzner Cloud deployment guide
- [LiveKit Docs](https://docs.livekit.io/) - Official LiveKit documentation

### Support
- **LiveKit Community**: https://livekit.io/community
- **Docker Desktop**: https://docs.docker.com/desktop/mac/
- **Cloudflare Tunnel**: https://developers.cloudflare.com/cloudflare-one/connections/connect-apps/

## 🤝 Contributing

When adding new features to deployment scripts:

1. Maintain platform independence where possible
2. Add proper error handling and logging
3. Update this README with new scripts/features
4. Test on both Mac (Intel & Apple Silicon) and Linux
5. Document all configuration options

## 📝 License

Same as parent repository.

---

**Quick Links:**
- [Mac Deployment Guide](../07-mac-deployment.md)
- [Hetzner Deployment Guide](../06-hetzner-deployment.md)
- [Linux Hosting Research](../Linux-Hosting-Research-LiveKit.md)
