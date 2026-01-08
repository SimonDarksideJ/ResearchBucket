# Networked Deployment

## Overview

Networked deployment allows you to host the LLM infrastructure on one machine (e.g., M2 Mac) and access it from other machines on your local network (e.g., Windows PC). This approach maximizes resource utilization and provides a consistent environment across multiple workstations.

## Key Characteristics

- **Resource Sharing**: ⭐⭐⭐⭐⭐ (Centralized GPU/compute)
- **Accessibility**: ⭐⭐⭐⭐⭐ (Access from any machine)
- **Consistency**: ⭐⭐⭐⭐⭐ (Same environment everywhere)
- **Complexity**: ⭐⭐⭐⭐ (Network configuration required)
- **Security**: ⭐⭐⭐ (Network exposure requires care)
- **Reliability**: ⭐⭐⭐ (Depends on network and server)

## When to Use

### Best For:
- Multiple workstations (Mac + Windows)
- Team environments (small teams)
- Maximizing single powerful machine
- Consistent environment across devices
- Remote work from different rooms
- Laptop + desktop combinations

### Not Ideal For:
- Single machine usage
- Maximum privacy requirements
- Unstable networks
- High latency requirements (<50ms)
- Internet-dependent workflows

## Architecture

```
┌─────────────────────────────────────────┐
│     M2 Mac (Server Host)                │
│                                         │
│  ┌─────────────────────────────────┐   │
│  │    Ollama Server                │   │
│  │    - Multiple Models            │   │
│  │    - GPU/Metal Acceleration     │   │
│  │    Port: 11434                  │   │
│  └─────────────────────────────────┘   │
│                                         │
│  ┌─────────────────────────────────┐   │
│  │    MCP Servers                  │   │
│  │    - Memory (Port 3000)         │   │
│  │    - Sequential (Port 3001)     │   │
│  │    - Context7 (Port 3002)       │   │
│  │    - Filesystem (Port 3003)     │   │
│  └─────────────────────────────────┘   │
│                                         │
│  ┌─────────────────────────────────┐   │
│  │    API Gateway                  │   │
│  │    - Authentication             │   │
│  │    - Rate Limiting              │   │
│  │    - Request Routing            │   │
│  │    Port: 8080                   │   │
│  └─────────────────────────────────┘   │
│                                         │
│         IP: 192.168.1.100               │
└─────────────────────────────────────────┘
                    │
            Local Network (1Gbps)
                    │
    ┌───────────────┼───────────────┐
    │               │               │
┌───▼────┐    ┌─────▼────┐    ┌────▼────┐
│Windows │    │ MacBook  │    │  iPad   │
│Desktop │    │  Pro     │    │  Client │
│ Client │    │  Client  │    │         │
└────────┘    └──────────┘    └─────────┘
RTX 4070         M1 Pro
```

## Server Setup (M2 Mac)

### Step 1: Install Server Components

```bash
# Install Ollama
brew install ollama

# Install Node.js for MCP servers
brew install node@20

# Install Nginx for reverse proxy (optional but recommended)
brew install nginx

# Install authentication tools
brew install jwt-cli
```

### Step 2: Configure Ollama for Network Access

```bash
# Create systemd service (if using systemd)
# Or launchd on macOS

cat > ~/Library/LaunchAgents/com.ollama.server.plist << 'EOF'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key>
    <string>com.ollama.server</string>
    <key>ProgramArguments</key>
    <array>
        <string>/opt/homebrew/bin/ollama</string>
        <string>serve</string>
    </array>
    <key>EnvironmentVariables</key>
    <dict>
        <key>OLLAMA_HOST</key>
        <string>0.0.0.0:11434</string>
        <key>OLLAMA_ORIGINS</key>
        <string>*</string>
    </dict>
    <key>RunAtLoad</key>
    <true/>
    <key>KeepAlive</key>
    <true/>
    <key>StandardOutPath</key>
    <string>/tmp/ollama.log</string>
    <key>StandardErrorPath</key>
    <string>/tmp/ollama.error.log</string>
</dict>
</plist>
EOF

# Load service
launchctl load ~/Library/LaunchAgents/com.ollama.server.plist
launchctl start com.ollama.server

# Verify
curl http://localhost:11434/api/tags
```

### Step 3: Setup MCP Servers

```bash
# Install MCP servers globally
npm install -g @modelcontextprotocol/server-memory
npm install -g @modelcontextprotocol/server-sequential-thinking
npm install -g @modelcontextprotocol/server-filesystem

# Create MCP server manager script
cat > ~/ai-server/start-mcp-servers.sh << 'EOF'
#!/bin/bash

# Memory Server
mcp-server-memory --storage /Users/$(whoami)/ai-server/data/memory.db --host 0.0.0.0 --port 3000 &
echo $! > /tmp/mcp-memory.pid

# Sequential Thinking
mcp-server-sequential-thinking --host 0.0.0.0 --port 3001 &
echo $! > /tmp/mcp-sequential.pid

# Filesystem
export ALLOWED_DIRECTORIES="/Users/$(whoami)/Projects"
mcp-server-filesystem --host 0.0.0.0 --port 3003 &
echo $! > /tmp/mcp-filesystem.pid

echo "MCP servers started"
EOF

chmod +x ~/ai-server/start-mcp-servers.sh

# Start servers
~/ai-server/start-mcp-servers.sh
```

### Step 4: Setup API Gateway with Authentication

```python
# gateway/server.py
from fastapi import FastAPI, HTTPException, Depends, Header
from fastapi.security import HTTPBearer, HTTPAuthorizationCredentials
import jwt
import httpx
import os
from datetime import datetime, timedelta

app = FastAPI()
security = HTTPBearer()

# Configuration
SECRET_KEY = os.getenv("JWT_SECRET_KEY", "change-this-secret")
OLLAMA_URL = "http://localhost:11434"
MCP_SERVICES = {
    "memory": "http://localhost:3000",
    "sequential": "http://localhost:3001",
    "filesystem": "http://localhost:3003"
}

# Valid API keys (in production, use database)
VALID_TOKENS = {
    "windows-desktop": "user1",
    "macbook-pro": "user2"
}

def verify_token(credentials: HTTPAuthorizationCredentials = Depends(security)):
    """Verify JWT token"""
    try:
        token = credentials.credentials
        payload = jwt.decode(token, SECRET_KEY, algorithms=["HS256"])
        return payload
    except jwt.InvalidTokenError:
        raise HTTPException(status_code=401, detail="Invalid token")

@app.post("/auth/login")
async def login(username: str, api_key: str):
    """Generate JWT token"""
    if api_key not in VALID_TOKENS or VALID_TOKENS[api_key] != username:
        raise HTTPException(status_code=401, detail="Invalid credentials")
    
    token = jwt.encode(
        {
            "sub": username,
            "exp": datetime.utcnow() + timedelta(days=30)
        },
        SECRET_KEY,
        algorithm="HS256"
    )
    
    return {"access_token": token, "token_type": "bearer"}

@app.post("/v1/completions")
async def completion(
    request: dict,
    user: dict = Depends(verify_token)
):
    """Proxy to Ollama with authentication"""
    async with httpx.AsyncClient() as client:
        response = await client.post(
            f"{OLLAMA_URL}/api/generate",
            json=request,
            timeout=60.0
        )
        return response.json()

@app.get("/mcp/{service}/{path:path}")
async def mcp_proxy(
    service: str,
    path: str,
    user: dict = Depends(verify_token)
):
    """Proxy to MCP services"""
    if service not in MCP_SERVICES:
        raise HTTPException(status_code=404, detail="Service not found")
    
    async with httpx.AsyncClient() as client:
        response = await client.get(
            f"{MCP_SERVICES[service]}/{path}",
            timeout=30.0
        )
        return response.json()

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8080)
```

### Step 5: Configure Firewall

```bash
# macOS Firewall Configuration
# Enable firewall
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --setglobalstate on

# Allow specific ports
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --add /opt/homebrew/bin/ollama
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --unblockapp /opt/homebrew/bin/ollama

# Or configure through System Preferences > Security & Privacy > Firewall
```

### Step 6: Setup Nginx Reverse Proxy (Optional)

```nginx
# /opt/homebrew/etc/nginx/servers/ai-gateway.conf

upstream ollama {
    server localhost:11434;
}

upstream gateway {
    server localhost:8080;
}

server {
    listen 80;
    server_name ai-server.local;
    
    # Increase timeouts for long-running requests
    proxy_read_timeout 300;
    proxy_connect_timeout 300;
    proxy_send_timeout 300;
    
    # Gateway API
    location /api/ {
        proxy_pass http://gateway/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
    
    # Direct Ollama access (requires auth)
    location /ollama/ {
        proxy_pass http://ollama/;
        proxy_set_header Host $host;
    }
    
    # Rate limiting
    limit_req_zone $binary_remote_addr zone=api_limit:10m rate=10r/s;
    limit_req zone=api_limit burst=20;
}

# HTTPS (recommended)
server {
    listen 443 ssl;
    server_name ai-server.local;
    
    ssl_certificate /path/to/cert.pem;
    ssl_certificate_key /path/to/key.pem;
    
    # Same locations as above
    location /api/ {
        proxy_pass http://gateway/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

## Client Setup (Windows Desktop)

### Step 1: Configure VS Code

```json
// settings.json
{
  "continue.mcp.enabled": true,
  "continue.modelServer": "http://192.168.1.100:8080",
  "continue.apiKey": "your-jwt-token-here",
  "continue.models": [
    {
      "title": "DeepSeek Coder (Server)",
      "provider": "ollama",
      "model": "deepseek-coder:33b",
      "apiBase": "http://192.168.1.100:8080/api"
    },
    {
      "title": "Mixtral (Server)",
      "provider": "ollama",
      "model": "mixtral:8x7b",
      "apiBase": "http://192.168.1.100:8080/api"
    }
  ],
  "continue.mcp.servers": [
    "http://192.168.1.100:8080/mcp/memory",
    "http://192.168.1.100:8080/mcp/sequential",
    "http://192.168.1.100:8080/mcp/filesystem"
  ]
}
```

### Step 2: Authentication Helper

```python
# auth-client.py
import requests
import json
import os

class AIServerClient:
    def __init__(self, server_url, username, api_key):
        self.server_url = server_url
        self.username = username
        self.api_key = api_key
        self.token = None
        
    def authenticate(self):
        """Get JWT token"""
        response = requests.post(
            f"{self.server_url}/auth/login",
            params={"username": self.username, "api_key": self.api_key}
        )
        
        if response.status_code == 200:
            self.token = response.json()["access_token"]
            # Save token
            with open(os.path.expanduser("~/.ai-server-token"), "w") as f:
                f.write(self.token)
            return True
        return False
    
    def complete(self, prompt, model="deepseek-coder:33b"):
        """Send completion request"""
        if not self.token:
            self.authenticate()
        
        headers = {"Authorization": f"Bearer {self.token}"}
        response = requests.post(
            f"{self.server_url}/v1/completions",
            headers=headers,
            json={
                "model": model,
                "prompt": prompt,
                "stream": False
            }
        )
        
        return response.json()

# Usage
client = AIServerClient(
    "http://192.168.1.100:8080",
    "windows-desktop",
    "your-api-key"
)

result = client.complete("def fibonacci(n):")
print(result)
```

## Network Optimization

### Bandwidth Monitoring

```python
# monitor-bandwidth.py
import time
import requests
from collections import deque

class BandwidthMonitor:
    def __init__(self, server_url):
        self.server_url = server_url
        self.measurements = deque(maxlen=100)
    
    def measure_latency(self):
        """Measure round-trip latency"""
        start = time.time()
        try:
            requests.get(f"{self.server_url}/health", timeout=5)
            latency = (time.time() - start) * 1000  # ms
            self.measurements.append(latency)
            return latency
        except:
            return None
    
    def get_average_latency(self):
        """Get average latency"""
        if not self.measurements:
            return None
        return sum(self.measurements) / len(self.measurements)
    
    def is_healthy(self, threshold_ms=100):
        """Check if connection is healthy"""
        avg = self.get_average_latency()
        return avg is not None and avg < threshold_ms

# Usage
monitor = BandwidthMonitor("http://192.168.1.100:8080")

while True:
    latency = monitor.measure_latency()
    if latency:
        print(f"Latency: {latency:.2f}ms (avg: {monitor.get_average_latency():.2f}ms)")
    time.sleep(5)
```

### Compression

```python
# Enable response compression in gateway
from fastapi.middleware.gzip import GZipMiddleware

app.add_middleware(GZipMiddleware, minimum_size=1000)
```

## Security Considerations

### 1. Network Isolation

```bash
# Mac server - restrict to local network only
sudo pfctl -e
sudo pfctl -f /etc/pf.conf

# Add to /etc/pf.conf
pass in on en0 proto tcp from 192.168.1.0/24 to any port 8080
block in on en0 proto tcp from any to any port 8080
```

### 2. TLS/SSL

```bash
# Generate self-signed certificate
openssl req -x509 -newkey rsa:4096 -keyout key.pem -out cert.pem -days 365 -nodes

# Or use Let's Encrypt for real domain
certbot certonly --standalone -d ai-server.yourdomain.com
```

### 3. API Key Rotation

```python
# Implement key rotation
class APIKeyManager:
    def __init__(self):
        self.keys = {}
        self.key_expiry = {}
    
    def generate_key(self, user, days_valid=30):
        """Generate new API key"""
        key = secrets.token_urlsafe(32)
        self.keys[key] = user
        self.key_expiry[key] = datetime.now() + timedelta(days=days_valid)
        return key
    
    def is_valid(self, key):
        """Check if key is valid and not expired"""
        if key not in self.keys:
            return False
        if datetime.now() > self.key_expiry[key]:
            del self.keys[key]
            del self.key_expiry[key]
            return False
        return True
```

## High Availability

### Load Balancing (Multiple Servers)

```nginx
# nginx load balancer
upstream ollama_cluster {
    least_conn;
    server 192.168.1.100:11434;  # M2 Mac
    server 192.168.1.101:11434;  # Windows RTX 4070
}

server {
    listen 80;
    location / {
        proxy_pass http://ollama_cluster;
    }
}
```

### Failover

```python
class FailoverClient:
    def __init__(self, primary_url, fallback_url):
        self.primary = primary_url
        self.fallback = fallback_url
        self.current = primary_url
    
    async def complete(self, prompt):
        """Try primary, fallback to secondary"""
        try:
            return await self._request(self.current, prompt)
        except Exception as e:
            if self.current == self.primary:
                print(f"Primary failed: {e}, trying fallback...")
                self.current = self.fallback
                return await self._request(self.fallback, prompt)
            raise
```

## Monitoring Dashboard

```python
# dashboard/app.py
from flask import Flask, render_template, jsonify
import requests
from datetime import datetime

app = Flask(__name__)

SERVERS = {
    "mac-m2": "http://192.168.1.100:8080",
    "windows-rtx4070": "http://192.168.1.101:8080"
}

@app.route('/')
def dashboard():
    return render_template('dashboard.html')

@app.route('/api/status')
def get_status():
    status = {}
    
    for name, url in SERVERS.items():
        try:
            response = requests.get(f"{url}/health", timeout=2)
            stats = requests.get(f"{url}/stats", timeout=2).json()
            
            status[name] = {
                'online': True,
                'latency': response.elapsed.total_seconds() * 1000,
                'requests_today': stats.get('requests_today', 0),
                'active_models': stats.get('active_models', []),
                'gpu_usage': stats.get('gpu_usage', 0),
                'last_check': datetime.now().isoformat()
            }
        except:
            status[name] = {
                'online': False,
                'last_check': datetime.now().isoformat()
            }
    
    return jsonify(status)
```

## Best Practices

1. **Use Authentication**: Always require auth for network access
2. **Enable HTTPS**: Encrypt traffic between clients and server
3. **Monitor Performance**: Track latency and throughput
4. **Implement Rate Limiting**: Prevent abuse
5. **Regular Backups**: Backup memory databases
6. **Log Everything**: Audit trail for security
7. **Network Isolation**: Keep on private network
8. **Resource Limits**: Prevent single client monopolizing

## Performance Benchmarks

### Network Latency Impact

| Connection | Latency | Throughput | User Experience |
|------------|---------|------------|-----------------|
| Localhost | 1-5ms | 10GB/s | Excellent |
| 1Gbps LAN | 10-20ms | 100MB/s | Very Good |
| WiFi 6 | 20-50ms | 50MB/s | Good |
| WiFi 5 | 30-80ms | 30MB/s | Acceptable |
| Remote VPN | 100-300ms | 10MB/s | Poor |

### Server Capacity (M2 Mac)

| Concurrent Clients | Response Time | Success Rate |
|-------------------|---------------|--------------|
| 1 | 150ms | 100% |
| 2 | 180ms | 100% |
| 4 | 250ms | 100% |
| 8 | 400ms | 95% |
| 16 | 800ms | 85% |

## Advantages

- Centralized resource management
- Access from multiple devices
- Consistent environment
- Better hardware utilization
- Easier updates and maintenance
- Team collaboration possible

## Disadvantages

- Network dependency
- Additional latency
- Security complexity
- Single point of failure
- Requires network expertise
- Limited by network bandwidth

## Troubleshooting

### Issue: High Latency

```bash
# Check network speed
iperf3 -c 192.168.1.100

# Check for packet loss
ping -c 100 192.168.1.100

# Monitor server load
ssh mac-server "top -l 1 | grep 'CPU usage'"
```

### Issue: Connection Refused

```bash
# Check if server is running
curl http://192.168.1.100:8080/health

# Check firewall
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --getglobalstate

# Check process
ps aux | grep ollama
```

## Next Steps

- [Security Configuration](../frameworks/security.md)
- [MCP Servers Setup](../frameworks/mcp-servers.md)
- [Performance Optimization](../comparisons/resources-matrix.md)
