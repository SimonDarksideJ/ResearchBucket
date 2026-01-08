# Containerization Guide

## Overview

Containerization provides isolation, reproducibility, and portability for local LLM deployments. This guide covers Docker and Kubernetes strategies for packaging and deploying LLM agents.

## Benefits of Containerization

- **Isolation**: Separate environments per project
- **Reproducibility**: Same environment everywhere
- **Portability**: Easy to share and deploy
- **Resource Control**: CPU/GPU/memory limits
- **Version Control**: Easy rollback and upgrades

## Docker Basics

### Complete Stack Docker Compose

```yaml
version: '3.8'

services:
  # Ollama with GPU support
  ollama:
    image: ollama/ollama:latest
    container_name: ollama-server
    volumes:
      - ollama-models:/root/.ollama
      - ./custom-models:/models
    ports:
      - "11434:11434"
    deploy:
      resources:
        reservations:
          devices:
            - driver: nvidia
              count: 1
              capabilities: [gpu]
        limits:
          memory: 16G
    environment:
      - OLLAMA_NUM_GPU=1
      - OLLAMA_GPU_LAYERS=999
      - OLLAMA_CONTEXT_SIZE=16384
      - OLLAMA_MAX_LOADED_MODELS=2
    restart: unless-stopped

  # MCP Memory Server
  mcp-memory:
    image: node:20-alpine
    container_name: mcp-memory
    volumes:
      - mcp-memory-data:/data
    ports:
      - "3000:3000"
    environment:
      - MCP_STORAGE=/data/memory.db
    command: npx -y @modelcontextprotocol/server-memory --storage /data/memory.db --host 0.0.0.0 --port 3000
    restart: unless-stopped

  # MCP Sequential Thinking
  mcp-sequential:
    image: node:20-alpine
    container_name: mcp-sequential
    ports:
      - "3001:3001"
    command: npx -y @modelcontextprotocol/server-sequential-thinking --host 0.0.0.0 --port 3001
    restart: unless-stopped

  # MCP Filesystem
  mcp-filesystem:
    image: node:20-alpine
    container_name: mcp-filesystem
    volumes:
      - ../projects:/projects:ro
    ports:
      - "3003:3003"
    environment:
      - ALLOWED_DIRECTORIES=/projects
    command: npx -y @modelcontextprotocol/server-filesystem --host 0.0.0.0 --port 3003
    restart: unless-stopped

  # MCP Git
  mcp-git:
    image: node:20-alpine
    container_name: mcp-git
    volumes:
      - ../projects:/projects:ro
    ports:
      - "3004:3004"
    environment:
      - GIT_REPOS=/projects
    command: npx -y @modelcontextprotocol/server-git --host 0.0.0.0 --port 3004
    restart: unless-stopped

  # API Gateway
  gateway:
    build: ./gateway
    container_name: api-gateway
    ports:
      - "8080:8080"
    depends_on:
      - ollama
      - mcp-memory
      - mcp-sequential
    environment:
      - OLLAMA_URL=http://ollama:11434
      - MCP_MEMORY_URL=http://mcp-memory:3000
      - MCP_SEQUENTIAL_URL=http://mcp-sequential:3001
      - JWT_SECRET=${JWT_SECRET}
    restart: unless-stopped

volumes:
  ollama-models:
  mcp-memory-data:

networks:
  default:
    name: ai-network
```

### Building Custom Images

#### Gateway Dockerfile

```dockerfile
# gateway/Dockerfile
FROM python:3.11-slim

WORKDIR /app

# Install dependencies
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Copy application
COPY . .

# Expose port
EXPOSE 8080

# Run application
CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8080"]
```

#### Custom MCP Server Dockerfile

```dockerfile
# mcp-custom/Dockerfile
FROM node:20-alpine

WORKDIR /app

# Install dependencies
COPY package*.json ./
RUN npm install

# Copy server code
COPY . .

# Expose port
EXPOSE 3005

# Run server
CMD ["node", "server.js"]
```

## Kubernetes Deployment

### Ollama Deployment

```yaml
# k8s/ollama-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: ollama
  labels:
    app: ollama
spec:
  replicas: 1
  selector:
    matchLabels:
      app: ollama
  template:
    metadata:
      labels:
        app: ollama
    spec:
      containers:
      - name: ollama
        image: ollama/ollama:latest
        ports:
        - containerPort: 11434
        resources:
          limits:
            nvidia.com/gpu: 1
            memory: "16Gi"
          requests:
            nvidia.com/gpu: 1
            memory: "8Gi"
        env:
        - name: OLLAMA_NUM_GPU
          value: "1"
        - name: OLLAMA_CONTEXT_SIZE
          value: "16384"
        volumeMounts:
        - name: models
          mountPath: /root/.ollama
      volumes:
      - name: models
        persistentVolumeClaim:
          claimName: ollama-models-pvc
      nodeSelector:
        gpu: nvidia-rtx-4070

---
apiVersion: v1
kind: Service
metadata:
  name: ollama-service
spec:
  selector:
    app: ollama
  ports:
  - protocol: TCP
    port: 11434
    targetPort: 11434
  type: ClusterIP
```

### MCP Services

```yaml
# k8s/mcp-services.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: mcp-memory
spec:
  replicas: 1
  selector:
    matchLabels:
      app: mcp-memory
  template:
    metadata:
      labels:
        app: mcp-memory
    spec:
      containers:
      - name: mcp-memory
        image: node:20-alpine
        command: ["npx", "-y", "@modelcontextprotocol/server-memory"]
        args: ["--storage", "/data/memory.db", "--host", "0.0.0.0", "--port", "3000"]
        ports:
        - containerPort: 3000
        volumeMounts:
        - name: memory-data
          mountPath: /data
      volumes:
      - name: memory-data
        persistentVolumeClaim:
          claimName: mcp-memory-pvc

---
apiVersion: v1
kind: Service
metadata:
  name: mcp-memory
spec:
  selector:
    app: mcp-memory
  ports:
  - port: 3000
    targetPort: 3000
```

### Persistent Storage

```yaml
# k8s/storage.yaml
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: ollama-models-pvc
spec:
  accessModes:
    - ReadWriteOnce
  resources:
    requests:
      storage: 500Gi
  storageClassName: fast-ssd

---
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: mcp-memory-pvc
spec:
  accessModes:
    - ReadWriteOnce
  resources:
    requests:
      storage: 10Gi
  storageClassName: standard
```

## Project Isolation with Namespaces

```yaml
# k8s/project-namespace.yaml
apiVersion: v1
kind: Namespace
metadata:
  name: project-unity-game
  labels:
    project: unity-game
    environment: development

---
apiVersion: v1
kind: ResourceQuota
metadata:
  name: project-quota
  namespace: project-unity-game
spec:
  hard:
    requests.cpu: "4"
    requests.memory: 16Gi
    requests.nvidia.com/gpu: "1"
    persistentvolumeclaims: "5"
```

## Docker Management Scripts

### Startup Script

```bash
#!/bin/bash
# start-ai-environment.sh

echo "Starting AI Development Environment..."

# Pull latest images
echo "Pulling latest images..."
docker-compose pull

# Start services
echo "Starting services..."
docker-compose up -d

# Wait for Ollama to be ready
echo "Waiting for Ollama..."
until curl -s http://localhost:11434/api/tags > /dev/null; do
    sleep 2
done

echo "Ollama ready!"

# Ensure models are downloaded
echo "Checking models..."
docker exec ollama-server ollama pull deepseek-coder:33b
docker exec ollama-server ollama pull codellama:13b

# Check MCP servers
echo "Checking MCP servers..."
curl -s http://localhost:3000/health && echo "Memory server: OK"
curl -s http://localhost:3001/health && echo "Sequential thinking: OK"

echo "AI Environment ready!"
echo "Ollama: http://localhost:11434"
echo "Gateway: http://localhost:8080"
```

### Backup Script

```bash
#!/bin/bash
# backup-ai-data.sh

BACKUP_DIR="./backups/$(date +%Y%m%d-%H%M%S)"
mkdir -p "$BACKUP_DIR"

echo "Backing up AI data..."

# Backup memory database
docker exec mcp-memory cat /data/memory.db > "$BACKUP_DIR/memory.db"

# Backup whitelist
cp ./whitelist.json "$BACKUP_DIR/"

# Backup configurations
cp docker-compose.yml "$BACKUP_DIR/"
cp -r ./gateway "$BACKUP_DIR/"

# Create archive
tar -czf "$BACKUP_DIR.tar.gz" "$BACKUP_DIR"
rm -rf "$BACKUP_DIR"

echo "Backup created: $BACKUP_DIR.tar.gz"
```

### Cleanup Script

```bash
#!/bin/bash
# cleanup-ai-environment.sh

echo "Cleaning up AI environment..."

# Stop all containers
docker-compose down

# Remove unused images
docker image prune -f

# Remove old backups (keep last 7 days)
find ./backups -name "*.tar.gz" -mtime +7 -delete

# Clean Ollama cache
docker volume rm $(docker volume ls -q --filter name=ollama-cache)

echo "Cleanup complete!"
```

## Resource Limits

### Per-Container Limits

```yaml
# Resource limits for different scenarios

# Fast completion (7B model)
resources:
  limits:
    memory: "8Gi"
    nvidia.com/gpu: "0.3"  # 30% GPU
  requests:
    memory: "4Gi"
    nvidia.com/gpu: "0.3"

# Quality generation (33B model)
resources:
  limits:
    memory: "16Gi"
    nvidia.com/gpu: "1"  # Full GPU
  requests:
    memory: "12Gi"
    nvidia.com/gpu: "1"
```

## Best Practices

1. **Use Multi-Stage Builds**: Smaller images
2. **Pin Versions**: Reproducibility
3. **Use .dockerignore**: Faster builds
4. **Health Checks**: Monitoring
5. **Persistent Volumes**: Data safety
6. **Resource Limits**: Prevent exhaustion
7. **Secrets Management**: Never hardcode
8. **Logging**: Centralized logs

## Portability

### Exporting Environment

```bash
#!/bin/bash
# export-environment.sh

EXPORT_DIR="ai-environment-export"
mkdir -p "$EXPORT_DIR"

# Export Docker images
docker save ollama/ollama:latest | gzip > "$EXPORT_DIR/ollama.tar.gz"
docker save node:20-alpine | gzip > "$EXPORT_DIR/node.tar.gz"

# Copy configurations
cp docker-compose.yml "$EXPORT_DIR/"
cp -r gateway "$EXPORT_DIR/"
cp -r mcp-custom "$EXPORT_DIR/"
cp whitelist.json "$EXPORT_DIR/"

# Create README
cat > "$EXPORT_DIR/README.md" << 'ENDREADME'
# AI Development Environment

## Import Instructions

1. Load Docker images:
   ```bash
   docker load < ollama.tar.gz
   docker load < node.tar.gz
   ```

2. Start environment:
   ```bash
   docker-compose up -d
   ```

3. Download models:
   ```bash
   docker exec ollama-server ollama pull deepseek-coder:33b
   ```
ENDREADME

# Create archive
tar -czf ai-environment.tar.gz "$EXPORT_DIR"
rm -rf "$EXPORT_DIR"

echo "Environment exported to: ai-environment.tar.gz"
```

## Monitoring

### Prometheus Configuration

```yaml
# prometheus/prometheus.yml
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: 'ollama'
    static_configs:
      - targets: ['ollama:11434']
  
  - job_name: 'mcp-servers'
    static_configs:
      - targets:
        - 'mcp-memory:3000'
        - 'mcp-sequential:3001'
```

## Conclusion

Containerization provides isolation, reproducibility, and ease of deployment for local LLM agents. Use Docker Compose for single-machine setups and Kubernetes for multi-machine or high-availability scenarios.

**Recommended Approach**:
- Development: Docker Compose
- Production: Kubernetes with proper resource limits
- Portability: Export scripts for sharing

## Next Steps

- [Resource Comparison Matrix](../comparisons/resources-matrix.md)
- [Deployment Models](../deployment/local-only.md)
- [Security Configuration](./security.md)
