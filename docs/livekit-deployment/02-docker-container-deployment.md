# LiveKit Docker Container Deployment Guide

## Overview

This guide covers deploying LiveKit Server using Docker containers. This approach provides portability, easy versioning, and simplified deployment automation.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Installation Steps](#installation-steps)
3. [Configuration](#configuration)
4. [Container Management](#container-management)
5. [Monitoring & Logging](#monitoring--logging)
6. [Security Considerations](#security-considerations)
7. [Pros and Cons](#pros-and-cons)
8. [Administration Patterns](#administration-patterns)
9. [Gotchas and Troubleshooting](#gotchas-and-troubleshooting)

## Prerequisites

- Docker Engine 20.10+ or Docker Desktop
- Docker Compose 2.0+ (optional but recommended)
- Host with:
  - Minimum 2 CPU cores, 4GB RAM (production: 4+ cores, 8GB+ RAM)
  - Static IP address or DNS hostname
  - Docker storage: 20GB minimum
- Firewall configured to allow:
  - TCP 7880 (HTTP API & WebSocket)
  - TCP 7881 (HTTPS API & WebSocket)
  - UDP 50000-60000 (WebRTC media)
- SSL/TLS certificate (Let's Encrypt recommended)
- Basic understanding of Docker concepts

## Installation Steps

### Method 1: Using Official Docker Image (Recommended)

#### Step 1: Pull Official Image

```bash
# Pull latest LiveKit image
docker pull livekit/livekit-server:latest

# Or specific version
docker pull livekit/livekit-server:v1.5.3

# Verify image
docker images | grep livekit
```

#### Step 2: Create Directory Structure

```bash
# Create directory structure
mkdir -p ~/livekit-deploy/{config,logs,recordings}
cd ~/livekit-deploy

# Set appropriate permissions
sudo chown -R 1000:1000 logs recordings
```

#### Step 3: Create Configuration File

Create `~/livekit-deploy/config/config.yaml`:

```yaml
port: 7880
bind_addresses:
  - "0.0.0.0"

rtc:
  port_range_start: 50000
  port_range_end: 60000
  use_ice_lite: true
  tcp_port: 7881
  udp_port: 7882

keys:
  APIKey1: <your-api-key>
  APISecret1: <your-api-secret>

logging:
  level: info
  json: true

room:
  auto_create: true
  empty_timeout: 300
  max_participants: 50

# Use environment variable for node IP
# Will be set via Docker environment
# node_ip: will be set via LIVEKIT_NODE_IP env var
```

#### Step 4: Run Container

```bash
# Run LiveKit container
docker run -d \
  --name livekit-server \
  --restart unless-stopped \
  -p 7880:7880 \
  -p 7881:7881 \
  -p 7882:7882/udp \
  -p 50000-60000:50000-60000/udp \
  -v ~/livekit-deploy/config:/config \
  -v ~/livekit-deploy/logs:/logs \
  -v ~/livekit-deploy/recordings:/recordings \
  -e LIVEKIT_NODE_IP=<your-public-ip> \
  livekit/livekit-server:latest \
  --config /config/config.yaml

# Verify container is running
docker ps | grep livekit-server

# Check logs
docker logs livekit-server
```

### Method 2: Using Docker Compose (Production Recommended)

Create `docker-compose.yml`:

```yaml
version: '3.9'

services:
  livekit:
    image: livekit/livekit-server:v1.5.3
    container_name: livekit-server
    restart: unless-stopped
    
    # Network configuration
    ports:
      - "7880:7880/tcp"     # HTTP API
      - "7881:7881/tcp"     # HTTPS API
      - "7882:7882/udp"     # UDP port
      - "50000-60000:50000-60000/udp"  # RTC ports
    
    # Volume mounts
    volumes:
      - ./config:/config:ro
      - ./logs:/logs
      - ./recordings:/recordings
      - /etc/letsencrypt:/etc/letsencrypt:ro  # SSL certificates
    
    # Environment variables
    environment:
      - LIVEKIT_NODE_IP=${PUBLIC_IP}
      - LIVEKIT_KEYS=${API_KEYS}
    
    # Command
    command: --config /config/config.yaml
    
    # Resource limits
    deploy:
      resources:
        limits:
          cpus: '4'
          memory: 8G
        reservations:
          cpus: '2'
          memory: 4G
    
    # Health check
    healthcheck:
      test: ["CMD", "wget", "--quiet", "--tries=1", "--spider", "http://localhost:7880/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
    
    # Logging
    logging:
      driver: "json-file"
      options:
        max-size: "100m"
        max-file: "10"
        labels: "service=livekit"
    
    # Security
    security_opt:
      - no-new-privileges:true
    
    # User (non-root)
    user: "1000:1000"
  
  # Redis for distributed deployments (optional)
  redis:
    image: redis:7-alpine
    container_name: livekit-redis
    restart: unless-stopped
    ports:
      - "127.0.0.1:6379:6379"
    volumes:
      - redis-data:/data
    command: redis-server --appendonly yes
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 30s
      timeout: 3s
      retries: 3
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"

volumes:
  redis-data:
    driver: local

networks:
  default:
    name: livekit-network
```

Create `.env` file:

```bash
# .env file
PUBLIC_IP=<your-public-ip>
API_KEYS=APIKey1:secret1,APIKey2:secret2
```

Start services:

```bash
# Start all services
docker-compose up -d

# Check status
docker-compose ps

# View logs
docker-compose logs -f livekit

# Stop services
docker-compose down
```

### Method 3: Custom Dockerfile (Advanced)

Create a custom Dockerfile for additional tooling:

```dockerfile
# Dockerfile
FROM livekit/livekit-server:v1.5.3

# Install additional tools
USER root
RUN apk add --no-cache \
    curl \
    jq \
    bash \
    ca-certificates

# Copy custom scripts
COPY scripts/ /usr/local/bin/
RUN chmod +x /usr/local/bin/*.sh

# Health check script
COPY healthcheck.sh /usr/local/bin/
RUN chmod +x /usr/local/bin/healthcheck.sh

# Switch back to non-root user
USER 1000

# Custom entrypoint
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh"]
CMD ["--config", "/config/config.yaml"]
```

Create `entrypoint.sh`:

```bash
#!/bin/bash
set -e

# Wait for dependencies (e.g., Redis)
if [ -n "$REDIS_HOST" ]; then
    echo "Waiting for Redis..."
    while ! nc -z $REDIS_HOST 6379; do
        sleep 1
    done
    echo "Redis is ready"
fi

# Execute LiveKit server
exec /livekit-server "$@"
```

Build and run:

```bash
# Build custom image
docker build -t livekit-custom:latest .

# Run custom image
docker run -d \
  --name livekit-custom \
  -p 7880:7880 \
  -p 50000-60000:50000-60000/udp \
  -v $(pwd)/config:/config \
  livekit-custom:latest
```

## Configuration

### Production Configuration with Docker

Enhanced `config.yaml` for production:

```yaml
port: 7880
bind_addresses:
  - "0.0.0.0"

rtc:
  port_range_start: 50000
  port_range_end: 60000
  use_ice_lite: true
  tcp_fallback: true
  
  # UDP and TCP ports
  udp_port: 7882
  tcp_port: 7881
  
  # STUN servers for NAT traversal
  stun_servers:
    - stun:stun.l.google.com:19302
    - stun:stun1.l.google.com:19302

# Redis for multi-instance deployments
redis:
  address: redis:6379
  db: 0
  username: ""
  password: ""

keys:
  # Use environment variables for security
  devkey: devkey

logging:
  level: info
  json: true
  # Container logs go to stdout
  
room:
  auto_create: true
  empty_timeout: 300
  max_participants: 100
  departure_timeout: 20

# Webhook configuration
webhook:
  api_key: ${WEBHOOK_API_KEY}
  urls:
    - ${WEBHOOK_URL}

# Node configuration
region: ${AWS_REGION:-us-east-1}
# node_ip set via environment variable

# TLS configuration
# tls:
#   cert_file: /etc/letsencrypt/live/yourdomain.com/fullchain.pem
#   key_file: /etc/letsencrypt/live/yourdomain.com/privkey.pem
```

### Environment-Based Configuration

Create multiple docker-compose files for different environments:

**docker-compose.prod.yml**:
```yaml
version: '3.9'

services:
  livekit:
    image: livekit/livekit-server:v1.5.3
    environment:
      - LIVEKIT_LOG_LEVEL=info
      - LIVEKIT_NODE_IP=${PUBLIC_IP}
    deploy:
      resources:
        limits:
          cpus: '8'
          memory: 16G
```

**docker-compose.dev.yml**:
```yaml
version: '3.9'

services:
  livekit:
    image: livekit/livekit-server:latest
    environment:
      - LIVEKIT_LOG_LEVEL=debug
      - LIVEKIT_NODE_IP=localhost
    ports:
      - "7880:7880"
```

Run with:
```bash
# Production
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d

# Development
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
```

## Container Management

### Basic Docker Commands

```bash
# Start container
docker start livekit-server

# Stop container
docker stop livekit-server

# Restart container
docker restart livekit-server

# Remove container
docker rm livekit-server

# Check container status
docker ps -a | grep livekit

# View resource usage
docker stats livekit-server

# Execute command in container
docker exec -it livekit-server sh

# Inspect container
docker inspect livekit-server
```

### Docker Compose Commands

```bash
# Start services
docker-compose up -d

# Stop services
docker-compose stop

# Restart specific service
docker-compose restart livekit

# View logs
docker-compose logs -f livekit

# Scale service (if configured)
docker-compose up -d --scale livekit=3

# Remove all services
docker-compose down

# Remove with volumes
docker-compose down -v

# Pull latest images
docker-compose pull

# Rebuild images
docker-compose build --no-cache
```

### Container Updates

#### Zero-Downtime Update Strategy

```bash
#!/bin/bash
# update-livekit.sh

# Pull new image
docker-compose pull livekit

# Start new container with different name
docker-compose up -d --no-deps --scale livekit=2 livekit

# Wait for health check
sleep 30

# Stop old container
docker-compose up -d --no-deps --scale livekit=1 livekit

echo "Update completed"
```

#### Rollback Strategy

```bash
#!/bin/bash
# rollback-livekit.sh

BACKUP_IMAGE="livekit/livekit-server:v1.5.2"

# Stop current container
docker-compose stop livekit

# Change image in docker-compose.yml or use override
cat > docker-compose.override.yml << EOF
version: '3.9'
services:
  livekit:
    image: ${BACKUP_IMAGE}
EOF

# Start with backup image
docker-compose up -d livekit

echo "Rolled back to ${BACKUP_IMAGE}"
```

## Monitoring & Logging

### Container Logs

```bash
# View logs in real-time
docker logs -f livekit-server

# View last 100 lines
docker logs --tail 100 livekit-server

# View logs since timestamp
docker logs --since 2024-01-01T00:00:00 livekit-server

# View logs with timestamps
docker logs -t livekit-server

# Save logs to file
docker logs livekit-server > livekit-logs.txt 2>&1
```

### Log Aggregation with ELK Stack

Create `docker-compose.monitoring.yml`:

```yaml
version: '3.9'

services:
  elasticsearch:
    image: elasticsearch:8.11.0
    container_name: elasticsearch
    environment:
      - discovery.type=single-node
      - "ES_JAVA_OPTS=-Xms512m -Xmx512m"
      - xpack.security.enabled=false
    volumes:
      - es-data:/usr/share/elasticsearch/data
    ports:
      - "9200:9200"
  
  logstash:
    image: logstash:8.11.0
    container_name: logstash
    volumes:
      - ./logstash/config/logstash.yml:/usr/share/logstash/config/logstash.yml
      - ./logstash/pipeline:/usr/share/logstash/pipeline
    ports:
      - "5044:5044"
    depends_on:
      - elasticsearch
  
  kibana:
    image: kibana:8.11.0
    container_name: kibana
    environment:
      - ELASTICSEARCH_HOSTS=http://elasticsearch:9200
    ports:
      - "5601:5601"
    depends_on:
      - elasticsearch

  filebeat:
    image: elastic/filebeat:8.11.0
    container_name: filebeat
    user: root
    volumes:
      - ./filebeat/filebeat.yml:/usr/share/filebeat/filebeat.yml:ro
      - /var/lib/docker/containers:/var/lib/docker/containers:ro
      - /var/run/docker.sock:/var/run/docker.sock:ro
    command: filebeat -e -strict.perms=false
    depends_on:
      - logstash

volumes:
  es-data:
```

### Prometheus Monitoring

Create `prometheus.yml`:

```yaml
global:
  scrape_interval: 15s
  evaluation_interval: 15s

scrape_configs:
  - job_name: 'livekit'
    static_configs:
      - targets: ['livekit:7880']
    metrics_path: '/metrics'
    
  - job_name: 'cadvisor'
    static_configs:
      - targets: ['cadvisor:8080']
```

Add to `docker-compose.yml`:

```yaml
  prometheus:
    image: prom/prometheus:latest
    container_name: prometheus
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
      - prometheus-data:/prometheus
    ports:
      - "9090:9090"
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'
      - '--storage.tsdb.path=/prometheus'
  
  grafana:
    image: grafana/grafana:latest
    container_name: grafana
    volumes:
      - grafana-data:/var/lib/grafana
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=admin
    depends_on:
      - prometheus
  
  cadvisor:
    image: gcr.io/cadvisor/cadvisor:latest
    container_name: cadvisor
    volumes:
      - /:/rootfs:ro
      - /var/run:/var/run:ro
      - /sys:/sys:ro
      - /var/lib/docker/:/var/lib/docker:ro
    ports:
      - "8080:8080"
    privileged: true

volumes:
  prometheus-data:
  grafana-data:
```

### Health Monitoring

Create health check script `healthcheck.sh`:

```bash
#!/bin/bash

# Check if LiveKit is responding
HEALTH_ENDPOINT="http://localhost:7880/health"
HTTP_CODE=$(wget --quiet --tries=1 --spider --server-response "$HEALTH_ENDPOINT" 2>&1 | grep "HTTP/" | awk '{print $2}')

if [ "$HTTP_CODE" = "200" ]; then
    exit 0
else
    exit 1
fi
```

Add to Dockerfile or use in docker-compose healthcheck.

### Container Resource Monitoring

```bash
#!/bin/bash
# monitor-resources.sh

while true; do
    echo "=== $(date) ==="
    
    # CPU and Memory
    docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.NetIO}}" | grep livekit
    
    # Active connections
    echo "Active rooms:"
    curl -s http://localhost:7880/rooms | jq '.rooms | length'
    
    echo ""
    sleep 60
done
```

## Security Considerations

### Container Security Best Practices

#### 1. Run as Non-Root User

Already configured in docker-compose:
```yaml
user: "1000:1000"
```

#### 2. Read-Only Filesystem

```yaml
services:
  livekit:
    read_only: true
    tmpfs:
      - /tmp
      - /logs
```

#### 3. Security Options

```yaml
services:
  livekit:
    security_opt:
      - no-new-privileges:true
      - seccomp:unconfined  # May be needed for WebRTC
    cap_drop:
      - ALL
    cap_add:
      - NET_BIND_SERVICE
```

#### 4. Network Isolation

```yaml
networks:
  frontend:
    driver: bridge
  backend:
    driver: bridge
    internal: true

services:
  livekit:
    networks:
      - frontend
      - backend
  redis:
    networks:
      - backend
```

#### 5. Secrets Management

```yaml
services:
  livekit:
    secrets:
      - livekit_api_key
      - livekit_api_secret
    environment:
      - LIVEKIT_KEYS_FILE=/run/secrets/livekit_api_key

secrets:
  livekit_api_key:
    file: ./secrets/api_key.txt
  livekit_api_secret:
    file: ./secrets/api_secret.txt
```

#### 6. Image Scanning

```bash
# Scan image for vulnerabilities
docker scan livekit/livekit-server:latest

# Or use Trivy
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
    aquasec/trivy image livekit/livekit-server:latest
```

### Network Security

```bash
# Use Docker networks for isolation
docker network create --driver bridge livekit-net

# Run container in isolated network
docker run -d \
  --network livekit-net \
  --name livekit-server \
  livekit/livekit-server:latest
```

## Pros and Cons

### Pros ✅

1. **Portability**: Consistent environment across dev/staging/prod
2. **Easy Updates**: Simple pull and restart for new versions
3. **Quick Rollback**: Instant rollback to previous image version
4. **Isolation**: Process and filesystem isolation from host
5. **Version Management**: Easy to run multiple versions simultaneously
6. **Resource Control**: Built-in CPU and memory limits
7. **Reproducibility**: Same image works everywhere
8. **CI/CD Integration**: Easy to integrate into pipelines
9. **Development**: Matches production environment locally
10. **Dependencies**: All dependencies bundled in image

### Cons ❌

1. **Network Overhead**: Small networking performance penalty
2. **Storage**: Requires Docker storage management
3. **Complexity**: Additional layer to understand and manage
4. **UDP Performance**: Slight overhead for UDP-based RTC traffic
5. **Debugging**: Additional step to exec into container
6. **Resource Overhead**: Docker daemon resource usage
7. **Port Management**: More complex port mapping
8. **Host Integration**: Less integrated with host services
9. **Learning Curve**: Requires Docker knowledge
10. **Security**: Additional attack surface if not configured properly

## Administration Patterns

### Daily Operations

#### Health Check Script

```bash
#!/bin/bash
# daily-health-check.sh

echo "=== LiveKit Container Health Check ==="
echo "Date: $(date)"
echo ""

# Container status
echo "Container Status:"
docker ps --filter name=livekit-server --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
echo ""

# Resource usage
echo "Resource Usage:"
docker stats --no-stream livekit-server --format "CPU: {{.CPUPerc}} | Memory: {{.MemUsage}}"
echo ""

# Health endpoint
echo "Health Endpoint:"
curl -s http://localhost:7880/health | jq '.'
echo ""

# Active rooms
echo "Active Rooms:"
curl -s http://localhost:7880/rooms | jq '.rooms | length'
echo ""

# Recent errors in logs
echo "Recent Errors (last 1 hour):"
docker logs --since 1h livekit-server 2>&1 | grep -i error | tail -10
```

### Automated Restart During Quiet Periods

```bash
#!/bin/bash
# smart-restart.sh

MAX_WAIT=3600
CHECK_INTERVAL=60
ELAPSED=0

echo "Waiting for quiet period..."

while [ $ELAPSED -lt $MAX_WAIT ]; do
    ACTIVE_ROOMS=$(docker exec livekit-server curl -s http://localhost:7880/rooms | jq '.rooms | length')
    
    if [ "$ACTIVE_ROOMS" -eq 0 ]; then
        echo "No active rooms, restarting container..."
        docker-compose restart livekit
        echo "Restart completed"
        exit 0
    else
        echo "$(date): $ACTIVE_ROOMS active rooms, waiting..."
        sleep $CHECK_INTERVAL
        ELAPSED=$((ELAPSED + CHECK_INTERVAL))
    fi
done

echo "Timeout reached, restart skipped"
exit 1
```

### Backup and Disaster Recovery

```bash
#!/bin/bash
# backup-livekit-container.sh

BACKUP_DIR="/var/backups/livekit"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)

mkdir -p $BACKUP_DIR

# Backup configuration
tar -czf "$BACKUP_DIR/config-$TIMESTAMP.tar.gz" config/

# Backup volumes
docker run --rm \
  --volumes-from livekit-server \
  -v $BACKUP_DIR:/backup \
  alpine tar -czf /backup/volumes-$TIMESTAMP.tar.gz /recordings /logs

# Export container configuration
docker inspect livekit-server > "$BACKUP_DIR/container-config-$TIMESTAMP.json"

# Keep only last 7 days
find $BACKUP_DIR -name "*.tar.gz" -mtime +7 -delete

echo "Backup completed: $TIMESTAMP"
```

### Container Auto-Healing

```bash
#!/bin/bash
# auto-heal.sh

# Run as cron job every 5 minutes

CONTAINER_NAME="livekit-server"

# Check if container is running
if ! docker ps | grep -q $CONTAINER_NAME; then
    echo "$(date): Container not running, attempting restart"
    docker start $CONTAINER_NAME
    
    # Wait and check again
    sleep 10
    if docker ps | grep -q $CONTAINER_NAME; then
        echo "$(date): Container restarted successfully"
    else
        echo "$(date): Failed to restart container, alerting..."
        # Send alert
        curl -X POST https://alerts.example.com/webhook \
             -d '{"service":"livekit","status":"down","timestamp":"'$(date -Iseconds)'"}'
    fi
fi

# Check health endpoint
HEALTH_STATUS=$(docker exec $CONTAINER_NAME wget -q -O- http://localhost:7880/health 2>/dev/null)
if [ -z "$HEALTH_STATUS" ]; then
    echo "$(date): Health check failed, restarting container"
    docker restart $CONTAINER_NAME
fi
```

### Log Rotation

```bash
#!/bin/bash
# rotate-logs.sh

# For Docker's json-file driver, configure in docker-compose:
# logging:
#   driver: "json-file"
#   options:
#     max-size: "100m"
#     max-file: "10"

# Manual log cleanup
docker logs livekit-server > /tmp/livekit-backup.log 2>&1
docker exec livekit-server truncate -s 0 /logs/livekit.log
```

## Gotchas and Troubleshooting

### Common Issues

#### 1. UDP Port Mapping Issues

**Symptom**: WebRTC connections fail

**Solution**:
```bash
# Ensure UDP port range is properly mapped
docker run -p 50000-60000:50000-60000/udp livekit/livekit-server

# Check if ports are actually listening
docker exec livekit-server netstat -uln | grep 500
```

#### 2. Container Can't Resolve DNS

**Symptom**: External webhooks fail

**Solution**:
```yaml
services:
  livekit:
    dns:
      - 8.8.8.8
      - 8.8.4.4
```

#### 3. Permission Issues with Volumes

**Symptom**: Cannot write to mounted volumes

**Solution**:
```bash
# Fix permissions on host
sudo chown -R 1000:1000 ~/livekit-deploy/logs
sudo chmod -R 755 ~/livekit-deploy/logs

# Or run container as root (not recommended)
user: "0:0"
```

#### 4. Container Restart Loop

**Symptom**: Container keeps restarting

**Diagnosis**:
```bash
# Check exit code
docker inspect livekit-server | jq '.[0].State'

# View logs
docker logs livekit-server

# Check events
docker events --filter container=livekit-server
```

#### 5. High Memory Usage

**Symptom**: Container uses excessive memory

**Solution**:
```yaml
# Set memory limits
services:
  livekit:
    deploy:
      resources:
        limits:
          memory: 4G
        reservations:
          memory: 2G
    # Enable OOM kill
    oom_kill_disable: false
```

#### 6. Network Performance Issues

**Symptom**: High latency or packet loss

**Solution**:
```bash
# Use host network mode (less isolation but better performance)
docker run --network host livekit/livekit-server

# Or optimize bridge network
docker network create --driver bridge \
  --opt com.docker.network.bridge.name=livekit0 \
  --opt com.docker.network.driver.mtu=9000 \
  livekit-net
```

### Performance Tuning

#### Optimize Docker Daemon

Edit `/etc/docker/daemon.json`:

```json
{
  "log-driver": "json-file",
  "log-opts": {
    "max-size": "100m",
    "max-file": "3"
  },
  "storage-driver": "overlay2",
  "default-ulimits": {
    "nofile": {
      "Name": "nofile",
      "Hard": 64000,
      "Soft": 64000
    }
  }
}
```

Restart Docker:
```bash
sudo systemctl restart docker
```

#### Container Ulimits

```yaml
services:
  livekit:
    ulimits:
      nofile:
        soft: 65536
        hard: 65536
      nproc:
        soft: 4096
        hard: 4096
```

### Debugging

```bash
# Enter container shell
docker exec -it livekit-server sh

# Check network connectivity
docker exec livekit-server ping -c 3 8.8.8.8

# View container resource usage
docker stats livekit-server

# Export container filesystem
docker export livekit-server > livekit-container.tar

# Check container networking
docker network inspect bridge

# View container processes
docker top livekit-server
```

## Multi-Container Architecture

### Load Balanced Setup

```yaml
version: '3.9'

services:
  haproxy:
    image: haproxy:2.8
    container_name: haproxy
    ports:
      - "80:80"
      - "443:443"
      - "7880:7880"
    volumes:
      - ./haproxy.cfg:/usr/local/etc/haproxy/haproxy.cfg:ro
    depends_on:
      - livekit-1
      - livekit-2
  
  livekit-1:
    image: livekit/livekit-server:latest
    container_name: livekit-1
    volumes:
      - ./config:/config
    environment:
      - LIVEKIT_NODE_IP=${PUBLIC_IP}
  
  livekit-2:
    image: livekit/livekit-server:latest
    container_name: livekit-2
    volumes:
      - ./config:/config
    environment:
      - LIVEKIT_NODE_IP=${PUBLIC_IP}
  
  redis:
    image: redis:7-alpine
    container_name: redis
    volumes:
      - redis-data:/data

volumes:
  redis-data:
```

## Conclusion

Docker deployment is ideal for:
- Development and testing environments
- Teams familiar with containerization
- CI/CD pipelines requiring consistent environments
- Deployments requiring easy rollback
- Multi-version testing scenarios
- Small to medium scale deployments

Not recommended for:
- Maximum performance requirements (use Linux host)
- Large-scale production (use Kubernetes)
- Teams without Docker expertise
- Environments with strict container policies
