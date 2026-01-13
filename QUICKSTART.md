# Rainbow Fire Effect - Quick Start Guide

## Overview
This package provides a complete rainbow fire effect system for Unity 6000.3 with URP, optimized for mobile platforms.

## What's Included

### Scripts
- **RainbowFireController.cs** - Main controller for the fire effect
- **RainbowFireExample.cs** - Example implementations and use cases
- **RainbowFireTextureGenerator.cs** - Generates particle textures at runtime
- **RainbowFireEditorUtility.cs** - Editor tools for quick setup

### Shaders
- **RainbowFire.shader** - Custom URP shader with rainbow colors and heat shimmer

### Folders
- `/Assets/Scripts/` - All C# scripts
- `/Assets/Shaders/` - Shader files
- `/Assets/Materials/` - Material assets (created automatically)
- `/Assets/Textures/` - Particle textures
- `/Assets/Prefabs/` - Prefab storage

## Quick Setup (5 Minutes)

### Method 1: Using Editor Menu (Recommended)
1. In Unity Editor, go to **GameObject > Effects > Create Rainbow Fire**
2. The effect is ready to use! Adjust parameters in the Inspector
3. Press Play to see it in action

### Method 2: Manual Setup
1. Create Empty GameObject: `GameObject > Create Empty`
2. Add Particle System: `Add Component > Particle System`
3. Add Controller: `Add Component > RainbowFireController`
4. Assign material with RainbowFire shader to Particle System Renderer
5. Press Play and configure parameters

## Basic Configuration

### Fire Dimensions
- **Height**: 0.5 to 10 (default: 2.5)
- **Width**: 0.5 to 5 (default: 1.2)
- **Breadth**: 0.5 to 5 (default: 1.2)

### Fire Behavior
- **Intensity**: 10 to 500 particles/sec (default: 120)
- **Frequency**: 0.1 to 5 (default: 1.0)
- **Slow Motion Speed**: 0.1 to 2 (default: 0.5)

### Rainbow Colors
- **Rainbow Gradient**: Click to edit color stops
- **Animate Colors**: Enable color cycling
- **Color Cycle Speed**: 0.1 to 5 (default: 1.0)

### Heat Shimmer (Optional)
- **Enable Heat Shimmer**: Check to enable
- **Shimmer Intensity**: 0 to 1 (default: 0.5)
- **Shimmer Height Offset**: 0 to 5 (default: 0.5)

## Example Presets

Use RainbowFireExample.cs component for quick presets:

1. **Standard** - Default rainbow fire
2. **Pulsing Fire** - Animated intensity
3. **Color Cycling** - Fast color transitions
4. **Dynamic Size** - Morphing dimensions
5. **Intense Burst** - High particle count
6. **Subtle Flame** - Low intensity
7. **Cool Colors** - Blue/cyan gradient
8. **Warm Colors** - Red/orange/yellow gradient

## Testing Your Setup

1. Press Play in Unity Editor
2. Select your fire GameObject
3. Adjust parameters in real-time
4. Watch changes instantly update

## Common Adjustments

### Make Fire Taller
- Increase `height` parameter
- Increase velocity in particle system

### Make Fire More Intense
- Increase `intensity` parameter
- Adjust `frequency` for faster movement

### Change Colors
- Edit `rainbowGradient` in Inspector
- Add/remove color stops as needed
- Enable `animateColors` for cycling

### Optimize for Mobile
- Reduce `intensity` to 50-100
- Disable `enableHeatShimmer`
- Set Max Particles to 500 or less

## Troubleshooting

### Fire Not Visible
- Check material is assigned
- Ensure shader is "URP/Particles/RainbowFire"
- Verify camera can see particle layer

### Poor Performance
- Reduce intensity value
- Lower Max Particles setting
- Disable heat shimmer
- Use smaller particle texture (128x128)

### Colors Look Wrong
- Check Color Over Lifetime module is enabled
- Verify gradient has color keys
- Increase Emission Strength in material

## Next Steps

1. Read full documentation in `/Assets/README.md`
2. Explore example modes in RainbowFireExample.cs
3. Customize shader properties in material
4. Create prefab variants for different effects
5. Integrate into your game scenes

## Support

For detailed technical information, see:
- `/Assets/README.md` - Complete documentation
- Shader comments in `RainbowFire.shader`
- Script comments in C# files

## Performance Notes

**Mobile Optimization:**
- Default settings work well on mid-range devices
- For low-end devices: intensity < 100, no shimmer
- For high-end devices: intensity up to 300, full shimmer

**Desktop/Console:**
- Can use maximum settings
- Consider LOD system for multiple fires

Enjoy your rainbow fire effect! 🔥🌈
