# Learning and Extensibility

## Overview

Local LLM agents can be enhanced through fine-tuning, RAG, and continuous learning strategies to become more effective over time. This guide covers methods to make your local setup "grow with the environment."

## Learning Strategies

### 1. Fine-Tuning on Project Codebase

**Purpose**: Adapt model to project-specific patterns and conventions

**Benefits**:
- Better code style matching
- Project-specific naming conventions
- Framework-specific patterns
- Domain knowledge integration

**Implementation**:
```python
# fine-tune.py
from transformers import AutoTokenizer, AutoModelForCausalLM, Trainer, TrainingArguments
import torch

def fine_tune_on_codebase(base_model, codebase_path, output_path):
    # Load model and tokenizer
    tokenizer = AutoTokenizer.from_pretrained(base_model)
    model = AutoModelForCausalLM.from_pretrained(
        base_model,
        torch_dtype=torch.float16,
        device_map="auto"
    )
    
    # Prepare dataset from codebase
    dataset = load_and_tokenize_codebase(codebase_path, tokenizer)
    
    # Training arguments
    training_args = TrainingArguments(
        output_dir=output_path,
        num_train_epochs=3,
        per_device_train_batch_size=4,
        gradient_accumulation_steps=4,
        learning_rate=2e-5,
        fp16=True,
        save_steps=500,
        logging_steps=100,
        warmup_steps=100
    )
    
    # Create trainer
    trainer = Trainer(
        model=model,
        args=training_args,
        train_dataset=dataset
    )
    
    # Fine-tune
    trainer.train()
    trainer.save_model(output_path)
    
    print(f"Model fine-tuned and saved to {output_path}")

# Usage
fine_tune_on_codebase(
    base_model="deepseek-coder-13b",
    codebase_path="/path/to/unity-project",
    output_path="./models/deepseek-unity-specialized"
)
```

### 2. RAG (Retrieval Augmented Generation)

**Purpose**: Dynamically augment model with relevant context from codebase

**Benefits**:
- No model retraining needed
- Always current information
- Scalable to large codebases
- Easy to update

**Implementation**:
```python
# rag_system.py
from langchain.vectorstores import Chroma
from langchain.embeddings import OllamaEmbeddings
from langchain.text_splitter import RecursiveCharacterTextSplitter
import os

class CodebaseRAG:
    def __init__(self, codebase_path):
        self.codebase_path = codebase_path
        self.embeddings = OllamaEmbeddings(model="nomic-embed-text")
        self.vectorstore = None
        
    def index_codebase(self):
        """Index entire codebase"""
        documents = []
        
        for root, dirs, files in os.walk(self.codebase_path):
            # Skip common non-code directories
            dirs[:] = [d for d in dirs if d not in [
                'node_modules', 'bin', 'obj', '.git', 'dist', 'build'
            ]]
            
            for file in files:
                if self._is_code_file(file):
                    file_path = os.path.join(root, file)
                    try:
                        with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
                            content = f.read()
                            documents.append({
                                'content': content,
                                'metadata': {
                                    'source': file_path,
                                    'filename': file,
                                    'extension': os.path.splitext(file)[1]
                                }
                            })
                    except:
                        continue
        
        # Split documents
        text_splitter = RecursiveCharacterTextSplitter(
            chunk_size=1000,
            chunk_overlap=200,
            separators=["\n\nclass ", "\n\ndef ", "\n\n", "\n", " "]
        )
        
        splits = []
        for doc in documents:
            chunks = text_splitter.split_text(doc['content'])
            for chunk in chunks:
                splits.append({
                    'content': chunk,
                    'metadata': doc['metadata']
                })
        
        # Create vector store
        self.vectorstore = Chroma.from_documents(
            documents=[{'page_content': s['content'], 'metadata': s['metadata']} for s in splits],
            embedding=self.embeddings,
            persist_directory=f"./vectorstore/{os.path.basename(self.codebase_path)}"
        )
        
        print(f"Indexed {len(documents)} files into {len(splits)} chunks")
    
    def search(self, query, k=5):
        """Search for relevant code"""
        if not self.vectorstore:
            raise ValueError("Codebase not indexed. Call index_codebase() first.")
        
        results = self.vectorstore.similarity_search_with_score(query, k=k)
        return results
    
    def get_relevant_context(self, query, max_tokens=4000):
        """Get relevant context within token limit"""
        results = self.search(query, k=10)
        
        context = ""
        for doc, score in results:
            if len(context) + len(doc.page_content) < max_tokens * 4:  # rough token estimate
                context += f"// From {doc.metadata['source']}\n"
                context += doc.page_content + "\n\n"
        
        return context
    
    @staticmethod
    def _is_code_file(filename):
        code_extensions = ['.cs', '.ts', '.tsx', '.js', '.jsx', '.py', 
                          '.go', '.rs', '.java', '.cpp', '.h', '.hpp', '.md']
        return any(filename.endswith(ext) for ext in code_extensions)

# Usage
rag = CodebaseRAG("/path/to/unity-project")
rag.index_codebase()

# When generating code
query = "how to create a player controller"
relevant_context = rag.get_relevant_context(query)
prompt = f"Context:\n{relevant_context}\n\nTask: {query}"
```

### 3. Memory-Based Learning

**Purpose**: Remember patterns, decisions, and preferences across sessions

**Implementation**:
```python
# memory_learning.py
class LearningMemory:
    def __init__(self, mcp_memory_url):
        self.memory = MCPMemoryClient(mcp_memory_url)
        
    async def record_interaction(self, prompt, response, feedback):
        """Record interaction with feedback"""
        await self.memory.store({
            'prompt': prompt,
            'response': response,
            'feedback': feedback,
            'timestamp': datetime.now().isoformat()
        })
    
    async def get_similar_interactions(self, prompt, k=5):
        """Retrieve similar past interactions"""
        results = await self.memory.search(prompt, k=k)
        return results
    
    async def learn_preference(self, category, preference):
        """Store user preference"""
        await self.memory.store({
            'type': 'preference',
            'category': category,
            'value': preference
        })
    
    async def get_preferences(self):
        """Get all stored preferences"""
        prefs = await self.memory.search("type:preference")
        return prefs

# Usage
memory = LearningMemory("http://localhost:3000")

# Record interaction
await memory.record_interaction(
    prompt="create a player controller",
    response="public class PlayerController : MonoBehaviour {...}",
    feedback="good - user accepted"
)

# Later, use similar interactions to improve
similar = await memory.get_similar_interactions("create enemy controller")
# Use similar[0] as example for better generation
```

### 4. Few-Shot Learning

**Purpose**: Improve performance by providing examples

**Implementation**:
```python
# few_shot.py
class FewShotLearner:
    def __init__(self):
        self.examples_db = {}
    
    def add_example(self, task_type, input_example, output_example):
        """Add an example for a task type"""
        if task_type not in self.examples_db:
            self.examples_db[task_type] = []
        
        self.examples_db[task_type].append({
            'input': input_example,
            'output': output_example
        })
    
    def get_examples(self, task_type, k=3):
        """Get k examples for task type"""
        if task_type not in self.examples_db:
            return []
        
        return self.examples_db[task_type][:k]
    
    def build_prompt(self, task_type, new_input):
        """Build prompt with examples"""
        examples = self.get_examples(task_type)
        
        prompt = f"Task: {task_type}\n\n"
        prompt += "Examples:\n\n"
        
        for i, ex in enumerate(examples, 1):
            prompt += f"Example {i}:\n"
            prompt += f"Input: {ex['input']}\n"
            prompt += f"Output: {ex['output']}\n\n"
        
        prompt += f"Now complete this:\n"
        prompt += f"Input: {new_input}\n"
        prompt += f"Output:"
        
        return prompt

# Usage
learner = FewShotLearner()

# Add examples
learner.add_example(
    "unity_monobehaviour",
    "player movement",
    '''public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    
    void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        transform.Translate(new Vector3(h, 0, v) * speed * Time.deltaTime);
    }
}'''
)

# Generate with examples
prompt = learner.build_prompt("unity_monobehaviour", "enemy patrol")
result = model.generate(prompt)
```

### 5. Continuous Feedback Loop

**Purpose**: Improve through user feedback

**Implementation**:
```python
# feedback_loop.py
class FeedbackSystem:
    def __init__(self):
        self.feedback_db = []
        
    def record_feedback(self, generation_id, code, rating, comments):
        """Record user feedback"""
        self.feedback_db.append({
            'id': generation_id,
            'code': code,
            'rating': rating,  # 1-5
            'comments': comments,
            'timestamp': datetime.now()
        })
    
    def get_high_quality_examples(self, min_rating=4):
        """Get highly rated examples"""
        return [f for f in self.feedback_db if f['rating'] >= min_rating]
    
    def analyze_patterns(self):
        """Analyze what works well"""
        high_quality = self.get_high_quality_examples()
        
        # Extract patterns
        patterns = {
            'common_keywords': extract_keywords(high_quality),
            'code_structures': analyze_structures(high_quality),
            'naming_conventions': extract_naming(high_quality)
        }
        
        return patterns
    
    def update_system_prompt(self, patterns):
        """Update system prompt based on patterns"""
        prompt = "You are a coding assistant. Based on successful past generations:\n"
        prompt += f"- Use these naming conventions: {patterns['naming_conventions']}\n"
        prompt += f"- Follow these structures: {patterns['code_structures']}\n"
        prompt += f"- Include these patterns: {patterns['common_keywords']}\n"
        
        return prompt

# Usage
feedback = FeedbackSystem()

# User provides feedback
feedback.record_feedback(
    generation_id="gen_123",
    code="public class PlayerController {...}",
    rating=5,
    comments="Perfect, exactly what I needed"
)

# Periodically update system
patterns = feedback.analyze_patterns()
updated_prompt = feedback.update_system_prompt(patterns)
```

## Extensibility Framework

### Plugin System

```python
# plugin_system.py
class PluginManager:
    def __init__(self):
        self.plugins = {}
    
    def register_plugin(self, name, plugin):
        """Register a new plugin"""
        self.plugins[name] = plugin
    
    def call_plugin(self, name, *args, **kwargs):
        """Call a plugin"""
        if name not in self.plugins:
            raise ValueError(f"Plugin {name} not found")
        
        return self.plugins[name](*args, **kwargs)

# Example plugin: Unity-specific code enhancer
class UnityEnhancerPlugin:
    def __call__(self, code):
        # Add Unity-specific improvements
        if "MonoBehaviour" in code:
            # Ensure SerializeField attributes
            code = self.add_serialize_field(code)
            # Add null checks
            code = self.add_null_checks(code)
        return code
    
    def add_serialize_field(self, code):
        # Implementation
        return code
    
    def add_null_checks(self, code):
        # Implementation
        return code

# Usage
plugin_manager = PluginManager()
plugin_manager.register_plugin("unity_enhancer", UnityEnhancerPlugin())

# Use plugin
enhanced_code = plugin_manager.call_plugin("unity_enhancer", generated_code)
```

### Custom MCP Servers

```javascript
// custom-learning-mcp.js
const { MCPServer } = require('@modelcontextprotocol/sdk');

class LearningMCPServer extends MCPServer {
  constructor() {
    super({
      name: 'learning-server',
      version: '1.0.0'
    });
    
    this.patterns = {};
    this.registerTools();
  }
  
  registerTools() {
    this.registerTool({
      name: 'record_pattern',
      description: 'Record a successful code pattern',
      parameters: {
        type: 'object',
        properties: {
          pattern_type: { type: 'string' },
          code: { type: 'string' },
          context: { type: 'string' }
        }
      },
      handler: async (params) => {
        if (!this.patterns[params.pattern_type]) {
          this.patterns[params.pattern_type] = [];
        }
        this.patterns[params.pattern_type].push({
          code: params.code,
          context: params.context,
          timestamp: new Date()
        });
        return { success: true };
      }
    });
    
    this.registerTool({
      name: 'get_patterns',
      description: 'Retrieve patterns for a type',
      parameters: {
        type: 'object',
        properties: {
          pattern_type: { type: 'string' },
          limit: { type: 'number', default: 5 }
        }
      },
      handler: async (params) => {
        const patterns = this.patterns[params.pattern_type] || [];
        return {
          patterns: patterns.slice(0, params.limit)
        };
      }
    });
  }
}

const server = new LearningMCPServer();
server.start();
```

## Best Practices for Learning Systems

1. **Start with RAG**: Easiest to implement, no model training
2. **Collect Feedback**: Every generation should be rateable
3. **Fine-Tune Periodically**: Monthly or when enough data collected
4. **Version Control Models**: Keep history of fine-tuned versions
5. **A/B Testing**: Compare model versions objectively
6. **Monitor Quality**: Track metrics over time
7. **User Preferences**: Learn individual coding styles
8. **Domain-Specific**: Separate models/RAG per project type

## Conclusion

Local LLM agents can significantly improve through various learning strategies. Combining RAG for immediate context, memory for long-term patterns, and occasional fine-tuning provides a robust learning framework that grows with your environment.

**Recommended Learning Stack**:
- RAG for dynamic context
- MCP Memory for preferences and patterns
- Few-shot learning for task-specific improvements
- Quarterly fine-tuning on successful generations
- User feedback loop for continuous improvement

## Next Steps

- [Use Case Guides](../use-cases/)
- [MCP Servers Integration](../frameworks/mcp-servers.md)
- [Deployment Models](../deployment/hybrid.md)
