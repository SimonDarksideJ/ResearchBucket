# Local-Only Deployment

## Overview

Local-only deployment represents the most secure and private approach to running LLM agents. All processing occurs on your local machine with no external network dependencies, ensuring complete data sovereignty and maximum privacy.

## Key Characteristics

- **Privacy**: ⭐⭐⭐⭐⭐ (Maximum - no data leaves machine)
- **Security**: ⭐⭐⭐⭐⭐ (Complete isolation)
- **Latency**: ⭐⭐⭐⭐⭐ (No network overhead)
- **Complexity**: ⭐⭐⭐ (Moderate - simpler than distributed)
- **Cost**: ⭐⭐⭐⭐⭐ (Only hardware and electricity)
- **Capability**: ⭐⭐⭐⭐ (Limited by local resources)

## When to Use

### Best For:
- Proprietary/confidential codebases
- Regulated industries (healthcare, finance, defense)
- No internet access environments
- Complete privacy requirements
- Predictable, offline operation

### Not Ideal For:
- Need for latest/largest models
- Limited local hardware
- Multi-user team environments
- Tasks requiring web search

## Architecture

```
┌─────────────────────────────────────┐
│      Windows RTX 4070 Machine       │
│                                     │
│  ┌──────────────────────────────┐  │
│  │   LLM Model (Ollama/vLLM)    │  │
│  │  - Mixtral 8x7B / DeepSeek   │  │
│  │  - GPU Accelerated           │  │
│  └──────────────────────────────┘  │
│              ↕                      │
│  ┌──────────────────────────────┐  │
│  │     MCP Servers (Local)      │  │
│  │  - Memory (SQLite)           │  │
│  │  - Sequential Thinking       │  │
│  │  - Filesystem (project dirs) │  │
│  │  - Git (local repos)         │  │
│  └──────────────────────────────┘  │
│              ↕                      │
│  ┌──────────────────────────────┐  │
│  │         IDE/Editor           │  │
│  │  - VS Code with extensions   │  │
│  │  - JetBrains IDEs            │  │
│  └──────────────────────────────┘  │
│                                     │
│  ┌──────────────────────────────┐  │
│  │     Local Storage            │  │
│  │  - Models: 50-200GB          │  │
│  │  - Memory DB: 1-5GB          │  │
│  │  - Project code              │  │
│  └──────────────────────────────┘  │
└─────────────────────────────────────┘
     No Network Connection Required
```

## Hardware Requirements

### Minimum Viable Setup

**Windows PC**:
- GPU: GTX 1660 Ti (6GB VRAM)
- RAM: 16GB
- Storage: 100GB SSD
- Models: 7B parameter models

**Mac**:
- M1/M2 with 16GB unified memory
- Storage: 100GB SSD
- Models: 7B-13B parameter models

### Recommended Setup (RTX 4070)

**Windows PC**:
- GPU: RTX 4070 (12GB VRAM)
- RAM: 32-64GB
- Storage: 500GB NVMe SSD
- CPU: Intel i7/i9 or AMD Ryzen 7/9
- Models: Up to 33B parameters comfortably

**What This Enables**:
- Multiple models loaded simultaneously
- Fast inference (<100ms for most tasks)
- Large context windows (16K+ tokens)
- Multiple project containers

### High-End Setup

**Workstation**:
- GPU: RTX 4090 (24GB) or dual RTX 4070
- RAM: 64-128GB
- Storage: 1TB+ NVMe SSD
- CPU: High-end workstation CPU
- Models: Up to 70B parameters (quantized)

## Installation Guide

### Step 1: Install Core Components

#### Windows RTX 4070

```bash
# Install Ollama
winget install Ollama.Ollama

# Install Node.js for MCP servers
winget install OpenJS.NodeJS.LTS

# Install Docker Desktop (for containerization)
winget install Docker.DockerDesktop

# Install VS Code
winget install Microsoft.VisualStudioCode
```

#### M2 Mac

```bash
# Install Ollama
brew install ollama

# Install Node.js
brew install node@20

# Install Docker Desktop
brew install --cask docker

# Install VS Code
brew install --cask visual-studio-code
```

### Step 2: Pull Models

```bash
# Essential models for coding
ollama pull deepseek-coder:33b     # Primary coding model
ollama pull mixtral:8x7b           # General reasoning
ollama pull codellama:13b          # Fast code completion
ollama pull nomic-embed-text       # Embeddings for RAG

# Verify downloads
ollama list
```

### Step 3: Setup MCP Servers Locally

```bash
# Create MCP configuration directory
mkdir -p ~/.mcp/servers

# Install MCP servers globally
npm install -g @modelcontextprotocol/server-memory
npm install -g @modelcontextprotocol/server-sequential-thinking
npm install -g @modelcontextprotocol/server-filesystem

# Create MCP configuration
cat > ~/.mcp/config.json << 'EOF'
{
  "mcpServers": {
    "memory": {
      "command": "mcp-server-memory",
      "args": ["--storage", "/home/user/.mcp/memory.db"]
    },
    "sequential-thinking": {
      "command": "mcp-server-sequential-thinking"
    },
    "filesystem": {
      "command": "mcp-server-filesystem",
      "env": {
        "ALLOWED_DIRECTORIES": "/path/to/projects,/path/to/libraries"
      }
    },
    "git": {
      "command": "mcp-server-git",
      "env": {
        "GIT_REPOS": "/path/to/projects"
      }
    }
  }
}
EOF
```

### Step 4: Configure VS Code

```json
// settings.json
{
  "continue.mcp.enabled": true,
  "continue.mcp.configPath": "~/.mcp/config.json",
  "continue.modelServer": "http://localhost:11434",
  "continue.models": [
    {
      "title": "DeepSeek Coder 33B",
      "provider": "ollama",
      "model": "deepseek-coder:33b",
      "contextLength": 16384
    },
    {
      "title": "Mixtral 8x7B",
      "provider": "ollama",
      "model": "mixtral:8x7b",
      "contextLength": 32768
    },
    {
      "title": "CodeLlama 13B (Fast)",
      "provider": "ollama",
      "model": "codellama:13b",
      "contextLength": 8192
    }
  ],
  "continue.tabAutocompleteModel": {
    "title": "CodeLlama 13B",
    "provider": "ollama",
    "model": "codellama:13b"
  }
}
```

### Step 5: Setup Local RAG (Optional but Recommended)

```python
# Install dependencies
pip install sentence-transformers faiss-cpu chromadb

# Create local knowledge base
from chromadb import Client
from chromadb.config import Settings

# Initialize local vector DB
client = Client(Settings(
    chroma_db_impl="duckdb+parquet",
    persist_directory="./local_knowledge"
))

# Create collection for your codebase
collection = client.create_collection(
    name="my_codebase",
    metadata={"description": "Local code knowledge base"}
)

# Index your projects (run this once)
def index_project(project_path):
    from sentence_transformers import SentenceTransformer
    model = SentenceTransformer('all-MiniLM-L6-v2')
    
    # Walk through code files
    for root, dirs, files in os.walk(project_path):
        for file in files:
            if file.endswith(('.cs', '.ts', '.tsx', '.js', '.jsx', '.py')):
                with open(os.path.join(root, file), 'r') as f:
                    content = f.read()
                    embedding = model.encode(content)
                    collection.add(
                        documents=[content],
                        embeddings=[embedding.tolist()],
                        ids=[os.path.join(root, file)]
                    )
```

## Security Configuration

### Network Isolation

#### Windows Firewall Rules

```powershell
# Block all outbound connections from Ollama
New-NetFirewallRule -DisplayName "Block Ollama Outbound" `
  -Direction Outbound `
  -Program "C:\Users\<User>\AppData\Local\Programs\Ollama\ollama.exe" `
  -Action Block

# Allow only localhost connections
New-NetFirewallRule -DisplayName "Allow Ollama Localhost" `
  -Direction Inbound `
  -Program "C:\Users\<User>\AppData\Local\Programs\Ollama\ollama.exe" `
  -LocalAddress 127.0.0.1 `
  -Action Allow
```

#### Mac Firewall (Little Snitch or built-in)

```bash
# Using built-in pf firewall
# Create pf rule file
sudo cat > /etc/pf.anchors/ollama.block << 'EOF'
# Block all network access for ollama except localhost
block out proto tcp from any to !127.0.0.1
block out proto udp from any to !127.0.0.1
EOF

# Load rule
sudo pfctl -e
sudo pfctl -f /etc/pf.conf
```

### Data Isolation

```bash
# Create dedicated partition/directory for AI data
# Linux/Mac
mkdir -p ~/ai-workspace/{models,memory,projects,cache}
chmod 700 ~/ai-workspace

# Set environment variables for isolation
export OLLAMA_MODELS=~/ai-workspace/models
export MCP_MEMORY_PATH=~/ai-workspace/memory
export AI_CACHE_DIR=~/ai-workspace/cache

# Windows (PowerShell)
$env:OLLAMA_MODELS = "$HOME\ai-workspace\models"
$env:MCP_MEMORY_PATH = "$HOME\ai-workspace\memory"
```

### File Access Control

```json
// Strict filesystem MCP configuration
{
  "filesystem": {
    "command": "mcp-server-filesystem",
    "env": {
      "ALLOWED_DIRECTORIES": "/specific/project/path",
      "READONLY_MODE": "false",
      "MAX_FILE_SIZE": "10485760",
      "ALLOWED_EXTENSIONS": ".cs,.ts,.tsx,.js,.jsx,.py,.md,.json"
    }
  }
}
```

## Performance Optimization

### GPU Optimization (RTX 4070)

```bash
# Set environment variables
set CUDA_VISIBLE_DEVICES=0
set PYTORCH_CUDA_ALLOC_CONF=max_split_size_mb:512

# Ollama optimization
set OLLAMA_NUM_GPU=1
set OLLAMA_GPU_LAYERS=999
set OLLAMA_MAX_LOADED_MODELS=3
set OLLAMA_CONTEXT_SIZE=16384
set OLLAMA_NUM_PARALLEL=2

# CUDA cache
set CUDA_CACHE_PATH=D:\ai-workspace\cuda_cache
```

### Memory Management

```python
# Configure model loading strategy
class ModelManager:
    def __init__(self):
        self.loaded_models = {}
        self.max_loaded = 3
        
    def load_model(self, model_name):
        if len(self.loaded_models) >= self.max_loaded:
            # Unload least recently used
            lru_model = min(self.loaded_models.items(), 
                          key=lambda x: x[1]['last_used'])
            self.unload_model(lru_model[0])
        
        # Load new model
        self.loaded_models[model_name] = {
            'instance': load_model(model_name),
            'last_used': time.time()
        }
```

### Storage Optimization

```bash
# Use symbolic links for model sharing
# Instead of duplicating models
ln -s ~/.ollama/models/deepseek-coder-33b ~/.mcp/models/

# Compress old memory databases
sqlite3 ~/.mcp/memory.db "VACUUM;"

# Clean up old cached embeddings
find ~/ai-workspace/cache -name "*.emb" -mtime +30 -delete
```

## Monitoring and Maintenance

### Resource Monitoring Script

```python
# monitor.py
import psutil
import GPUtil
import time
from datetime import datetime

def monitor_resources():
    while True:
        # GPU stats
        gpus = GPUtil.getGPUs()
        for gpu in gpus:
            print(f"GPU {gpu.id}: {gpu.load*100:.1f}% | "
                  f"Memory: {gpu.memoryUsed}/{gpu.memoryTotal}MB | "
                  f"Temp: {gpu.temperature}°C")
        
        # RAM stats
        ram = psutil.virtual_memory()
        print(f"RAM: {ram.percent:.1f}% | "
              f"Used: {ram.used/1e9:.1f}GB/{ram.total/1e9:.1f}GB")
        
        # Disk I/O
        disk = psutil.disk_usage('/')
        print(f"Disk: {disk.percent:.1f}% | "
              f"Used: {disk.used/1e9:.1f}GB/{disk.total/1e9:.1f}GB")
        
        print("-" * 60)
        time.sleep(5)

if __name__ == "__main__":
    monitor_resources()
```

### Automated Cleanup

```bash
#!/bin/bash
# cleanup.sh - Run weekly

# Vacuum memory database
echo "Compacting memory database..."
sqlite3 ~/.mcp/memory.db "VACUUM;"

# Remove old embeddings cache
echo "Cleaning old cache..."
find ~/ai-workspace/cache -type f -mtime +30 -delete

# Prune unused models
echo "Pruning unused models..."
ollama list | grep "weeks ago" | awk '{print $1}' | xargs -I {} ollama rm {}

# Check disk space
echo "Disk usage:"
du -sh ~/ai-workspace/*

echo "Cleanup complete!"
```

## Troubleshooting

### Issue 1: Out of Memory

**Symptoms**: Model loading fails, system freezes

**Solutions**:
```bash
# Use smaller models
ollama pull deepseek-coder:6.7b  # Instead of 33b

# Reduce context size
export OLLAMA_CONTEXT_SIZE=8192  # Instead of 16384

# Enable CPU offloading
export OLLAMA_GPU_LAYERS=20  # Instead of 999
```

### Issue 2: Slow Inference

**Symptoms**: Response time >5 seconds

**Solutions**:
```bash
# Check GPU usage
nvidia-smi

# Ensure CUDA is being used
ollama run deepseek-coder:33b --verbose

# Try quantized version
ollama pull deepseek-coder:33b-q4_K_M
```

### Issue 3: Model Won't Load

**Symptoms**: "Failed to load model" error

**Solutions**:
```bash
# Check available VRAM
nvidia-smi

# Verify model file integrity
ollama pull deepseek-coder:33b --force

# Check logs
tail -f ~/.ollama/logs/server.log
```

## Containerized Local-Only Setup

### Docker Compose

```yaml
version: '3.8'

services:
  ollama:
    image: ollama/ollama:latest
    container_name: local-ollama
    volumes:
      - ./models:/root/.ollama
      - ./projects:/projects:ro
    deploy:
      resources:
        reservations:
          devices:
            - driver: nvidia
              count: 1
              capabilities: [gpu]
    environment:
      - OLLAMA_GPU_LAYERS=999
      - OLLAMA_MAX_LOADED_MODELS=3
    network_mode: none  # No network access!
    
  mcp-memory:
    image: node:20-alpine
    container_name: local-mcp-memory
    volumes:
      - ./memory:/data
    network_mode: none
    command: npx @modelcontextprotocol/server-memory --storage /data/memory.db
    
  mcp-filesystem:
    image: node:20-alpine
    container_name: local-mcp-filesystem
    volumes:
      - ./projects:/projects:ro
    environment:
      - ALLOWED_DIRECTORIES=/projects
    network_mode: none
    command: npx @modelcontextprotocol/server-filesystem

volumes:
  models:
  memory:
  projects:
```

## Best Practices

### 1. Model Selection
- **Use appropriate model sizes**: Don't use 33B when 13B suffices
- **Keep fast model loaded**: For quick completions
- **Specialize models**: Different models for different tasks

### 2. Memory Management
- **Regular cleanup**: Weekly vacuum of memory DB
- **Limit history**: Keep only last 30 days of conversations
- **Smart caching**: Cache frequent code patterns

### 3. Security
- **Network isolation**: Block all external access
- **File permissions**: Restrict filesystem access
- **Audit logs**: Keep logs of file access and generations

### 4. Backup Strategy
- **Model backups**: Keep model files on external drive
- **Memory snapshots**: Weekly backups of memory DB
- **Configuration**: Version control MCP configs

## Performance Benchmarks (RTX 4070)

### Single Model Performance

| Model | Load Time | First Token | Throughput | VRAM Usage |
|-------|-----------|-------------|------------|------------|
| CodeLlama 13B | 5s | 80ms | 120 tok/s | 8.5GB |
| DeepSeek 33B | 12s | 150ms | 60 tok/s | 11.8GB |
| Mixtral 8x7B | 10s | 180ms | 55 tok/s | 11.2GB |

### Multi-Model Scenarios

| Scenario | Models Loaded | Total VRAM | Response Time |
|----------|---------------|------------|---------------|
| Dev Setup | CodeLlama 13B + DeepSeek 6.7B | 10GB | 90ms avg |
| Quality Setup | DeepSeek 33B only | 11.8GB | 150ms avg |
| Balanced | Mixtral 8x7B + CodeLlama 7B | 11.5GB | 120ms avg |

## Cost Analysis

### Initial Investment
- **Hardware**: $0 (assuming RTX 4070 already owned)
- **Software**: $0 (all open source)
- **Setup Time**: 4-6 hours

### Ongoing Costs
- **Electricity**: ~$15/month (24/7 operation)
- **Storage**: $0 (using existing drive)
- **Maintenance**: 2 hours/month

### Compared to Cloud
- **GPT-4 API**: $50-200/month
- **Claude API**: $30-150/month
- **Local**: $15/month + initial time investment

**Breakeven**: ~2 months

## Advantages

1. **Maximum Privacy**: Zero data leakage
2. **No Latency**: No network round trips
3. **Predictable**: No rate limits or outages
4. **Cost Effective**: Only electricity costs
5. **Full Control**: Complete customization
6. **Offline**: Works without internet

## Disadvantages

1. **Limited Capability**: Smaller models than cloud
2. **Hardware Requirement**: Need decent GPU
3. **Maintenance**: Must manage everything yourself
4. **No Latest Models**: Lag behind cloud releases
5. **Single User**: One machine, one user
6. **No Web Access**: Can't search internet for help

## Conclusion

Local-only deployment is ideal for scenarios requiring maximum privacy and security. With an RTX 4070, you can run impressive models locally with excellent performance. While you trade some capability for privacy, the gap is narrowing as open source models improve.

**This approach is recommended for**:
- Proprietary/confidential code
- Offline/air-gapped environments
- Privacy-critical applications
- Cost-sensitive scenarios (after initial setup)

## Next Steps

- [Single Project Deployment](./single-project.md) - Isolate per project
- [Hybrid Deployment](./hybrid.md) - Combine local + cloud
- [MCP Servers Setup](../frameworks/mcp-servers.md)
- [Security Configuration](../frameworks/security.md)
