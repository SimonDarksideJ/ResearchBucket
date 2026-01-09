# LiveKit Linux Host Deployment Guide

## Overview

This guide covers deploying LiveKit Server as a native Linux service using systemd. This approach provides a lightweight, direct-to-metal deployment suitable for dedicated servers.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Installation Steps](#installation-steps)
3. [Configuration](#configuration)
4. [Service Management](#service-management)
5. [Monitoring & Logging](#monitoring--logging)
6. [Security Considerations](#security-considerations)
7. [Pros and Cons](#pros-and-cons)
8. [Administration Patterns](#administration-patterns)
9. [Gotchas and Troubleshooting](#gotchas-and-troubleshooting)

## Prerequisites

- Ubuntu 20.04+ or Debian 11+ (recommended) or RHEL/CentOS 8+
- Minimum 2 CPU cores, 4GB RAM (production: 4+ cores, 8GB+ RAM)
- Static IP address or DNS hostname
- Firewall configured to allow:
  - TCP 7880 (HTTP API & WebSocket)
  - TCP 7881 (HTTPS API & WebSocket) 
  - UDP 50000-60000 (WebRTC media - configurable range)
  - TCP 443 (if using reverse proxy)
- SSL/TLS certificate (Let's Encrypt recommended)
- Root or sudo access (for initial setup only)

## Installation Steps

### Step 1: Download and Install LiveKit

```bash
#!/bin/bash
# Install script for LiveKit on Linux

# Create dedicated user (no shell access for security)
sudo useradd -r -s /bin/false -d /opt/livekit -m livekit

# Download latest LiveKit server
LIVEKIT_VERSION="v1.5.3"  # Check https://github.com/livekit/livekit/releases for latest
cd /tmp
wget "https://github.com/livekit/livekit/releases/download/${LIVEKIT_VERSION}/livekit-server_${LIVEKIT_VERSION}_linux_amd64.tar.gz"

# Extract and install
sudo tar -xzf "livekit-server_${LIVEKIT_VERSION}_linux_amd64.tar.gz"
sudo mv livekit-server /opt/livekit/
sudo chown livekit:livekit /opt/livekit/livekit-server
sudo chmod +x /opt/livekit/livekit-server

# Create config directory
sudo mkdir -p /etc/livekit
sudo chown livekit:livekit /etc/livekit

# Create log directory
sudo mkdir -p /var/log/livekit
sudo chown livekit:livekit /var/log/livekit

# Create data directory (for recordings if needed)
sudo mkdir -p /var/lib/livekit
sudo chown livekit:livekit /var/lib/livekit
```

### Step 2: Configure LiveKit

Create the configuration file at `/etc/livekit/config.yaml`:

```yaml
# /etc/livekit/config.yaml
port: 7880
bind_addresses:
  - "0.0.0.0"

rtc:
  port_range_start: 50000
  port_range_end: 60000
  # Use ICE Lite for better NAT traversal
  use_ice_lite: true
  # Configure TURN/STUN
  udp_port: 7882
  tcp_port: 7881

# Redis for distributed deployments (optional for single server)
# redis:
#   address: localhost:6379
#   db: 0

# API keys for authentication
keys:
  # Generate with: openssl rand -base64 32
  APIKey1: <your-api-key>
  APISecret1: <your-api-secret>

# Logging configuration
logging:
  level: info
  json: true
  # Log to file and stdout
  file:
    filename: /var/log/livekit/livekit.log
    max_size: 100 # megabytes
    max_backups: 10
    max_age: 30 # days

# Room configuration
room:
  # Auto-create rooms
  auto_create: true
  # Room timeout (empty room cleanup)
  empty_timeout: 300 # 5 minutes
  # Maximum participants per room
  max_participants: 50

# Region and node configuration
region: us-west-2
node_ip: <your-server-public-ip>

# Turn server configuration (for NAT traversal)
turn:
  enabled: true
  domain: turn.yourdomain.com
  tls_port: 5349
  udp_port: 3478

# Webhook for events (optional)
# webhook:
#   api_key: <webhook-api-key>
#   urls:
#     - https://your-app.com/livekit/webhook

# Developer mode (disable in production)
dev_mode: false
```

Set proper permissions:

```bash
sudo chown livekit:livekit /etc/livekit/config.yaml
sudo chmod 600 /etc/livekit/config.yaml
```

### Step 3: Create systemd Service

Create `/etc/systemd/system/livekit.service`:

```ini
[Unit]
Description=LiveKit Media Server
Documentation=https://docs.livekit.io
After=network.target

[Service]
Type=simple
User=livekit
Group=livekit
ExecStart=/opt/livekit/livekit-server --config /etc/livekit/config.yaml
ExecReload=/bin/kill -HUP $MAINPID
Restart=on-failure
RestartSec=5s

# Security hardening
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=strict
ProtectHome=true
ReadWritePaths=/var/log/livekit /var/lib/livekit

# Resource limits
LimitNOFILE=65536
LimitNPROC=4096

# Logging
StandardOutput=journal
StandardError=journal
SyslogIdentifier=livekit

[Install]
WantedBy=multi-user.target
```

Enable and start the service:

```bash
# Reload systemd
sudo systemctl daemon-reload

# Enable service to start on boot
sudo systemctl enable livekit

# Start the service
sudo systemctl start livekit

# Check status
sudo systemctl status livekit
```

### Step 4: Configure Firewall

```bash
# For UFW (Ubuntu/Debian)
sudo ufw allow 7880/tcp comment "LiveKit HTTP"
sudo ufw allow 7881/tcp comment "LiveKit HTTPS"
sudo ufw allow 50000:60000/udp comment "LiveKit RTC"
sudo ufw allow 443/tcp comment "HTTPS"

# For firewalld (RHEL/CentOS)
sudo firewall-cmd --permanent --add-port=7880/tcp
sudo firewall-cmd --permanent --add-port=7881/tcp
sudo firewall-cmd --permanent --add-port=50000-60000/udp
sudo firewall-cmd --permanent --add-port=443/tcp
sudo firewall-cmd --reload
```

### Step 5: SSL/TLS Setup with Let's Encrypt

```bash
# Install certbot
sudo apt-get update
sudo apt-get install certbot

# Obtain certificate
sudo certbot certonly --standalone -d livekit.yourdomain.com

# Update config.yaml to use certificates
# Add to config.yaml:
# tls:
#   cert_file: /etc/letsencrypt/live/livekit.yourdomain.com/fullchain.pem
#   key_file: /etc/letsencrypt/live/livekit.yourdomain.com/privkey.pem

# Set up auto-renewal
sudo systemctl enable certbot.timer
sudo systemctl start certbot.timer

# Create renewal hook to restart LiveKit
sudo bash -c 'cat > /etc/letsencrypt/renewal-hooks/deploy/restart-livekit.sh' << 'EOF'
#!/bin/bash
systemctl restart livekit
EOF

sudo chmod +x /etc/letsencrypt/renewal-hooks/deploy/restart-livekit.sh
```

## Configuration

### Advanced Configuration Options

#### Multi-threaded Performance

```yaml
# config.yaml additions
rtc:
  # Enable multi-threading for better performance
  num_workers: 4  # Set to number of CPU cores
  
  # Packet buffer sizes
  packet_buffer_size: 500
  
  # Congestion control
  congestion_control: true
```

#### Network Optimization

```yaml
rtc:
  # Optimize for low latency
  use_ice_lite: true
  
  # TCP fallback for restricted networks
  tcp_fallback: true
  
  # STUN servers (for NAT traversal)
  stun_servers:
    - stun:stun.l.google.com:19302
    - stun:stun1.l.google.com:19302
```

#### Recording Configuration

```yaml
recording:
  enabled: true
  storage:
    type: filesystem
    path: /var/lib/livekit/recordings
  # Or use S3
  # storage:
  #   type: s3
  #   access_key: <aws-access-key>
  #   secret_key: <aws-secret-key>
  #   region: us-west-2
  #   bucket: livekit-recordings
```

## Service Management

### Basic Commands

```bash
# Start service
sudo systemctl start livekit

# Stop service
sudo systemctl stop livekit

# Restart service
sudo systemctl restart livekit

# Graceful reload (minimal disruption)
sudo systemctl reload livekit

# Check status
sudo systemctl status livekit

# View real-time logs
sudo journalctl -u livekit -f

# Check if service is active
systemctl is-active livekit
```

### Scheduled Maintenance

For zero-downtime deployments, schedule restarts during quiet periods:

```bash
# Create maintenance script: /opt/livekit/maintenance.sh
#!/bin/bash

# Check active room count before restart
ACTIVE_ROOMS=$(curl -s http://localhost:7880/rooms | jq '.rooms | length')

if [ "$ACTIVE_ROOMS" -eq 0 ]; then
    echo "No active rooms, performing restart..."
    systemctl restart livekit
    exit 0
else
    echo "Active rooms detected ($ACTIVE_ROOMS), skipping restart"
    exit 1
fi
```

Set up cron job:

```bash
# Run maintenance at 3 AM daily
sudo crontab -e
# Add:
0 3 * * * /opt/livekit/maintenance.sh >> /var/log/livekit/maintenance.log 2>&1
```

## Monitoring & Logging

### Log Management

#### View Logs

```bash
# Real-time logs
sudo journalctl -u livekit -f

# Last 100 lines
sudo journalctl -u livekit -n 100

# Logs from today
sudo journalctl -u livekit --since today

# Logs with specific priority
sudo journalctl -u livekit -p err

# Export logs to file
sudo journalctl -u livekit --since "2024-01-01" > livekit-logs.txt
```

#### Log Rotation

The configuration already includes log rotation in config.yaml. For journald logs:

```bash
# Configure journald log retention
sudo vim /etc/systemd/journald.conf

# Set:
SystemMaxUse=1G
SystemMaxFileSize=100M
SystemMaxFiles=10

# Restart journald
sudo systemctl restart systemd-journald
```

### Metrics and Monitoring

#### Prometheus Integration

LiveKit exposes Prometheus metrics on the HTTP port:

```bash
# Scrape metrics
curl http://localhost:7880/metrics
```

Create Prometheus config (`/etc/prometheus/prometheus.yml`):

```yaml
scrape_configs:
  - job_name: 'livekit'
    static_configs:
      - targets: ['localhost:7880']
    metrics_path: '/metrics'
    scrape_interval: 15s
```

#### Health Checks

```bash
# Simple health check script: /opt/livekit/healthcheck.sh
#!/bin/bash

HEALTH_URL="http://localhost:7880/health"
STATUS=$(curl -s -o /dev/null -w "%{http_code}" $HEALTH_URL)

if [ "$STATUS" -eq 200 ]; then
    echo "LiveKit is healthy"
    exit 0
else
    echo "LiveKit is unhealthy (HTTP $STATUS)"
    exit 1
fi
```

Set up monitoring with cron:

```bash
# Check health every minute
* * * * * /opt/livekit/healthcheck.sh || systemctl restart livekit
```

#### Resource Monitoring

```bash
# Monitor CPU and memory usage
# Install monitoring tools
sudo apt-get install sysstat htop

# Watch resources
watch -n 2 'ps aux | grep livekit-server | grep -v grep'

# Check network connections
ss -tunap | grep livekit
```

### Alerting Setup

Create alerting script for critical issues:

```bash
# /opt/livekit/alert.sh
#!/bin/bash

# Check if service is running
if ! systemctl is-active --quiet livekit; then
    echo "ALERT: LiveKit service is down!" | mail -s "LiveKit Alert" admin@yourdomain.com
    # Or use webhook
    curl -X POST https://your-webhook.com/alert \
         -H "Content-Type: application/json" \
         -d '{"service": "livekit", "status": "down", "timestamp": "'$(date -Iseconds)'"}'
fi

# Check high memory usage
MEM_USAGE=$(ps aux | grep livekit-server | awk '{print $4}' | head -1)
if (( $(echo "$MEM_USAGE > 80" | bc -l) )); then
    echo "ALERT: LiveKit memory usage high: $MEM_USAGE%" | mail -s "LiveKit Memory Alert" admin@yourdomain.com
fi
```

## Security Considerations

### User Isolation

The setup uses a dedicated `livekit` user with no shell access:

```bash
# Verify user has no shell
getent passwd livekit
# Should show: livekit:x:999:999::/opt/livekit:/bin/false
```

### File Permissions

```bash
# Configuration files should be readable only by livekit user
sudo chown livekit:livekit /etc/livekit/config.yaml
sudo chmod 600 /etc/livekit/config.yaml

# Binary should not be writable
sudo chmod 755 /opt/livekit/livekit-server
```

### API Key Security

```bash
# Generate secure API keys
openssl rand -base64 32

# Store keys in environment variables instead of config (optional)
sudo vim /etc/systemd/system/livekit.service

# Add under [Service]:
Environment="LIVEKIT_KEYS=APIKey1:secret1,APIKey2:secret2"
```

### Network Security

```bash
# Enable rate limiting with iptables
sudo iptables -A INPUT -p tcp --dport 7880 -m state --state NEW -m recent --set
sudo iptables -A INPUT -p tcp --dport 7880 -m state --state NEW -m recent --update --seconds 60 --hitcount 20 -j DROP

# Save rules
sudo netfilter-persistent save
```

### Regular Updates

```bash
# Create update script: /opt/livekit/update.sh
#!/bin/bash

CURRENT_VERSION=$(/opt/livekit/livekit-server --version 2>&1 | grep -oP 'v\d+\.\d+\.\d+')
LATEST_VERSION=$(curl -s https://api.github.com/repos/livekit/livekit/releases/latest | jq -r '.tag_name')

if [ "$CURRENT_VERSION" != "$LATEST_VERSION" ]; then
    echo "Update available: $CURRENT_VERSION -> $LATEST_VERSION"
    echo "Run manual update during maintenance window"
    # Add notification logic here
fi
```

## Pros and Cons

### Pros ✅

1. **Performance**: Direct hardware access, no containerization overhead
2. **Simplicity**: Fewer layers, easier to debug
3. **Resource Efficiency**: Lower memory and CPU overhead
4. **Startup Time**: Fast service startup (< 2 seconds)
5. **System Integration**: Native systemd integration for service management
6. **Debugging**: Direct access to processes with standard Linux tools
7. **Cost**: No additional infrastructure costs for orchestration
8. **Predictable**: Consistent performance without container runtime variability

### Cons ❌

1. **Scalability**: Manual horizontal scaling, no auto-scaling
2. **Portability**: Server-specific configuration, harder to replicate
3. **Updates**: Requires manual update process and testing
4. **Isolation**: Less isolation between services (if running multiple services)
5. **Rollback**: Manual rollback process, no quick version switching
6. **Multi-tenancy**: Difficult to run multiple isolated LiveKit versions
7. **Dependency Management**: OS-level dependencies can conflict
8. **Deployment Automation**: Requires custom scripts for deployment pipeline
9. **Recovery**: Longer recovery time compared to container orchestration
10. **Load Balancing**: Requires external load balancer setup

## Administration Patterns

### Daily Operations

#### Morning Check

```bash
#!/bin/bash
# /opt/livekit/daily-check.sh

echo "=== LiveKit Daily Health Check ==="
echo "Date: $(date)"
echo ""

echo "Service Status:"
systemctl status livekit --no-pager | head -5
echo ""

echo "Active Rooms:"
curl -s http://localhost:7880/rooms | jq '.rooms | length'
echo ""

echo "Resource Usage:"
ps aux | grep livekit-server | grep -v grep | awk '{print "CPU: "$3"% | MEM: "$4"%"}'
echo ""

echo "Disk Space:"
df -h /var/log/livekit /var/lib/livekit
echo ""

echo "Recent Errors (last hour):"
journalctl -u livekit --since "1 hour ago" -p err --no-pager | tail -10
```

#### Weekly Maintenance

```bash
#!/bin/bash
# /opt/livekit/weekly-maintenance.sh

# Rotate logs manually if needed
find /var/log/livekit -name "*.log" -mtime +30 -delete

# Clean old recordings
find /var/lib/livekit/recordings -mtime +90 -delete

# Check for updates
/opt/livekit/update.sh

# Generate weekly report
echo "Weekly Report - $(date)" > /var/log/livekit/weekly-report.txt
journalctl -u livekit --since "7 days ago" --no-pager | \
    grep -E "room_created|room_ended" | wc -l >> /var/log/livekit/weekly-report.txt
```

### Quiet Period Restart Pattern

```bash
#!/bin/bash
# /opt/livekit/smart-restart.sh

MAX_WAIT=3600  # 1 hour maximum wait
CHECK_INTERVAL=60  # Check every minute
ELAPSED=0

echo "Waiting for quiet period to restart LiveKit..."

while [ $ELAPSED -lt $MAX_WAIT ]; do
    ACTIVE_ROOMS=$(curl -s http://localhost:7880/rooms | jq '.rooms | length')
    
    if [ "$ACTIVE_ROOMS" -eq 0 ]; then
        echo "No active rooms found, restarting..."
        systemctl restart livekit
        echo "Restart completed at $(date)"
        exit 0
    else
        echo "$(date): $ACTIVE_ROOMS active rooms, waiting..."
        sleep $CHECK_INTERVAL
        ELAPSED=$((ELAPSED + CHECK_INTERVAL))
    fi
done

echo "Timeout reached, forcing restart (with active rooms)"
systemctl restart livekit
exit 1
```

### Backup and Recovery

```bash
#!/bin/bash
# /opt/livekit/backup.sh

BACKUP_DIR="/var/backups/livekit"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)

mkdir -p $BACKUP_DIR

# Backup configuration
tar -czf "$BACKUP_DIR/config-$TIMESTAMP.tar.gz" /etc/livekit/

# Backup recordings (if applicable)
if [ -d "/var/lib/livekit/recordings" ]; then
    rsync -av /var/lib/livekit/recordings/ "$BACKUP_DIR/recordings/"
fi

# Keep only last 7 days of backups
find $BACKUP_DIR -name "config-*.tar.gz" -mtime +7 -delete

echo "Backup completed: $TIMESTAMP"
```

## Gotchas and Troubleshooting

### Common Issues

#### 1. WebRTC Connection Failures

**Symptom**: Clients cannot establish media connections

**Diagnosis**:
```bash
# Check if UDP ports are accessible
sudo ss -ulnp | grep livekit

# Test STUN/TURN
sudo tcpdump -i any -n udp port 3478
```

**Solution**:
- Verify firewall rules allow UDP 50000-60000
- Check `node_ip` in config matches public IP
- Ensure TURN server is configured for NAT traversal
- Test with: `https://webrtc.github.io/samples/src/content/peerconnection/trickle-ice/`

#### 2. High CPU Usage

**Symptom**: CPU usage consistently > 80%

**Diagnosis**:
```bash
# Check thread usage
top -H -p $(pgrep livekit-server)

# Profile with perf
sudo perf top -p $(pgrep livekit-server)
```

**Solution**:
- Increase `num_workers` in config
- Reduce `max_participants` per room
- Consider horizontal scaling
- Check for codec issues (VP9 vs VP8 vs H264)

#### 3. Memory Leaks

**Symptom**: Memory usage grows over time

**Diagnosis**:
```bash
# Monitor memory over time
watch -n 5 'ps aux | grep livekit-server'

# Check for zombie rooms
curl http://localhost:7880/rooms
```

**Solution**:
- Set appropriate `empty_timeout` for rooms
- Enable room auto-cleanup
- Schedule periodic restarts
- Update to latest version (often fixes leaks)

#### 4. Service Won't Start

**Symptom**: `systemctl start livekit` fails

**Diagnosis**:
```bash
# Check detailed status
sudo systemctl status livekit -l

# Check logs
sudo journalctl -u livekit -n 50 --no-pager

# Test binary manually
sudo -u livekit /opt/livekit/livekit-server --config /etc/livekit/config.yaml
```

**Solution**:
- Check config.yaml syntax (use YAML validator)
- Verify file permissions
- Check port conflicts: `sudo ss -tlnp | grep 7880`
- Verify API keys are properly formatted

#### 5. SSL/TLS Certificate Issues

**Symptom**: HTTPS connections fail

**Diagnosis**:
```bash
# Test certificate
openssl s_client -connect livekit.yourdomain.com:7881

# Check certificate expiry
sudo certbot certificates
```

**Solution**:
- Ensure cert paths in config are correct
- Verify livekit user can read certificate files
- Check certificate renewal is working
- Test with: `curl -v https://livekit.yourdomain.com:7881/health`

#### 6. Port Exhaustion

**Symptom**: New connections fail after many participants

**Diagnosis**:
```bash
# Check ephemeral port usage
cat /proc/sys/net/ipv4/ip_local_port_range
ss -tan | wc -l
```

**Solution**:
```bash
# Increase ephemeral port range
sudo sysctl -w net.ipv4.ip_local_port_range="10000 65535"

# Make permanent
echo "net.ipv4.ip_local_port_range = 10000 65535" | sudo tee -a /etc/sysctl.conf
sudo sysctl -p

# Increase UDP buffer sizes
echo "net.core.rmem_max = 134217728" | sudo tee -a /etc/sysctl.conf
echo "net.core.wmem_max = 134217728" | sudo tee -a /etc/sysctl.conf
sudo sysctl -p
```

### Performance Tuning

```bash
# Kernel parameters for high-performance media server
cat > /etc/sysctl.d/99-livekit.conf << EOF
# Increase network buffers
net.core.rmem_default = 26214400
net.core.rmem_max = 134217728
net.core.wmem_default = 26214400
net.core.wmem_max = 134217728

# Increase connection tracking
net.netfilter.nf_conntrack_max = 1000000
net.nf_conntrack_max = 1000000

# Increase file descriptors
fs.file-max = 2097152

# Reduce TIME_WAIT sockets
net.ipv4.tcp_tw_reuse = 1
net.ipv4.tcp_fin_timeout = 15

# Ephemeral ports
net.ipv4.ip_local_port_range = 10000 65535
EOF

sudo sysctl -p /etc/sysctl.d/99-livekit.conf
```

### Emergency Procedures

#### Service Crash Recovery

```bash
#!/bin/bash
# /opt/livekit/emergency-restart.sh

# Kill all livekit processes
sudo pkill -9 livekit-server

# Clear any stale locks
rm -f /var/run/livekit.lock

# Clear temp files
rm -rf /tmp/livekit-*

# Restart service
sudo systemctl restart livekit

# Wait and verify
sleep 5
if systemctl is-active --quiet livekit; then
    echo "Service recovered successfully"
    exit 0
else
    echo "Service recovery failed, manual intervention required"
    exit 1
fi
```

## Migration and Upgrade Path

### Zero-Downtime Upgrade

Since this is a single-server setup, true zero-downtime requires:

1. Set up a second server with new version
2. Update load balancer to route to both servers
3. Wait for old server to drain connections
4. Shut down old server
5. Update DNS/load balancer to only new server

For single-server minimal-downtime upgrade:

```bash
#!/bin/bash
# /opt/livekit/upgrade.sh

NEW_VERSION="v1.5.4"  # Update as needed
BACKUP_DIR="/var/backups/livekit"

# Create backup
echo "Creating backup..."
mkdir -p $BACKUP_DIR
cp -r /opt/livekit $BACKUP_DIR/livekit-backup-$(date +%Y%m%d)
cp /etc/livekit/config.yaml $BACKUP_DIR/

# Download new version
cd /tmp
wget "https://github.com/livekit/livekit/releases/download/${NEW_VERSION}/livekit-server_${NEW_VERSION}_linux_amd64.tar.gz"
tar -xzf "livekit-server_${NEW_VERSION}_linux_amd64.tar.gz"

# Stop service
sudo systemctl stop livekit

# Replace binary
sudo mv livekit-server /opt/livekit/
sudo chown livekit:livekit /opt/livekit/livekit-server
sudo chmod +x /opt/livekit/livekit-server

# Start service
sudo systemctl start livekit

# Verify
sleep 5
if systemctl is-active --quiet livekit; then
    echo "Upgrade successful to $NEW_VERSION"
else
    echo "Upgrade failed, rolling back..."
    sudo cp $BACKUP_DIR/livekit-backup-$(date +%Y%m%d)/livekit-server /opt/livekit/
    sudo systemctl start livekit
fi
```

## Conclusion

Linux host deployment is ideal for:
- Small to medium deployments (< 1000 concurrent users)
- Budget-conscious deployments
- Teams with strong Linux administration skills
- Scenarios where maximum performance is critical
- Development and testing environments

Not recommended for:
- Large-scale deployments requiring auto-scaling
- Teams needing rapid deployment and rollback
- Multi-region deployments
- Organizations without dedicated Linux administrators
