# Hetzner Cloud LiveKit Deployment Guide

## Overview

This guide covers deploying LiveKit media server on Hetzner Cloud infrastructure with monitoring and management capabilities.

## Prerequisites

- Hetzner Cloud account
- Domain name configured
- SSH key pair generated
- Basic Linux knowledge

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

### 1. Create Hetzner Server
```bash
# Using Hetzner Cloud CLI (hcloud)
hcloud server create \
  --name livekit-prod \
  --type cx31 \
  --image ubuntu-22.04 \
  --location fsn1 \
  --ssh-key YOUR_KEY_NAME
```

### 2. Initial Server Setup
```bash
# Connect to server
ssh root@YOUR_SERVER_IP

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
```

### 3. Install Docker
```bash
curl -fsSL https://get.docker.com -o get-docker.sh
sh get-docker.sh

# Install Docker Compose
apt install -y docker-compose-plugin
```

### 4. Deploy Using Deployment Scripts
```bash
# Clone deployment repository
git clone https://github.com/YOUR_USERNAME/ResearchBucket.git
cd ResearchBucket/deployment-tools

# Run deployment script
./scripts/deploy.sh --platform linux --env production
```

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
