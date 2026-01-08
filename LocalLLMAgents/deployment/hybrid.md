# Hybrid Deployment

## Overview

Hybrid deployment combines the best of local and cloud: run lightweight models locally for speed and privacy, with intelligent fallback to cloud APIs for complex tasks requiring more computational power or larger context windows.

## Key Characteristics

- **Privacy**: ⭐⭐⭐⭐ (Mostly private, selective cloud)
- **Performance**: ⭐⭐⭐⭐⭐ (Fast local + powerful cloud)
- **Flexibility**: ⭐⭐⭐⭐⭐ (Best of both worlds)
- **Cost**: ⭐⭐⭐⭐ (Lower than full cloud)
- **Complexity**: ⭐⭐⭐⭐ (Smart routing required)
- **Reliability**: ⭐⭐⭐⭐⭐ (Local fallback if cloud down)

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│           Local Machine (RTX 4070)                      │
│                                                         │
│  ┌──────────────────────────────────────────┐          │
│  │         Smart Router / Gateway           │          │
│  │  - Complexity Analysis                   │          │
│  │  - Cost Tracking                         │          │
│  │  - Whitelist Enforcement                 │          │
│  └─────────────┬────────────────────────────┘          │
│                │                                        │
│       ┌────────┴────────┐                              │
│       │                 │                              │
│  ┌────▼─────┐     ┌─────▼──────┐                      │
│  │  Local   │     │   Cloud    │                      │
│  │  Models  │     │    APIs    │                      │
│  │          │     │            │                      │
│  │ • Fast   │     │ • Complex  │                      │
│  │ • Simple │     │ • Large    │                      │
│  │ • Private│     │ • Latest   │                      │
│  └──────────┘     └────────────┘                      │
│                          │                             │
│                          └─────────────────────────┐   │
└────────────────────────────────────────────────────┼───┘
                                                     │
                                            ┌────────▼──────┐
                                            │  Cloud APIs   │
                                            │ • GPT-4       │
                                            │ • Claude      │
                                            │ • Gemini      │
                                            └───────────────┘
```

## Routing Strategy

### Complexity-Based Routing

```python
class HybridRouter:
    def __init__(self):
        self.local_model = "http://localhost:11434"
        self.cloud_apis = {
            "gpt4": os.getenv("OPENAI_API_KEY"),
            "claude": os.getenv("ANTHROPIC_API_KEY"),
            "gemini": os.getenv("GOOGLE_API_KEY")
        }
        self.cost_tracker = CostTracker()
        
    def route_request(self, prompt, context):
        """Route to local or cloud based on complexity"""
        complexity = self.analyze_complexity(prompt, context)
        
        # Decision tree
        if complexity['score'] < 0.3:
            # Simple: Local fast model
            return self.send_to_local("codellama:13b", prompt)
            
        elif complexity['score'] < 0.6:
            # Moderate: Local quality model
            return self.send_to_local("deepseek-coder:33b", prompt)
            
        elif complexity['score'] < 0.8:
            # Complex: Local best or cheap cloud
            if self.cost_tracker.budget_remaining() > 1.0:
                return self.send_to_cloud("gemini-pro", prompt)
            else:
                return self.send_to_local("mixtral:8x7b", prompt)
                
        else:
            # Very complex: Premium cloud
            if self.cost_tracker.budget_remaining() > 5.0:
                return self.send_to_cloud("gpt-4-turbo", prompt)
            else:
                # Try best local with COT prompting
                return self.send_to_local_with_cot("mixtral:8x7b", prompt)
    
    def analyze_complexity(self, prompt, context):
        """Analyze prompt complexity"""
        score = 0.0
        
        # Length factors
        if len(prompt) > 1000: score += 0.2
        if len(context) > 10000: score += 0.3
        
        # Task type factors
        if any(word in prompt.lower() for word in 
               ['refactor', 'redesign', 'architect']):
            score += 0.3
        
        if any(word in prompt.lower() for word in
               ['debug', 'fix', 'error', 'bug']):
            score += 0.2
            
        if 'explain' in prompt.lower() and len(context) > 5000:
            score += 0.3
        
        # Context window requirement
        total_tokens = (len(prompt) + len(context)) / 4  # rough estimate
        if total_tokens > 8000: score += 0.4
        
        return {
            'score': min(score, 1.0),
            'requires_large_context': total_tokens > 8000,
            'is_complex_reasoning': score > 0.6
        }
```

### Cost-Based Routing

```python
class CostTracker:
    def __init__(self, monthly_budget=50.0):
        self.monthly_budget = monthly_budget
        self.current_month_cost = 0.0
        self.costs = {
            'gpt-4-turbo': {'input': 0.01, 'output': 0.03},  # per 1K tokens
            'claude-3-opus': {'input': 0.015, 'output': 0.075},
            'gemini-pro': {'input': 0.0005, 'output': 0.0015},
            'local': {'input': 0.0, 'output': 0.0}
        }
    
    def estimate_cost(self, model, input_tokens, output_tokens):
        """Estimate cost for request"""
        if model not in self.costs:
            return 0.0
        
        cost = (input_tokens / 1000 * self.costs[model]['input'] +
                output_tokens / 1000 * self.costs[model]['output'])
        return cost
    
    def budget_remaining(self):
        """Get remaining budget for month"""
        return self.monthly_budget - self.current_month_cost
    
    def should_use_cloud(self, estimated_cost):
        """Decide if cloud usage is within budget"""
        return self.budget_remaining() >= estimated_cost
```

### Whitelist Management

```python
class WhitelistManager:
    def __init__(self, config_path="whitelist.json"):
        self.config_path = config_path
        self.whitelist = self.load_whitelist()
    
    def load_whitelist(self):
        """Load whitelist from configuration"""
        with open(self.config_path, 'r') as f:
            return json.load(f)
    
    def is_allowed(self, request_type, destination):
        """Check if request is whitelisted"""
        if request_type == "api":
            return destination in self.whitelist.get('apis', [])
        elif request_type == "domain":
            return any(destination.endswith(d) 
                      for d in self.whitelist.get('domains', []))
        return False
    
    def allow_cloud_api(self, api_name):
        """Check if cloud API is whitelisted"""
        allowed_apis = self.whitelist.get('cloud_apis', [])
        return api_name in allowed_apis
```

### Whitelist Configuration

```json
{
  "cloud_apis": [
    "openai.com",
    "anthropic.com",
    "googleapis.com"
  ],
  "domains": [
    "github.com",
    "stackoverflow.com",
    "docs.microsoft.com"
  ],
  "mcp_servers": [
    "memory",
    "sequential-thinking",
    "context7",
    "filesystem",
    "git"
  ],
  "allowed_for_cloud": {
    "reasoning_tasks": true,
    "code_review": true,
    "architecture_design": true,
    "simple_completion": false,
    "inline_completion": false
  }
}
```

## Implementation

### Docker Compose Setup

```yaml
version: '3.8'

services:
  # Local Ollama
  ollama-local:
    image: ollama/ollama:latest
    volumes:
      - ollama-models:/root/.ollama
    deploy:
      resources:
        reservations:
          devices:
            - driver: nvidia
              count: 1
              capabilities: [gpu]
    environment:
      - OLLAMA_MAX_LOADED_MODELS=2
    networks:
      - ai-network

  # Hybrid Gateway
  gateway:
    build: ./gateway
    ports:
      - "8080:8080"
    volumes:
      - ./config:/config
      - ./whitelist.json:/app/whitelist.json
    depends_on:
      - ollama-local
    environment:
      - LOCAL_MODEL_URL=http://ollama-local:11434
      - OPENAI_API_KEY=${OPENAI_API_KEY}
      - ANTHROPIC_API_KEY=${ANTHROPIC_API_KEY}
      - GOOGLE_API_KEY=${GOOGLE_API_KEY}
      - MONTHLY_BUDGET=50
    networks:
      - ai-network

  # MCP Servers (local)
  mcp-memory:
    image: node:20-alpine
    volumes:
      - mcp-memory:/data
    command: npx -y @modelcontextprotocol/server-memory --storage /data/memory.db
    networks:
      - ai-network

  mcp-whitelist-manager:
    build: ./mcp-whitelist
    volumes:
      - ./whitelist.json:/data/whitelist.json
    command: node server.js
    networks:
      - ai-network

networks:
  ai-network:
    driver: bridge

volumes:
  ollama-models:
  mcp-memory:
```

### Gateway Implementation

```python
# gateway/app.py
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
import httpx
import os

app = FastAPI()

class Request(BaseModel):
    prompt: str
    context: str = ""
    force_local: bool = False
    force_cloud: bool = False
    max_cost: float = 10.0

router = HybridRouter()
whitelist = WhitelistManager()

@app.post("/v1/completions")
async def complete(request: Request):
    # Check whitelist
    if request.force_cloud and not whitelist.is_allowed("cloud", "api"):
        raise HTTPException(403, "Cloud access not whitelisted")
    
    # Route request
    if request.force_local:
        response = await router.send_to_local("deepseek-coder:33b", request.prompt)
    elif request.force_cloud:
        response = await router.send_to_cloud("gpt-4-turbo", request.prompt)
    else:
        response = await router.route_request(request.prompt, request.context)
    
    return response

@app.get("/stats")
async def get_stats():
    """Get usage statistics"""
    return {
        "cost_this_month": router.cost_tracker.current_month_cost,
        "budget_remaining": router.cost_tracker.budget_remaining(),
        "local_requests": router.local_count,
        "cloud_requests": router.cloud_count,
        "cost_savings": router.calculate_savings()
    }
```

### MCP Whitelist Manager

```javascript
// mcp-whitelist/server.js
const { MCPServer } = require('@modelcontextprotocol/sdk');
const fs = require('fs').promises;

class WhitelistMCPServer extends MCPServer {
  constructor() {
    super({
      name: 'whitelist-manager',
      version: '1.0.0'
    });
    
    this.whitelistPath = '/data/whitelist.json';
    this.whitelist = null;
    
    this.registerTools();
  }
  
  async loadWhitelist() {
    const data = await fs.readFile(this.whitelistPath, 'utf8');
    this.whitelist = JSON.parse(data);
  }
  
  async saveWhitelist() {
    await fs.writeFile(
      this.whitelistPath,
      JSON.stringify(this.whitelist, null, 2)
    );
  }
  
  registerTools() {
    this.registerTool({
      name: 'check_allowed',
      description: 'Check if resource is whitelisted',
      parameters: {
        type: 'object',
        properties: {
          resource_type: { type: 'string' },
          resource_name: { type: 'string' }
        }
      },
      handler: async (params) => {
        await this.loadWhitelist();
        const allowed = this.whitelist[params.resource_type]?.includes(params.resource_name);
        return { allowed };
      }
    });
    
    this.registerTool({
      name: 'add_to_whitelist',
      description: 'Add resource to whitelist',
      parameters: {
        type: 'object',
        properties: {
          resource_type: { type: 'string' },
          resource_name: { type: 'string' }
        }
      },
      handler: async (params) => {
        await this.loadWhitelist();
        if (!this.whitelist[params.resource_type]) {
          this.whitelist[params.resource_type] = [];
        }
        if (!this.whitelist[params.resource_type].includes(params.resource_name)) {
          this.whitelist[params.resource_type].push(params.resource_name);
          await this.saveWhitelist();
        }
        return { success: true };
      }
    });
  }
}

const server = new WhitelistMCPServer();
server.start();
```

## Caching Strategy

### Response Caching

```python
import hashlib
import json
from functools import lru_cache
import redis

class HybridCache:
    def __init__(self):
        self.redis_client = redis.Redis(host='localhost', port=6379, db=0)
        self.cache_ttl = 86400  # 24 hours
    
    def get_cache_key(self, prompt, context):
        """Generate cache key from prompt and context"""
        combined = f"{prompt}|{context}"
        return hashlib.sha256(combined.encode()).hexdigest()
    
    def get_cached_response(self, prompt, context):
        """Get cached response if available"""
        key = self.get_cache_key(prompt, context)
        cached = self.redis_client.get(key)
        
        if cached:
            return json.loads(cached)
        return None
    
    def cache_response(self, prompt, context, response, source="local"):
        """Cache response"""
        key = self.get_cache_key(prompt, context)
        value = {
            'response': response,
            'source': source,
            'timestamp': time.time()
        }
        self.redis_client.setex(key, self.cache_ttl, json.dumps(value))
```

## Monitoring and Analytics

### Usage Dashboard

```python
from flask import Flask, render_template, jsonify
import sqlite3
from datetime import datetime, timedelta

app = Flask(__name__)

@app.route('/dashboard')
def dashboard():
    return render_template('dashboard.html')

@app.route('/api/stats')
def get_stats():
    conn = sqlite3.connect('usage.db')
    cursor = conn.cursor()
    
    # Last 30 days stats
    thirty_days_ago = datetime.now() - timedelta(days=30)
    
    cursor.execute('''
        SELECT 
            DATE(timestamp) as date,
            source,
            COUNT(*) as count,
            SUM(cost) as total_cost,
            AVG(latency) as avg_latency
        FROM requests
        WHERE timestamp > ?
        GROUP BY DATE(timestamp), source
    ''', (thirty_days_ago,))
    
    results = cursor.fetchall()
    
    stats = {
        'daily_stats': [],
        'total_cost': 0,
        'total_requests': 0,
        'avg_latency': {}
    }
    
    for row in results:
        stats['daily_stats'].append({
            'date': row[0],
            'source': row[1],
            'count': row[2],
            'cost': row[3],
            'latency': row[4]
        })
        stats['total_cost'] += row[3]
        stats['total_requests'] += row[2]
    
    conn.close()
    return jsonify(stats)
```

## Configuration Profiles

### Conservative (Maximum Local)

```yaml
profile: conservative
monthly_budget: 10
routing_rules:
  complexity_threshold: 0.9  # Only very complex goes to cloud
  always_try_local_first: true
  cloud_models:
    - gemini-pro  # Cheapest only
  fallback: local_with_cot
```

### Balanced (Recommended)

```yaml
profile: balanced
monthly_budget: 50
routing_rules:
  complexity_threshold: 0.6
  always_try_local_first: true
  cloud_models:
    - gemini-pro      # Cost-effective
    - gpt-4-turbo     # Complex tasks
  fallback: local_best_effort
```

### Performance (Quality First)

```yaml
profile: performance
monthly_budget: 200
routing_rules:
  complexity_threshold: 0.4
  always_try_local_first: false
  cloud_models:
    - gpt-4-turbo
    - claude-3-opus
    - gemini-pro
  fallback: local
```

## Best Practices

1. **Start Local**: Always try local first unless explicitly complex
2. **Cache Aggressively**: Cache all cloud responses
3. **Monitor Costs**: Track spending daily
4. **Whitelist Strictly**: Only allow necessary cloud access
5. **Measure Everything**: Log source, latency, cost for all requests
6. **Budget Alerts**: Alert when approaching budget limits
7. **Review Routing**: Monthly review of routing decisions
8. **Update Models**: Keep local models current

## Advantages

- Fast response for simple tasks (local)
- High quality for complex tasks (cloud)
- Cost-effective (mostly local)
- Resilient (local fallback)
- Flexible (adjustable routing)

## Disadvantages

- More complex to configure
- Requires monitoring
- Need cloud account(s)
- Must manage budgets
- Potential privacy concerns (cloud)

## Next Steps

- [Networked Deployment](./networked.md)
- [Security & Whitelisting](../frameworks/security.md)
- [Cost Optimization](../comparisons/pros-cons.md)
