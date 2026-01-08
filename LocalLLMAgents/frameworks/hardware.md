# Hardware Requirements and Comparison

## Overview

This guide provides detailed hardware requirements for running LLM agents locally, with specific focus on the RTX 4070 Windows machine and M2 Mac configurations mentioned in the research requirements.

## Reference Hardware

### Windows RTX 4070 Machine
- **GPU**: NVIDIA RTX 4070 (12GB VRAM)
- **Capabilities**: Excellent for 13B-33B models
- **Performance**: 60-150ms inference for most tasks
- **Recommended For**: Primary development workstation

### M2 Mac
- **Chip**: Apple M2 with unified memory
- **Configurations**: 16GB, 24GB, 32GB, or 64GB options
- **Capabilities**: Great for 7B-13B models, acceptable for 30B with 64GB
- **Performance**: 80-200ms inference for most tasks
- **Recommended For**: Portable/secondary workstation, server hosting

## Hardware Comparison Matrix

| Component | Minimum | Recommended (4070) | High-End | M2 Mac Equivalent |
|-----------|---------|-------------------|----------|-------------------|
| GPU | GTX 1660 Ti (6GB) | RTX 4070 (12GB) | RTX 4090 (24GB) | M2 (16-64GB unified) |
| RAM | 16GB | 32-64GB | 128GB | 16-64GB unified |
| Storage | 256GB SSD | 1TB NVMe | 2TB+ NVMe | 512GB-2TB SSD |
| CPU | 4-core | 8-core i7/Ryzen 7 | 16-core i9/Ryzen 9 | M2 (8-core) |
| Network | 100Mbps | 1Gbps | 10Gbps | 1Gbps WiFi 6 |

## Model Size vs Hardware Requirements

### 7B Parameter Models

**Examples**: CodeLlama 7B, Mistral 7B, Gemma 7B

| Hardware | VRAM Used | RAM Used | Inference Speed | Quality |
|----------|-----------|----------|-----------------|---------|
| RTX 4070 | 4-5GB | 8GB | 50-80ms | ⭐⭐⭐⭐⭐ |
| M2 16GB | 4-5GB | 4-5GB | 80-120ms | ⭐⭐⭐⭐⭐ |
| M2 32GB+ | 4-5GB | 4-5GB | 60-100ms | ⭐⭐⭐⭐⭐ |

**Best For**: Fast code completion, inline suggestions

### 13B Parameter Models

**Examples**: CodeLlama 13B, DeepSeek Coder 13B

| Hardware | VRAM Used | RAM Used | Inference Speed | Quality |
|----------|-----------|----------|-----------------|---------|
| RTX 4070 | 7-9GB | 12GB | 80-120ms | ⭐⭐⭐⭐⭐ |
| M2 16GB | 7-9GB | 7-9GB | 150-220ms | ⭐⭐⭐⭐ |
| M2 32GB+ | 7-9GB | 7-9GB | 100-180ms | ⭐⭐⭐⭐⭐ |

**Best For**: General coding assistance, code review

### 33-34B Parameter Models

**Examples**: DeepSeek Coder 33B, CodeLlama 34B

| Hardware | VRAM Used | RAM Used | Inference Speed | Quality |
|----------|-----------|----------|-----------------|---------|
| RTX 4070 Q4 | 11-12GB | 16GB | 150-250ms | ⭐⭐⭐⭐⭐ |
| M2 64GB Q4 | 11-12GB | 18-20GB | 400-700ms | ⭐⭐⭐⭐ |

**Best For**: Complex reasoning, architecture design

### 70B+ Parameter Models

**Examples**: LLaMA 3 70B, Qwen 72B

| Hardware | VRAM Used | RAM Used | Inference Speed | Quality |
|----------|-----------|----------|-----------------|---------|
| RTX 4090 Q4 | 20-24GB | 32GB | 500-800ms | ⭐⭐⭐⭐⭐ |
| Dual 4070 Q4 | 20-24GB | 32GB | 600-1000ms | ⭐⭐⭐⭐⭐ |
| M2 96GB Q4 | 22-26GB | 40-50GB | 1000-1500ms | ⭐⭐⭐⭐ |

**Best For**: Maximum quality when local is absolutely required

## RTX 4070 Detailed Analysis

### Specifications
- **CUDA Cores**: 5888
- **Tensor Cores**: 184 (4th gen)
- **VRAM**: 12GB GDDR6X
- **Memory Bandwidth**: 504 GB/s
- **TDP**: 200W
- **Price**: ~$600-700

### Performance Profile

```
Model Size     | Load Time | First Token | Tokens/sec | Concurrent
---------------|-----------|-------------|------------|------------
7B Q4         | 3s        | 50ms        | 150        | 4-5
13B Q4        | 6s        | 80ms        | 110        | 2-3
33B Q4        | 12s       | 150ms       | 60         | 1-2
```

### Optimal Configurations

**For Speed (Code Completion)**:
- Model: Stable-Code 3B or DeepSeek 6.7B
- Latency: 30-70ms
- Throughput: 150-200 tok/s
- Concurrent Users: 4-5

**For Quality (Code Review)**:
- Model: DeepSeek Coder 33B Q4_K_M
- Latency: 150-200ms
- Throughput: 55-65 tok/s
- Concurrent Users: 1-2

**For Balance**:
- Model: CodeLlama 13B or DeepSeek 13B
- Latency: 80-120ms
- Throughput: 90-110 tok/s
- Concurrent Users: 2-3

### Power Consumption

- **Idle**: 15-20W
- **Light Load** (7B model): 80-120W
- **Heavy Load** (33B model): 180-200W
- **24/7 Operation Cost**: ~$10-15/month (at $0.12/kWh)

## M2 Mac Detailed Analysis

### Specifications (Base M2)
- **CPU Cores**: 8 (4 performance, 4 efficiency)
- **GPU Cores**: 8-10
- **Neural Engine**: 16-core
- **Unified Memory**: 16GB / 24GB / 32GB / 64GB options
- **Memory Bandwidth**: 100 GB/s
- **TDP**: 15-30W
- **Price**: $1200-2500 (depending on config)

### Performance Profile

```
Model Size     | Load Time | First Token | Tokens/sec | Memory Used
---------------|-----------|-------------|------------|-------------
7B Q4 (16GB)  | 4s        | 80ms        | 100        | 6-8GB
13B Q4 (32GB) | 8s        | 150ms       | 70         | 12-15GB
33B Q4 (64GB) | 20s       | 400ms       | 25         | 25-30GB
```

### Configuration Recommendations

**16GB M2 Mac**:
- Best Models: 7B parameter models
- Avoid: >13B models
- Use For: Portable development, light coding assistance

**32GB M2 Mac**:
- Best Models: 13B parameter models
- Can Handle: 7B-13B comfortably
- Use For: Primary development if no RTX 4070

**64GB M2 Mac**:
- Best Models: 13B-30B parameter models
- Can Handle: Quantized 70B (slow)
- Use For: Server hosting, maximum flexibility

### Power Efficiency

- **Idle**: 5-10W
- **Light Load** (7B): 15-25W
- **Heavy Load** (13B): 25-35W
- **24/7 Cost**: ~$3-5/month (at $0.12/kWh)

## Multi-GPU Configurations

### Dual RTX 4070 Setup

**Capabilities**:
- Run two models simultaneously
- Load balance requests
- Total VRAM: 24GB
- Can run 70B models quantized

**Setup**:
```bash
# Configure Ollama for multi-GPU
export CUDA_VISIBLE_DEVICES=0,1
export OLLAMA_NUM_GPU=2

# Load model across GPUs
ollama run llama3:70b-q4
```

**Use Cases**:
- One GPU for fast completion (7B-13B)
- Other GPU for quality tasks (33B-70B)
- Redundancy/failover

## Hybrid Hardware Strategies

### Windows RTX 4070 + M2 Mac

**Strategy 1: Specialized Roles**
- **Windows RTX 4070**: Primary development, heavy models (33B)
- **M2 Mac**: Server hosting, always-on, light models (13B)

**Strategy 2: Failover**
- **Primary**: Windows RTX 4070 (33B model)
- **Backup**: M2 Mac (13B model)
- Automatic failover on primary failure

**Strategy 3: Task Distribution**
- **Windows**: Code generation, refactoring (DeepSeek 33B)
- **Mac**: Code completion, docs (CodeLlama 13B)

## Storage Requirements

### Model Storage

| Model Collection | Size | Recommended Storage |
|-----------------|------|---------------------|
| Minimal (2-3 models) | 50GB | 256GB SSD |
| Standard (5-7 models) | 150GB | 512GB SSD |
| Complete (10+ models) | 300GB | 1TB NVMe |
| Enterprise (many versions) | 500GB+ | 2TB NVMe |

### Additional Storage Needs

- **MCP Memory Database**: 1-5GB
- **Vector Embeddings**: 10-50GB (for large codebases)
- **Logs**: 5-10GB
- **Cache**: 10-20GB

**Total Recommended**: 1TB minimum, 2TB preferred

## Network Requirements

### Local-Only
- **Requirement**: None
- **Benefit**: Maximum privacy

### Networked (LAN)
- **Minimum**: 100Mbps
- **Recommended**: 1Gbps
- **Ideal**: 10Gbps or WiFi 6

### Hybrid (Cloud Fallback)
- **Minimum**: 10Mbps
- **Recommended**: 50Mbps+
- **Latency**: <50ms to cloud API

## Upgrade Paths

### From GTX 1660 Ti
1. **RTX 4070**: Best balance (~$650)
2. **RTX 4080**: More VRAM but expensive (~$1100)
3. **RTX 4090**: Maximum power (~$1600)

### From M1/M2 16GB
1. **Upgrade to 32GB**: Doubles model capacity
2. **Upgrade to 64GB**: Enables 30B+ models
3. **Add RTX 4070 PC**: Best of both worlds

### From RTX 3060
1. **RTX 4070**: Significant upgrade worth it
2. **Wait for 5000 series**: If budget limited

## Cost Analysis

### Initial Investment

| Component | Cost |
|-----------|------|
| RTX 4070 GPU | $650 |
| 32GB RAM | $80 |
| 1TB NVMe SSD | $100 |
| **Total** | **~$830** |

### M2 Mac (New Purchase)

| Model | Cost |
|-------|------|
| M2 Mac Mini 16GB | $799 |
| M2 Mac Mini 32GB | $1299 |
| M2 MacBook Pro 32GB | $2499 |

### Operating Costs (Monthly)

| Setup | Power Cost | Cloud API | Total |
|-------|------------|-----------|-------|
| Local Only (4070) | $15 | $0 | $15 |
| Local Only (Mac) | $5 | $0 | $5 |
| Hybrid (4070) | $15 | $20 | $35 |
| Cloud Only | $0 | $100-200 | $100-200 |

**Breakeven**: ~6-12 months for local vs cloud

## Benchmarks

### Real-World Performance (RTX 4070)

**Unity C# Development**:
- Generate MonoBehaviour: 2-3 seconds
- Explain complex code: 3-5 seconds
- Refactor class: 5-8 seconds

**React TypeScript Development**:
- Component generation: 2-4 seconds
- Hook creation: 1-2 seconds
- Type definitions: 1-3 seconds

**C# Backend Development**:
- API endpoint: 3-5 seconds
- Repository pattern: 4-6 seconds
- Async/await conversion: 5-8 seconds

## Conclusion

The RTX 4070 with 12GB VRAM provides excellent value for running local LLM agents, comfortably handling 13B-33B models. The M2 Mac excels at power efficiency and works well as a secondary device or server. For maximum flexibility, a hybrid Windows RTX 4070 + M2 Mac setup provides the best of both worlds.

**Recommended Purchase Priority**:
1. RTX 4070 Windows machine (if starting fresh)
2. Upgrade existing Mac to 32GB+ (if already have Mac)
3. 1TB+ NVMe storage
4. 32-64GB RAM for host system
5. Second RTX 4070 (only if needed for dual-GPU)

## Next Steps

- [Containerization Guide](./containers.md)
- [Resource Comparison Matrix](../comparisons/resources-matrix.md)
- [Deployment Models](../deployment/local-only.md)
