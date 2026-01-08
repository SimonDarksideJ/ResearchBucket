# Single Project Deployment

## Overview

Single project deployment creates isolated AI environments for each project, ensuring that context, memory, and model interactions remain project-specific. This approach is ideal for working on multiple projects with different codebases, frameworks, or security requirements.

## Key Characteristics

- **Isolation**: ⭐⭐⭐⭐⭐ (Complete project separation)
- **Context Accuracy**: ⭐⭐⭐⭐⭐ (Project-specific knowledge)
- **Resource Usage**: ⭐⭐⭐ (Higher overhead per project)
- **Complexity**: ⭐⭐⭐⭐ (More setup and management)
- **Flexibility**: ⭐⭐⭐⭐⭐ (Customize per project)
- **Security**: ⭐⭐⭐⭐⭐ (No cross-project contamination)

## When to Use

### Best For:
- Multiple concurrent projects
- Different technology stacks (Unity, React, C#, etc.)
- Client work requiring separation
- Different security levels per project
- Project-specific fine-tuned models
- Team projects with different members

### Not Ideal For:
- Single project focus
- Limited hardware resources
- Frequently switching between many projects
- Simple prototypes or experiments

## Architecture

```
┌──────────────────────────────────────────────────────┐
│              Windows RTX 4070 Machine                │
│                                                      │
│  ┌───────────────┐  ┌───────────────┐  ┌──────────┐│
│  │ Project A     │  │ Project B     │  │Project C ││
│  │ Container     │  │ Container     │  │Container ││
│  │               │  │               │  │          ││
│  │ ┌───────────┐ │  │ ┌───────────┐ │  │┌────────┐││
│  │ │  Ollama   │ │  │ │  Ollama   │ │  ││ Ollama │││
│  │ │  Model    │ │  │ │  Model    │ │  ││ Model  │││
│  │ │ Unity     │ │  │ │  React    │ │  ││ C#     │││
│  │ └───────────┘ │  │ └───────────┘ │  │└────────┘││
│  │               │  │               │  │          ││
│  │ ┌───────────┐ │  │ ┌───────────┐ │  │┌────────┐││
│  │ │MCP Memory │ │  │ │MCP Memory │ │  ││Memory  │││
│  │ │(Project A)│ │  │ │(Project B)│ │  ││(Proj C)│││
│  │ └───────────┘ │  │ └───────────┘ │  │└────────┘││
│  │               │  │               │  │          ││
│  │ ┌───────────┐ │  │ ┌───────────┐ │  │┌────────┐││
│  │ │Project    │ │  │ │Project    │ │  ││Project │││
│  │ │Files      │ │  │ │Files      │ │  ││Files   │││
│  │ │(Read Only)│ │  │ │(Read Only)│ │  ││(R/O)   │││
│  │ └───────────┘ │  │ └───────────┘ │  │└────────┘││
│  └───────────────┘  └───────────────┘  └──────────┘│
│                                                      │
│  Shared: Base Models, GPU, Host Filesystem          │
└──────────────────────────────────────────────────────┘
```

## Hardware Requirements

### Per-Project Resource Allocation

#### Minimum per Project
- **VRAM**: 4-6GB (shared via model)
- **RAM**: 4-8GB container limit
- **Storage**: 10-50GB (code + memory + cache)
- **Model**: Shared 7B-13B model

#### Recommended (RTX 4070)
- **VRAM**: Shared 12GB (3-4 projects with same model)
- **RAM**: 8-16GB per container (32-64GB host total)
- **Storage**: 50-100GB per project
- **Models**: Shared 13B-33B, or project-specific 7B-13B

### Example Configurations

#### Configuration 1: Shared Model (Efficient)
```yaml
Total Projects: 4
Shared Model: DeepSeek Coder 33B (11GB VRAM)
Per Project RAM: 8GB
Total Host RAM: 48GB (32GB + 16GB overhead)
Total Storage: 300GB
```

#### Configuration 2: Project-Specific Models
```yaml
Total Projects: 3
Project Models: Each has 13B model (8GB VRAM each, time-shared)
Per Project RAM: 12GB
Total Host RAM: 64GB
Total Storage: 400GB
```

## Installation Guide

### Step 1: Install Container Runtime

#### Windows - Docker Desktop

```powershell
# Install Docker Desktop
winget install Docker.DockerDesktop

# Ensure WSL2 is enabled
wsl --install

# Enable GPU support
# Edit ~/.wslconfig
[wsl2]
memory=32GB
processors=8
```

#### Alternative - Podman

```bash
# Install Podman
winget install RedHat.Podman

# Configure for NVIDIA GPU
podman machine init --cpus 8 --memory 32768
```

### Step 2: Create Project Template

```bash
# Create project structure template
mkdir -p ~/ai-projects/template/{config,scripts,data}

# Base docker-compose template
cat > ~/ai-projects/template/docker-compose.yml << 'EOF'
version: '3.8'

services:
  ollama:
    image: ollama/ollama:latest
    container_name: ${PROJECT_NAME}-ollama
    volumes:
      - shared-models:/root/.ollama/models:ro
      - project-cache:/root/.ollama/cache
      - ../project-code:/workspace:ro
    deploy:
      resources:
        reservations:
          devices:
            - driver: nvidia
              count: 1
              capabilities: [gpu]
        limits:
          memory: 12G
    environment:
      - OLLAMA_MODEL=${PROJECT_MODEL:-deepseek-coder:13b}
      - OLLAMA_CONTEXT_SIZE=${CONTEXT_SIZE:-8192}
    networks:
      - project-network

  mcp-memory:
    image: node:20-alpine
    container_name: ${PROJECT_NAME}-memory
    volumes:
      - memory-data:/data
    deploy:
      resources:
        limits:
          memory: 1G
    environment:
      - MCP_STORAGE=/data/memory.db
    command: npx -y @modelcontextprotocol/server-memory --storage /data/memory.db
    networks:
      - project-network

  mcp-filesystem:
    image: node:20-alpine
    container_name: ${PROJECT_NAME}-filesystem
    volumes:
      - ../project-code:/workspace:ro
    environment:
      - ALLOWED_DIRECTORIES=/workspace
    command: npx -y @modelcontextprotocol/server-filesystem
    networks:
      - project-network

  mcp-sequential:
    image: node:20-alpine
    container_name: ${PROJECT_NAME}-sequential
    command: npx -y @modelcontextprotocol/server-sequential-thinking
    networks:
      - project-network

  mcp-git:
    image: node:20-alpine
    container_name: ${PROJECT_NAME}-git
    volumes:
      - ../project-code:/workspace:ro
    environment:
      - GIT_REPOS=/workspace
    command: npx -y @modelcontextprotocol/server-git
    networks:
      - project-network

networks:
  project-network:
    driver: bridge
    internal: true  # No external access

volumes:
  shared-models:
    external: true
    name: ai-shared-models
  project-cache:
  memory-data:
EOF
```

### Step 3: Create Project Management Scripts

```bash
# create-project.sh
cat > ~/ai-projects/scripts/create-project.sh << 'EOF'
#!/bin/bash

PROJECT_NAME=$1
PROJECT_PATH=$2
MODEL=${3:-"deepseek-coder:13b"}

if [ -z "$PROJECT_NAME" ] || [ -z "$PROJECT_PATH" ]; then
    echo "Usage: create-project.sh <project-name> <project-path> [model]"
    exit 1
fi

# Create project AI directory
mkdir -p ~/ai-projects/$PROJECT_NAME

# Copy template
cp -r ~/ai-projects/template/* ~/ai-projects/$PROJECT_NAME/

# Create .env file
cat > ~/ai-projects/$PROJECT_NAME/.env << ENVEOF
PROJECT_NAME=$PROJECT_NAME
PROJECT_MODEL=$MODEL
CONTEXT_SIZE=8192
ENVEOF

# Create symbolic link to actual project
ln -s "$PROJECT_PATH" ~/ai-projects/$PROJECT_NAME/project-code

echo "Project $PROJECT_NAME created!"
echo "Start with: cd ~/ai-projects/$PROJECT_NAME && docker-compose up -d"
EOF

chmod +x ~/ai-projects/scripts/create-project.sh
```

### Step 4: Create Shared Model Volume

```bash
# Create shared volume for models
docker volume create ai-shared-models

# Pull models into shared volume
docker run -v ai-shared-models:/root/.ollama \
  ollama/ollama:latest \
  sh -c "ollama pull deepseek-coder:13b && \
         ollama pull codellama:13b && \
         ollama pull mixtral:8x7b"
```

### Step 5: Create New Projects

```bash
# Example: Unity project
./scripts/create-project.sh "unity-game" \
  "/path/to/UnityProject" \
  "deepseek-coder:13b"

# Example: React project
./scripts/create-project.sh "react-app" \
  "/path/to/ReactApp" \
  "codellama:13b"

# Example: C# backend
./scripts/create-project.sh "csharp-api" \
  "/path/to/CSharpAPI" \
  "deepseek-coder:33b"
```

## Project Configuration

### Project-Specific Model Selection

```yaml
# unity-game/.env
PROJECT_NAME=unity-game
PROJECT_MODEL=deepseek-coder:13b
CONTEXT_SIZE=8192
TEMPERATURE=0.7
SPECIALIZED_FOR=csharp,unity

# react-app/.env
PROJECT_NAME=react-app
PROJECT_MODEL=codellama:13b
CONTEXT_SIZE=16384
TEMPERATURE=0.6
SPECIALIZED_FOR=typescript,react

# csharp-api/.env
PROJECT_NAME=csharp-api
PROJECT_MODEL=deepseek-coder:33b
CONTEXT_SIZE=16384
TEMPERATURE=0.5
SPECIALIZED_FOR=csharp,dotnet,async
```

### VS Code Workspace Settings

```json
// .vscode/settings.json per project
{
  "continue.mcp.enabled": true,
  "continue.modelServer": "http://localhost:11434",
  "continue.models": [
    {
      "title": "Project-Specific Model",
      "provider": "ollama",
      "model": "deepseek-coder:13b",
      "contextLength": 8192
    }
  ],
  "continue.mcp.servers": [
    "http://localhost:3000",  // memory
    "http://localhost:3001",  // sequential
    "http://localhost:3002",  // filesystem
    "http://localhost:3003"   // git
  ]
}
```

## Advanced Features

### Project-Specific Fine-Tuning

```python
# fine-tune-for-project.py
from transformers import AutoTokenizer, AutoModelForCausalLM, Trainer, TrainingArguments
import torch

def fine_tune_for_project(base_model, project_code_path, output_path):
    """
    Fine-tune model on project-specific code
    """
    # Load base model
    tokenizer = AutoTokenizer.from_pretrained(base_model)
    model = AutoModelForCausalLM.from_pretrained(
        base_model,
        torch_dtype=torch.float16,
        device_map="auto"
    )
    
    # Load project code
    project_code = load_project_files(project_code_path)
    
    # Prepare dataset
    dataset = tokenize_code(project_code, tokenizer)
    
    # Training arguments
    training_args = TrainingArguments(
        output_dir=output_path,
        num_train_epochs=3,
        per_device_train_batch_size=4,
        save_steps=500,
        logging_steps=100,
        learning_rate=2e-5,
        warmup_steps=100,
        fp16=True
    )
    
    # Train
    trainer = Trainer(
        model=model,
        args=training_args,
        train_dataset=dataset
    )
    
    trainer.train()
    trainer.save_model(output_path)
    
    print(f"Model fine-tuned and saved to {output_path}")

# Usage
fine_tune_for_project(
    base_model="deepseek-coder-13b",
    project_code_path="/path/to/unity-game",
    output_path="./models/unity-game-specialized"
)
```

### Project Knowledge Base (RAG)

```python
# project-rag.py
from langchain.vectorstores import Chroma
from langchain.embeddings import OllamaEmbeddings
from langchain.text_splitter import RecursiveCharacterTextSplitter
import os

class ProjectKnowledgeBase:
    def __init__(self, project_name, project_path):
        self.project_name = project_name
        self.project_path = project_path
        self.embeddings = OllamaEmbeddings(model="nomic-embed-text")
        self.vectorstore = None
        
    def index_project(self):
        """Index all project files"""
        documents = []
        
        # Walk through project
        for root, dirs, files in os.walk(self.project_path):
            # Skip node_modules, bin, obj, etc.
            dirs[:] = [d for d in dirs if d not in ['node_modules', 'bin', 'obj', '.git']]
            
            for file in files:
                if self.is_code_file(file):
                    file_path = os.path.join(root, file)
                    with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
                        content = f.read()
                        documents.append({
                            'content': content,
                            'metadata': {
                                'source': file_path,
                                'filename': file
                            }
                        })
        
        # Split documents
        text_splitter = RecursiveCharacterTextSplitter(
            chunk_size=1000,
            chunk_overlap=200
        )
        
        splits = text_splitter.split_documents(documents)
        
        # Create vector store
        self.vectorstore = Chroma.from_documents(
            documents=splits,
            embedding=self.embeddings,
            persist_directory=f"./data/{self.project_name}/vectorstore"
        )
        
        print(f"Indexed {len(documents)} files into {len(splits)} chunks")
    
    def search(self, query, k=5):
        """Search project knowledge base"""
        if not self.vectorstore:
            raise ValueError("Project not indexed. Call index_project() first.")
        
        results = self.vectorstore.similarity_search(query, k=k)
        return results
    
    @staticmethod
    def is_code_file(filename):
        code_extensions = ['.cs', '.ts', '.tsx', '.js', '.jsx', '.py', 
                          '.go', '.rs', '.java', '.cpp', '.h', '.md']
        return any(filename.endswith(ext) for ext in code_extensions)

# Usage
kb = ProjectKnowledgeBase("unity-game", "/path/to/unity-game")
kb.index_project()
results = kb.search("player movement controller")
```

## Resource Management

### Dynamic Resource Allocation

```python
# resource-manager.py
import docker
import psutil

class ProjectResourceManager:
    def __init__(self):
        self.client = docker.from_env()
        self.projects = {}
        
    def get_active_projects(self):
        """Get list of running project containers"""
        containers = self.client.containers.list()
        projects = {}
        
        for container in containers:
            if 'project' in container.name:
                project_name = container.name.split('-')[0]
                if project_name not in projects:
                    projects[project_name] = []
                projects[project_name].append(container)
        
        return projects
    
    def adjust_resources(self, project_name, memory_limit=None, cpu_limit=None):
        """Dynamically adjust project resources"""
        projects = self.get_active_projects()
        
        if project_name in projects:
            for container in projects[project_name]:
                update_dict = {}
                if memory_limit:
                    update_dict['mem_limit'] = memory_limit
                if cpu_limit:
                    update_dict['cpu_quota'] = cpu_limit
                
                container.update(**update_dict)
                print(f"Updated {container.name}: {update_dict}")
    
    def pause_inactive_projects(self, idle_minutes=30):
        """Pause projects that haven't been used recently"""
        projects = self.get_active_projects()
        
        for project_name, containers in projects.items():
            # Check last activity (simplified - would check logs/memory access)
            idle = self.check_idle_time(project_name)
            
            if idle > idle_minutes:
                print(f"Pausing idle project: {project_name}")
                for container in containers:
                    container.pause()
    
    def resume_project(self, project_name):
        """Resume a paused project"""
        projects = self.get_active_projects()
        
        if project_name in projects:
            for container in projects[project_name]:
                if container.status == 'paused':
                    container.unpause()
                    print(f"Resumed {container.name}")

# Usage
manager = ProjectResourceManager()

# Adjust resources for active project
manager.adjust_resources("unity-game", memory_limit="16G")

# Pause inactive projects to save resources
manager.pause_inactive_projects(idle_minutes=60)

# Resume when needed
manager.resume_project("unity-game")
```

### Smart Model Loading

```python
# model-scheduler.py
class ModelScheduler:
    def __init__(self, gpu_memory_gb=12):
        self.gpu_memory = gpu_memory_gb * 1024  # MB
        self.loaded_models = {}
        self.model_sizes = {
            'deepseek-coder:6.7b': 4500,
            'deepseek-coder:13b': 8500,
            'deepseek-coder:33b': 11800,
            'codellama:13b': 8200,
            'mixtral:8x7b': 11200
        }
    
    def can_load_model(self, model_name):
        """Check if model can fit in available GPU memory"""
        current_usage = sum(self.loaded_models.values())
        required = self.model_sizes.get(model_name, 8000)
        return (current_usage + required) <= self.gpu_memory
    
    def load_model_for_project(self, project_name, model_name):
        """Load model, unloading others if necessary"""
        if not self.can_load_model(model_name):
            # Unload least recently used model
            self.unload_lru_model()
        
        # Load model
        self.loaded_models[project_name] = self.model_sizes[model_name]
        print(f"Loaded {model_name} for {project_name}")
        print(f"GPU Usage: {sum(self.loaded_models.values())}/{self.gpu_memory}MB")
    
    def unload_lru_model(self):
        """Unload least recently used model"""
        if self.loaded_models:
            lru_project = list(self.loaded_models.keys())[0]
            del self.loaded_models[lru_project]
            print(f"Unloaded model for {lru_project}")

# Usage
scheduler = ModelScheduler(gpu_memory_gb=12)

# Load models for different projects
scheduler.load_model_for_project("unity-game", "deepseek-coder:13b")
scheduler.load_model_for_project("react-app", "codellama:13b")  # May unload unity-game
```

## Project Switching

### Quick Switch Script

```bash
#!/bin/bash
# switch-project.sh

PROJECT=$1

if [ -z "$PROJECT" ]; then
    echo "Active projects:"
    docker ps --filter "name=project" --format "table {{.Names}}\t{{.Status}}"
    exit 0
fi

# Pause all other projects
for container in $(docker ps -q --filter "name=project"); do
    docker pause $container 2>/dev/null
done

# Resume target project
docker unpause ${PROJECT}-ollama 2>/dev/null
docker unpause ${PROJECT}-memory 2>/dev/null
docker unpause ${PROJECT}-filesystem 2>/dev/null
docker unpause ${PROJECT}-sequential 2>/dev/null
docker unpause ${PROJECT}-git 2>/dev/null

echo "Switched to project: $PROJECT"
echo "Active containers:"
docker ps --filter "name=$PROJECT"
```

## Monitoring Per-Project

### Project Dashboard

```python
# project-dashboard.py
from flask import Flask, render_template, jsonify
import docker
import psutil

app = Flask(__name__)
client = docker.from_env()

@app.route('/')
def dashboard():
    return render_template('dashboard.html')

@app.route('/api/projects')
def get_projects():
    projects = {}
    containers = client.containers.list(all=True)
    
    for container in containers:
        if '-ollama' in container.name:
            project_name = container.name.replace('-ollama', '')
            stats = container.stats(stream=False)
            
            projects[project_name] = {
                'status': container.status,
                'cpu_usage': calculate_cpu_percent(stats),
                'memory_usage': stats['memory_stats']['usage'] / 1e6,  # MB
                'memory_limit': stats['memory_stats']['limit'] / 1e6,
                'containers': count_project_containers(project_name)
            }
    
    return jsonify(projects)

def calculate_cpu_percent(stats):
    cpu_delta = stats['cpu_stats']['cpu_usage']['total_usage'] - \
                stats['precpu_stats']['cpu_usage']['total_usage']
    system_delta = stats['cpu_stats']['system_cpu_usage'] - \
                   stats['precpu_stats']['system_cpu_usage']
    return (cpu_delta / system_delta) * 100.0 if system_delta > 0 else 0.0

def count_project_containers(project_name):
    containers = client.containers.list(filters={'name': project_name})
    return len(containers)

if __name__ == '__main__':
    app.run(debug=True, port=5000)
```

## Best Practices

### 1. Project Organization
```
~/ai-projects/
├── template/           # Base template
├── scripts/           # Management scripts
├── unity-game/        # Project 1
│   ├── docker-compose.yml
│   ├── .env
│   ├── data/
│   │   ├── memory.db
│   │   └── vectorstore/
│   └── project-code -> /actual/project/path
├── react-app/         # Project 2
└── csharp-api/        # Project 3
```

### 2. Resource Limits
- Set memory limits per container
- Use CPU quotas for fair sharing
- Monitor and adjust based on usage
- Pause inactive projects automatically

### 3. Backup Strategy
- Backup memory databases weekly
- Version control docker-compose files
- Export project configurations
- Document project-specific customizations

### 4. Security
- Read-only project mounts
- Isolated networks per project
- No cross-project communication
- Separate API keys per project (if hybrid)

## Advantages

1. **Perfect Isolation**: No context bleeding between projects
2. **Customization**: Each project optimized individually
3. **Security**: Clear separation of concerns
4. **Scalability**: Add projects without affecting others
5. **Reproducibility**: Easy to recreate environments
6. **Team Friendly**: Share project configs with team

## Disadvantages

1. **Resource Intensive**: Higher overhead per project
2. **Complex Management**: More containers to manage
3. **Storage Usage**: Duplicate caches and configs
4. **Context Switching**: Need to manage which project is active
5. **Initial Setup**: More work upfront
6. **GPU Sharing**: Need smart scheduling for model loading

## Conclusion

Single project deployment is ideal when working on multiple distinct projects that benefit from isolation and customization. With proper resource management and automation, the overhead is manageable even on a single RTX 4070 machine.

**Recommended For**:
- Professional developers with multiple clients
- Teams working on different products
- Projects with different tech stacks
- Scenarios requiring strict isolation

## Next Steps

- [Hybrid Deployment](./hybrid.md) - Add cloud fallback
- [Networked Deployment](./networked.md) - Share across machines
- [MCP Servers](../frameworks/mcp-servers.md) - Enhance each project
- [Containerization Details](../frameworks/containers.md)
