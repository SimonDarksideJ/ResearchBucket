# React/React Native Development with Local LLM Agents

## Overview

React development works well with local LLMs for component generation, hooks, and state management. React Native extends to mobile.

## Recommended Setup

- **Model**: CodeLlama 13B or DeepSeek 13B
- **Hardware**: RTX 4070 or M2 Mac
- **Deployment**: Hybrid (local primary, cloud for latest patterns)

## Common Tasks

| Task | Model | Time | Quality |
|------|-------|------|---------|
| Functional Component | 7B | 2-3s | ⭐⭐⭐⭐⭐ |
| Custom Hook | 13B | 3-5s | ⭐⭐⭐⭐⭐ |
| Context Provider | 13B | 4-6s | ⭐⭐⭐⭐ |
| Redux Slice | 13B | 5-8s | ⭐⭐⭐⭐ |
| API Integration | 13B | 4-6s | ⭐⭐⭐⭐⭐ |
| Responsive Layout | 13B | 5-8s | ⭐⭐⭐⭐ |

## React Native

Similar to React but with platform-specific APIs. Local models handle well with RAG.

## TypeScript Support

Excellent - models trained heavily on TypeScript. Use type-aware prompts for best results.

## Next Steps

- [Documentation Guide](./documentation.md)
- [Deployment Models](../deployment/hybrid.md)
