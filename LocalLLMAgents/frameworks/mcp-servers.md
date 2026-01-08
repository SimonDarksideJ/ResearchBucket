# MCP Servers Integration

## Overview

The Model Context Protocol (MCP) is an open standard that enables LLM applications to securely access external data sources and tools. This guide covers integrating MCP servers into your local LLM setup for enhanced capabilities.

## Core MCP Servers

### 1. Memory Server

**Purpose**: Persistent conversation history and context across sessions

**Installation**:
```bash
npm install -g @modelcontextprotocol/server-memory
```

**Configuration**:
```json
{
  "memory": {
    "command": "mcp-server-memory",
    "args": ["--storage", "/path/to/memory.db"],
    "description": "Long-term memory and context"
  }
}
```

**Use Cases**:
- Remember previous conversations
- Track project decisions
- Maintain coding patterns learned
- Store user preferences

**Storage Backend**:
- SQLite (default)
- PostgreSQL (enterprise)
- Redis (fast access)

### 2. Sequential Thinking Server

**Purpose**: Break down complex problems into steps

**Installation**:
```bash
npm install -g @modelcontextprotocol/server-sequential-thinking
```

**Configuration**:
```json
{
  "sequential-thinking": {
    "command": "mcp-server-sequential-thinking",
    "description": "Step-by-step problem solving"
  }
}
```

**Benefits**:
- Improved complex reasoning
- Better architectural decisions
- Clearer debugging processes
- Structured refactoring

### 3. Filesystem Server

**Purpose**: Secure file system access with whitelisting

**Installation**:
```bash
npm install -g @modelcontextprotocol/server-filesystem
```

**Configuration**:
```json
{
  "filesystem": {
    "command": "mcp-server-filesystem",
    "env": {
      "ALLOWED_DIRECTORIES": "/path/to/projects,/path/to/libraries",
      "READONLY_MODE": "false",
      "MAX_FILE_SIZE": "10485760"
    }
  }
}
```

**Security Features**:
- Directory whitelisting
- Read-only mode option
- File size limits
- Extension filtering

### 4. Git Server

**Purpose**: Access git repository information

**Installation**:
```bash
npm install -g @modelcontextprotocol/server-git
```

**Configuration**:
```json
{
  "git": {
    "command": "mcp-server-git",
    "env": {
      "GIT_REPOS": "/path/to/repos"
    }
  }
}
```

**Capabilities**:
- Read commit history
- View diffs
- Understand code evolution
- Identify code authors

### 5. Context7 Server

**Purpose**: Documentation search and management

**Installation**:
```bash
npm install -g context7-mcp-server
```

**Configuration**:
```json
{
  "context7": {
    "command": "context7-mcp-server",
    "env": {
      "DOCS_PATH": "/path/to/documentation",
      "INDEX_ON_START": "true"
    }
  }
}
```

**Features**:
- Search Unity docs
- Search React docs
- Custom documentation
- Code example retrieval

## Custom MCP Server Development

### Basic MCP Server Template

```javascript
// custom-mcp-server.js
const { MCPServer } = require('@modelcontextprotocol/sdk');

class CustomMCPServer extends MCPServer {
  constructor() {
    super({
      name: 'custom-server',
      version: '1.0.0',
      description: 'Custom MCP server'
    });
    
    this.registerTools();
    this.registerResources();
  }
  
  registerTools() {
    this.registerTool({
      name: 'custom_tool',
      description: 'Does something useful',
      parameters: {
        type: 'object',
        properties: {
          input: { type: 'string' }
        }
      },
      handler: async (params) => {
        // Tool implementation
        return { result: `Processed: ${params.input}` };
      }
    });
  }
  
  registerResources() {
    this.registerResource({
      uri: 'custom://data',
      name: 'Custom Data',
      description: 'Access custom data',
      mimeType: 'application/json',
      handler: async () => {
        return { data: 'custom data' };
      }
    });
  }
}

const server = new CustomMCPServer();
server.start();
```

### Whitelist Management MCP Server

```javascript
// whitelist-mcp-server.js
const { MCPServer } = require('@modelcontextprotocol/sdk');
const fs = require('fs').promises;

class WhitelistMCPServer extends MCPServer {
  constructor() {
    super({
      name: 'whitelist-manager',
      version: '1.0.0'
    });
    
    this.whitelistPath = './whitelist.json';
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
        return { allowed, resource: params.resource_name };
      }
    });
    
    this.registerTool({
      name: 'add_to_whitelist',
      description: 'Add resource to whitelist (requires approval)',
      parameters: {
        type: 'object',
        properties: {
          resource_type: { type: 'string' },
          resource_name: { type: 'string' },
          reason: { type: 'string' }
        },
        required: ['resource_type', 'resource_name', 'reason']
      },
      handler: async (params) => {
        await this.loadWhitelist();
        
        if (!this.whitelist[params.resource_type]) {
          this.whitelist[params.resource_type] = [];
        }
        
        // Add to pending approval
        if (!this.whitelist.pending) {
          this.whitelist.pending = [];
        }
        
        this.whitelist.pending.push({
          type: params.resource_type,
          name: params.resource_name,
          reason: params.reason,
          timestamp: new Date().toISOString()
        });
        
        await this.saveWhitelist();
        
        return { 
          success: true,
          message: 'Added to pending approval',
          requires_manual_approval: true
        };
      }
    });
    
    this.registerTool({
      name: 'list_whitelist',
      description: 'List all whitelisted resources',
      parameters: { type: 'object', properties: {} },
      handler: async () => {
        await this.loadWhitelist();
        return { whitelist: this.whitelist };
      }
    });
  }
}

const server = new WhitelistMCPServer();
server.start();
```

## MCP Server Orchestration

### Docker Compose for All MCP Servers

```yaml
version: '3.8'

services:
  mcp-memory:
    image: node:20-alpine
    container_name: mcp-memory
    volumes:
      - memory-data:/data
    ports:
      - "3000:3000"
    environment:
      - MCP_PORT=3000
    command: npx -y @modelcontextprotocol/server-memory --storage /data/memory.db --port 3000
    restart: unless-stopped

  mcp-sequential:
    image: node:20-alpine
    container_name: mcp-sequential
    ports:
      - "3001:3001"
    environment:
      - MCP_PORT=3001
    command: npx -y @modelcontextprotocol/server-sequential-thinking --port 3001
    restart: unless-stopped

  mcp-context7:
    image: node:20-alpine
    container_name: mcp-context7
    volumes:
      - ./documentation:/docs:ro
      - context7-index:/index
    ports:
      - "3002:3002"
    environment:
      - DOCS_PATH=/docs
      - INDEX_PATH=/index
      - MCP_PORT=3002
    command: npx -y context7-mcp-server --port 3002
    restart: unless-stopped

  mcp-filesystem:
    image: node:20-alpine
    container_name: mcp-filesystem
    volumes:
      - ../projects:/projects:ro
    ports:
      - "3003:3003"
    environment:
      - ALLOWED_DIRECTORIES=/projects
      - MCP_PORT=3003
    command: npx -y @modelcontextprotocol/server-filesystem --port 3003
    restart: unless-stopped

  mcp-git:
    image: node:20-alpine
    container_name: mcp-git
    volumes:
      - ../projects:/projects:ro
    ports:
      - "3004:3004"
    environment:
      - GIT_REPOS=/projects
      - MCP_PORT=3004
    command: npx -y @modelcontextprotocol/server-git --port 3004
    restart: unless-stopped

  mcp-whitelist:
    build: ./mcp-whitelist
    container_name: mcp-whitelist
    volumes:
      - ./whitelist.json:/data/whitelist.json
    ports:
      - "3005:3005"
    environment:
      - MCP_PORT=3005
    restart: unless-stopped

volumes:
  memory-data:
  context7-index:
```

## Advanced MCP Integration

### RAG MCP Server

```javascript
// rag-mcp-server.js
const { MCPServer } = require('@modelcontextprotocol/sdk');
const { ChromaClient } = require('chromadb');

class RAGMCPServer extends MCPServer {
  constructor() {
    super({
      name: 'rag-server',
      version: '1.0.0',
      description: 'Retrieval Augmented Generation'
    });
    
    this.client = new ChromaClient();
    this.collection = null;
    
    this.init();
    this.registerTools();
  }
  
  async init() {
    this.collection = await this.client.getOrCreateCollection({
      name: 'codebase',
      metadata: { description: 'Project codebase embeddings' }
    });
  }
  
  registerTools() {
    this.registerTool({
      name: 'search_code',
      description: 'Search codebase for relevant code',
      parameters: {
        type: 'object',
        properties: {
          query: { type: 'string' },
          n_results: { type: 'number', default: 5 }
        }
      },
      handler: async (params) => {
        const results = await this.collection.query({
          queryTexts: [params.query],
          nResults: params.n_results
        });
        
        return {
          results: results.documents[0].map((doc, i) => ({
            content: doc,
            score: results.distances[0][i],
            metadata: results.metadatas[0][i]
          }))
        };
      }
    });
    
    this.registerTool({
      name: 'index_file',
      description: 'Add file to codebase index',
      parameters: {
        type: 'object',
        properties: {
          file_path: { type: 'string' },
          content: { type: 'string' }
        }
      },
      handler: async (params) => {
        await this.collection.add({
          documents: [params.content],
          metadatas: [{ file_path: params.file_path }],
          ids: [params.file_path]
        });
        
        return { success: true, indexed: params.file_path };
      }
    });
  }
}

const server = new RAGMCPServer();
server.start();
```

## Best Practices

1. **Stateless When Possible**: MCP servers should be stateless or use external storage
2. **Error Handling**: Always handle errors gracefully
3. **Logging**: Log all requests for debugging
4. **Rate Limiting**: Implement rate limits for expensive operations
5. **Caching**: Cache frequently accessed data
6. **Security**: Validate all inputs, use whitelists
7. **Documentation**: Document all tools and resources
8. **Testing**: Test MCP servers independently

## Monitoring MCP Servers

```python
# mcp-monitor.py
import requests
import time
from datetime import datetime

class MCPMonitor:
    def __init__(self, servers):
        self.servers = servers
        self.stats = {name: {'up': 0, 'down': 0} for name in servers}
    
    def check_health(self, name, url):
        try:
            response = requests.get(f"{url}/health", timeout=2)
            if response.status_code == 200:
                self.stats[name]['up'] += 1
                return True
        except:
            pass
        
        self.stats[name]['down'] += 1
        return False
    
    def monitor(self):
        while True:
            print(f"\n--- MCP Server Health Check - {datetime.now()} ---")
            for name, url in self.servers.items():
                status = "UP" if self.check_health(name, url) else "DOWN"
                uptime = self.calculate_uptime(name)
                print(f"{name}: {status} (Uptime: {uptime:.2f}%)")
            
            time.sleep(30)
    
    def calculate_uptime(self, name):
        total = self.stats[name]['up'] + self.stats[name]['down']
        if total == 0:
            return 100.0
        return (self.stats[name]['up'] / total) * 100

# Usage
monitor = MCPMonitor({
    'memory': 'http://localhost:3000',
    'sequential': 'http://localhost:3001',
    'context7': 'http://localhost:3002',
    'filesystem': 'http://localhost:3003',
    'git': 'http://localhost:3004'
})

monitor.monitor()
```

## Integration Examples

### VS Code Extension Integration

```typescript
// MCP client for VS Code
import * as vscode from 'vscode';
import axios from 'axios';

class MCPClient {
    private servers: Map<string, string>;
    
    constructor() {
        this.servers = new Map([
            ['memory', 'http://localhost:3000'],
            ['sequential', 'http://localhost:3001'],
            ['filesystem', 'http://localhost:3003']
        ]);
    }
    
    async callTool(serverName: string, toolName: string, params: any): Promise<any> {
        const serverUrl = this.servers.get(serverName);
        if (!serverUrl) {
            throw new Error(`Server ${serverName} not found`);
        }
        
        const response = await axios.post(`${serverUrl}/tools/${toolName}`, params);
        return response.data;
    }
    
    async searchMemory(query: string): Promise<any> {
        return this.callTool('memory', 'search', { query });
    }
    
    async storeMemory(key: string, value: any): Promise<void> {
        await this.callTool('memory', 'store', { key, value });
    }
}

// Usage in extension
export function activate(context: vscode.ExtensionContext) {
    const mcpClient = new MCPClient();
    
    const command = vscode.commands.registerCommand('extension.searchCode', async () => {
        const query = await vscode.window.showInputBox({ prompt: 'Search query' });
        if (query) {
            const results = await mcpClient.searchMemory(query);
            // Display results
        }
    });
    
    context.subscriptions.push(command);
}
```

## Conclusion

MCP servers are essential for creating stateful, context-aware AI coding assistants. By integrating memory, sequential thinking, filesystem access, and custom tools, you can significantly enhance the capabilities of local LLM agents.

**Recommended MCP Stack**:
- Memory (essential)
- Sequential Thinking (highly recommended)
- Filesystem (essential)
- Git (recommended)
- Context7 (recommended)
- Custom whitelist manager (for security)
- RAG server (for large codebases)

## Next Steps

- [Security & Whitelisting](./security.md)
- [Hardware Requirements](./hardware.md)
- [Containerization Guide](./containers.md)
