# ResearchBucket - Rainbow Fire Effect for Unity 6000.3 URP

A complete, production-ready rainbow fire particle effect system optimized for Unity 6000.3 with Universal Render Pipeline (URP), targeting mobile platforms.

## 🎨 Features

- **🌈 Configurable Rainbow Fire**: Full spectrum rainbow gradient with customizable color stops
- **📏 Adjustable Dimensions**: Control height, width, and breadth independently
- **⚡ Slow Motion Support**: Built-in time scaling for cinematic effects
- **🔥 Dynamic Intensity**: Real-time particle emission control
- **🌊 Heat Shimmer Effect**: Optional atmospheric distortion above the fire
- **📱 Mobile Optimized**: Performance-tuned for mobile GPUs with URP
- **🎮 Runtime Configuration**: All parameters adjustable at runtime via script or Inspector
- **🛠️ Editor Tools**: Quick-setup menu commands for instant creation

## 🚀 Quick Start

See [QUICKSTART.md](QUICKSTART.md) for a 5-minute setup guide.

### Fastest Setup (30 seconds)
1. Open Unity 6000.3 with URP
2. Menu: `GameObject > Effects > Create Rainbow Fire`
3. Press Play and enjoy!

### Manual Setup
1. Create GameObject with Particle System
2. Add `RainbowFireController` component
3. Assign material with `URP/Particles/RainbowFire` shader
4. Configure and play

## 📦 What's Included

### Scripts
- **RainbowFireController.cs** - Main effect controller with all parameters
- **RainbowFireExample.cs** - Example implementations and presets
- **RainbowFireTextureGenerator.cs** - Runtime texture generation
- **RainbowFireEditorUtility.cs** - Editor tools and utilities

### Shaders
- **RainbowFire.shader** - Custom URP HLSL shader with:
  - Rainbow color generation
  - Procedural noise distortion
  - Heat shimmer effects
  - Soft particle blending
  - Mobile optimization

### Documentation
- **Assets/README.md** - Complete technical documentation
- **QUICKSTART.md** - Quick setup guide
- Inline code comments throughout

## 🎯 Use Cases

Perfect for:
- Fantasy game effects (magical fires, spell effects)
- Sci-fi environments (energy fields, portals)
- Artistic installations and visualizations
- Prototype effects and experimentation
- Educational demonstrations of particle systems

## ⚙️ Technical Specifications

- **Unity Version**: 6000.3 LTS (6000.0.23f1 or later)
- **Render Pipeline**: Universal Render Pipeline (URP)
- **Target Platforms**: Mobile (iOS, Android), Desktop, Console
- **Shader Model**: 3.0+
- **Particle System**: Unity's built-in particle system
- **Performance**: Optimized for 60 FPS on mid-range mobile devices

## 🎮 Key Parameters

### Fire Dimensions
- Height (0.5 - 10m)
- Width (0.5 - 5m)
- Breadth (0.5 - 5m)

### Fire Behavior
- Intensity (10 - 500 particles/sec)
- Frequency (0.1 - 5x)
- Slow Motion Speed (0.1 - 2x)

### Rainbow Colors
- Gradient with unlimited color stops
- Pre-filled rainbow spectrum
- Animated color cycling option

### Heat Shimmer
- Toggle on/off
- Adjustable intensity (0 - 1)
- Height offset control

## 📊 Performance

| Platform | Intensity | Max Particles | FPS Target |
|----------|-----------|---------------|------------|
| High-end Mobile | 150-300 | 1000 | 60 FPS |
| Mid-range Mobile | 80-150 | 500-800 | 60 FPS |
| Low-end Mobile | 50-100 | 300-500 | 30-60 FPS |
| Desktop/Console | 200-500 | 1000+ | 60+ FPS |

## 🔧 Customization Examples

```csharp
// Get controller
RainbowFireController fire = GetComponent<RainbowFireController>();

// Adjust size
fire.height = 5f;
fire.width = 2f;

// Custom gradient (blue fire)
Gradient blueGradient = new Gradient();
blueGradient.SetKeys(
    new GradientColorKey[] {
        new GradientColorKey(Color.blue, 0f),
        new GradientColorKey(Color.cyan, 0.5f),
        new GradientColorKey(Color.white, 1f)
    },
    new GradientAlphaKey[] {
        new GradientAlphaKey(1f, 0f),
        new GradientAlphaKey(1f, 1f)
    }
);
fire.SetRainbowGradient(blueGradient);

// Trigger burst
fire.TriggerBurst(100);
```

## 🎨 Example Presets

The `RainbowFireExample` component includes 8 preset modes:
1. **Standard** - Classic rainbow fire
2. **Pulsing Fire** - Breathing effect
3. **Color Cycling** - Animated colors
4. **Dynamic Size** - Morphing dimensions
5. **Intense Burst** - High energy
6. **Subtle Flame** - Gentle effect
7. **Cool Colors** - Blue/cyan theme
8. **Warm Colors** - Red/orange theme

## 📖 Documentation

- [Complete Documentation](Assets/README.md) - Full technical details
- [Quick Start Guide](QUICKSTART.md) - Fast setup instructions
- Inline code comments - Detailed explanations in all scripts

## 🛠️ Requirements

- Unity 6000.3 LTS or later
- Universal Render Pipeline (URP) package
- Basic understanding of particle systems (helpful but not required)

## 🎓 Technical Approach

### Why Particle System + Custom Shader?

For Unity 6000.3 URP targeting mobile:

✅ **Particle System Advantages:**
- Highly optimized in Unity 6
- Excellent mobile GPU performance
- Built-in simulation features
- Easy integration with existing projects
- Well-documented and stable

✅ **Custom HLSL Shader Advantages:**
- Full control over rendering
- Rainbow color generation
- Procedural effects
- URP optimization
- Mobile-friendly calculations

❌ **Not VFX Graph because:**
- Requires compute shader support
- Not all mobile devices support it
- Higher complexity for simple effects

❌ **Not Shader Graph because:**
- Less control over particle simulation
- More overhead for mobile
- Limited procedural capabilities

## 🐛 Troubleshooting

**Fire not visible?**
- Check material assignment
- Verify shader is "URP/Particles/RainbowFire"
- Ensure particle emission is enabled

**Poor performance?**
- Reduce intensity (< 100 for mobile)
- Disable heat shimmer
- Lower max particles (< 500)

**Colors wrong?**
- Enable Color Over Lifetime module
- Check gradient has color keys
- Increase material emission strength

See [full documentation](Assets/README.md) for more troubleshooting.

## 📝 License

This code is provided for use in Unity projects. Feel free to modify and integrate into your games.

## 🙏 Credits

Created for Unity 6000.3 with Universal Render Pipeline (URP)
Optimized for mobile platforms with desktop/console support

## 🔗 Project Structure

```
ResearchBucket/
├── Assets/
│   ├── Scripts/
│   │   ├── RainbowFireController.cs
│   │   ├── RainbowFireExample.cs
│   │   ├── RainbowFireTextureGenerator.cs
│   │   └── Editor/
│   │       └── RainbowFireEditorUtility.cs
│   ├── Shaders/
│   │   └── RainbowFire.shader
│   ├── Materials/
│   │   └── (Generated at runtime)
│   ├── Textures/
│   │   └── (Generated or imported)
│   ├── Prefabs/
│   │   └── (User-created)
│   └── README.md (Technical documentation)
├── QUICKSTART.md
└── README.md (This file)
```

---

**Ready to create amazing rainbow fire effects!** 🔥🌈

For questions or issues, refer to the documentation or examine the well-commented source code.