# Unity/MonoGame/Godot Development with Local LLM Agents

## Overview

Game development with Unity, MonoGame, and Godot benefits significantly from local LLM agents due to proprietary code concerns and iterative development needs. This guide provides setup recommendations and best practices.

## Recommended Setup

### Hardware
- **Preferred**: Windows RTX 4070
- **Model**: DeepSeek Coder 13B or 33B (C# optimized)
- **Alternative**: CodeLlama 13B
- **Deployment**: Hybrid (local primary, cloud for architecture)

### Why This Setup?
- C# is well-represented in training data
- Fast iteration for MonoBehaviour/Component creation
- Privacy for game logic
- Local latency critical for rapid prototyping

## Unity Development

### Optimal Configuration

```yaml
primary_model: deepseek-coder:13b
context_size: 16384
temperature: 0.6  # Balance creativity and correctness

mcp_servers:
  - memory (remember Unity patterns)
  - sequential-thinking (complex systems)
  - filesystem (project access)
  - context7 (Unity documentation)

rag_enabled: true
rag_sources:
  - /path/to/unity-docs
  - /path/to/project/Scripts
```

### Common Tasks & Performance

| Task | Model | Expected Time | Quality |
|------|-------|---------------|---------|
| MonoBehaviour skeleton | 7B | 1-2s | ⭐⭐⭐⭐⭐ |
| Player controller | 13B | 3-5s | ⭐⭐⭐⭐⭐ |
| Enemy AI system | 33B | 8-12s | ⭐⭐⭐⭐ |
| Inventory system | 33B | 10-15s | ⭐⭐⭐⭐ |
| Save/Load system | 33B + Cloud | 20-30s | ⭐⭐⭐⭐⭐ |

### Unity-Specific Prompts

**Good Prompt**:
```
Create a Unity C# MonoBehaviour for a third-person player controller with:
- WASD movement using CharacterController
- Mouse-look camera rotation
- Jump with space bar (ground check required)
- Smooth movement with acceleration
- SerializeField for inspector tunables
```

**Output Quality**: ⭐⭐⭐⭐⭐ with DeepSeek 13B

### Project Structure Optimization

```csharp
// Index entire Scripts folder for RAG
CodebaseRAG rag = new("C:/UnityProjects/MyGame/Assets/Scripts");
rag.index_codebase();

// When creating new script, provide context
string context = rag.get_relevant_context("player movement");
string prompt = $@"
Context from existing codebase:
{context}

Create a player controller that follows the same patterns.
";
```

### Unity Documentation Integration

```json
{
  "mcp-context7": {
    "docs_paths": [
      "C:/Unity/Documentation",
      "C:/UnityProjects/MyGame/Docs"
    ],
    "index_apis": [
      "CharacterController",
      "Rigidbody",
      "Transform",
      "Input",
      "Physics"
    ]
  }
}
```

## MonoGame Development

### Optimal Configuration

```yaml
primary_model: codellama:13b  # Fast for framework patterns
fallback_model: deepseek-coder:33b  # Complex algorithms
temperature: 0.5  # More deterministic for game logic

rag_sources:
  - /path/to/monogame-docs
  - /path/to/project/Source
```

### MonoGame-Specific Examples

**Content Pipeline Extension**:
```
Create a MonoGame ContentPipeline processor for:
- Custom sprite sheet format (.ssheet)
- Parse JSON metadata
- Output SpriteSheet type
- Handle multiple texture sizes
```

**Game System**:
```
Create a MonoGame component system with:
- Entity base class
- Component pattern
- Update/Draw separation
- Sprite rendering
```

### Performance Considerations

| Task | Local 13B | Cloud GPT-4 |
|------|-----------|-------------|
| Simple component | 2-3s ⭐⭐⭐⭐⭐ | 5-8s ⭐⭐⭐⭐⭐ |
| Content pipeline | 5-7s ⭐⭐⭐⭐ | 8-12s ⭐⭐⭐⭐⭐ |
| Physics system | 15-20s ⭐⭐⭐ | 12-18s ⭐⭐⭐⭐⭐ |

## Godot Development

### Optimal Configuration

```yaml
primary_model: deepseek-coder:13b
temperature: 0.7  # GDScript more flexible

fine_tune_needed: true  # Godot less common in training data
```

### GDScript Challenges

**Challenge**: Less GDScript in training data than C#

**Solution 1 - Few-Shot Learning**:
```python
few_shot_examples = [
    {
        "input": "player movement",
        "output": '''extends CharacterBody2D

export var speed = 200
export var jump_force = 400

func _physics_process(delta):
    var input_vector = Vector2.ZERO
    input_vector.x = Input.get_action_strength("ui_right") - Input.get_action_strength("ui_left")
    input_vector = input_vector.normalized()
    
    velocity.x = input_vector.x * speed
    velocity.y += gravity * delta
    
    if Input.is_action_just_pressed("ui_up") and is_on_floor():
        velocity.y = -jump_force
    
    velocity = move_and_slide(velocity, Vector2.UP)
'''
    }
]
```

**Solution 2 - Hybrid with Documentation**:
```json
{
  "godot_setup": {
    "rag": {
      "godot_docs": "/path/to/godot-docs",
      "example_projects": "/path/to/godot-examples"
    },
    "fallback": "claude-3-opus-api"
  }
}
```

### Node System Generation

```
Create a Godot scene structure for a platformer player:
- CharacterBody2D (root)
  - CollisionShape2D
  - AnimatedSprite2D
  - Camera2D
- Include GDScript for each node
- Handle animations (idle, run, jump)
```

## Best Practices

### 1. Project-Specific Fine-Tuning

```python
# Fine-tune on your game's codebase
fine_tune_on_codebase(
    base_model="deepseek-coder-13b",
    codebase_path="C:/UnityProjects/MyGame/Assets/Scripts",
    output_path="./models/deepseek-mygame",
    epochs=3
)
```

### 2. Component Templates

```python
# Store successful patterns in memory
await mcp_memory.store({
    "type": "template",
    "framework": "unity",
    "category": "player_controller",
    "code": "...",
    "rating": 5
})

# Retrieve when needed
templates = await mcp_memory.search("template unity player_controller")
```

### 3. Iterative Refinement

```python
# Generate -> Test -> Refine loop
def iterative_generation(prompt, max_iterations=3):
    for i in range(max_iterations):
        code = model.generate(prompt)
        
        # Test in Unity
        errors = test_in_unity(code)
        
        if not errors:
            return code
        
        # Refine prompt with errors
        prompt = f"{prompt}\n\nFix these errors:\n{errors}"
    
    # Fallback to cloud for complex issues
    return cloud_model.generate(prompt)
```

### 4. Documentation Sync

```bash
# Update Unity docs weekly
./scripts/update-unity-docs.sh

# Re-index for RAG
python reindex_rag.py --source unity-docs --target vectorstore/unity
```

## Common Patterns

### Unity Singleton Pattern

**Prompt**:
```
Create a Unity C# singleton pattern with:
- Generic base class
- DontDestroyOnLoad
- Instance access
- Proper cleanup
```

**Expected Quality**: ⭐⭐⭐⭐⭐ (well-known pattern)

### Object Pooling

**Prompt**:
```
Create an object pooling system for Unity:
- Generic pool class
- Prewarm option
- Grow on demand
- Return to pool method
```

**Expected Quality**: ⭐⭐⭐⭐ (may need refinement)

### State Machine

**Prompt**:
```
Create a finite state machine for enemy AI:
- State base class
- StateMachine controller
- Idle, Patrol, Chase, Attack states
- Smooth transitions
```

**Expected Quality**: ⭐⭐⭐⭐ (complex, may need iteration)

## Performance Optimization

### Model Selection by Task

| Task Type | Recommended Model | Rationale |
|-----------|------------------|-----------|
| Simple scripts (<50 lines) | CodeLlama 7B | Fast, good enough |
| Standard gameplay (50-200 lines) | DeepSeek 13B | Best balance |
| Complex systems (200+ lines) | DeepSeek 33B | Better architecture |
| Novel algorithms | Cloud GPT-4 | Needs reasoning |

### Caching Strategy

```python
# Cache common patterns
cache = {}

def generate_with_cache(prompt_type, details):
    cache_key = f"{prompt_type}_{hash(details)}"
    
    if cache_key in cache:
        return cache[cache_key]
    
    result = model.generate(build_prompt(prompt_type, details))
    cache[cache_key] = result
    return result

# Pre-populate cache
common_patterns = [
    "player_controller",
    "camera_follow",
    "health_system",
    "simple_enemy_ai"
]

for pattern in common_patterns:
    generate_with_cache(pattern, {})
```

## Troubleshooting

### Issue: Generated Code Doesn't Compile

**Solution**:
```python
# Add compilation check
def generate_and_validate(prompt):
    code = model.generate(prompt)
    
    # Quick syntax check
    if not syntax_check(code):
        # Try with more explicit prompt
        prompt += "\n\nEnsure: valid C# syntax, all variables declared, proper namespaces"
        code = model.generate(prompt)
    
    return code
```

### Issue: Unity-Specific APIs Wrong

**Solution**: Enhance RAG with Unity API documentation

```python
# Index Unity ScriptReference
rag.index_documentation(
    "C:/Program Files/Unity/Hub/Editor/2023.2.0f1/Documentation/en/ScriptReference"
)
```

### Issue: Performance Problems

**Solution**: Use tiered approach

```python
# Fast model for drafts, quality model for final
draft = fast_model_7b.generate(prompt)
if user_approves_draft(draft):
    final = quality_model_33b.refine(draft)
```

## Conclusion

Game development with local LLMs is highly effective, especially for Unity and MonoGame (C#). Godot requires more setup but is workable. The key is using appropriate model sizes, maintaining project-specific context, and hybrid fallback for complex systems.

**Recommended Stack**:
- DeepSeek Coder 13B (primary)
- DeepSeek Coder 33B (complex tasks)
- RAG with project code + Unity docs
- MCP Memory for patterns
- Cloud fallback for novel algorithms

**Expected Productivity Gain**: 40-60% for routine tasks, 20-30% for complex systems

## Next Steps

- [C# Development Guide](./csharp-development.md)
- [Hybrid Deployment](../deployment/hybrid.md)
- [MCP Integration](../frameworks/mcp-servers.md)
