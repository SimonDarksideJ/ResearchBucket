# Web/WebXR Development with Local LLM Agents

## Overview

Modern web development benefits from local LLMs for component generation, API integration, and responsive design. WebXR adds 3D/VR considerations.

## Recommended Setup

- **Model**: CodeLlama 13B or DeepSeek 13B
- **Hardware**: RTX 4070
- **Deployment**: Hybrid (local for components, cloud for latest patterns)

## Framework Support

| Framework | Local Support | Recommended |
|-----------|--------------|-------------|
| Vanilla JS | ⭐⭐⭐⭐⭐ | Local 13B |
| HTML/CSS | ⭐⭐⭐⭐⭐ | Local 7B |
| Three.js | ⭐⭐⭐⭐ | Local 13B |
| WebXR | ⭐⭐⭐ | Hybrid |
| A-Frame | ⭐⭐⭐ | Hybrid |

## WebXR Considerations

WebXR is less common in training data - recommend hybrid approach with documentation RAG.

## Next Steps

- [React Development Guide](./react-development.md)
- [Documentation Guide](./documentation.md)
