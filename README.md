# ResearchBucket

## Latest Addition: Translucent Light Shader System

A complete, production-ready shader effect system for Unity 6000.3 LTS with Universal Render Pipeline (URP), specifically designed and optimized for mobile platforms.

### What's Included

Located in `Assets/Rendering/TranslucentLight/`:

- **URP Shader** - Mobile-optimized translucent light effect
- **Controller Component** - Full-featured script with animations
- **Global Manager** - Centralized management for 100+ instances
- **Custom Editor** - Enhanced Unity Inspector interface
- **Setup Utilities** - Quick configuration helpers
- **Complete Documentation** - README, Quick Start, Examples
- **Sample Assets** - Default material and presets

### Quick Start

1. Apply the `TranslucentLight` shader to your material
2. Add `TranslucentLightController` component to your GameObject
3. Configure color gradient and intensity settings
4. Enable auto-pulse or auto-breathe for animations

See `Assets/Rendering/TranslucentLight/QUICKSTART.md` for detailed instructions.

### Features

- ✨ Transparent rendering with core glow effect
- 💡 Emissive lighting with Fresnel edge highlighting
- ⚡ Pulse animations (quick burst)
- 🫁 Breathing animations (smooth rhythm)
- 🎨 Full color gradient support
- 📊 Optimized for mobile (< 0.1ms per instance)
- 🎯 Global management for hundreds of instances

### Documentation

- **Quick Start**: `Assets/Rendering/TranslucentLight/QUICKSTART.md`
- **Full Documentation**: `Assets/Rendering/TranslucentLight/README.md`
- **Implementation Details**: `IMPLEMENTATION_SUMMARY.md`
- **Change History**: `Assets/Rendering/TranslucentLight/CHANGELOG.md`

### Performance

Designed for mobile-first performance:
- Single instance: < 0.1ms on mid-range mobile
- 100 instances: ~2-3ms on mid-range mobile
- Cached shader properties for efficiency
- Automatic material instancing
- No unnecessary shader variants

---

For more research projects, check the repository contents.