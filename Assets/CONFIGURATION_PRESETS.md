# Rainbow Fire - Configuration Presets

This document provides tested configuration presets for various use cases.

## Standard Presets

### 1. Classic Rainbow Fire
```
Height: 2.5
Width: 1.2
Breadth: 1.2
Intensity: 120
Frequency: 1.0
Slow Motion Speed: 0.5
Enable Heat Shimmer: true
Shimmer Intensity: 0.5
Animate Colors: true
Color Cycle Speed: 1.0

Gradient:
- 0.0: RGB(255, 0, 0) - Red
- 0.166: RGB(255, 128, 0) - Orange
- 0.333: RGB(255, 255, 0) - Yellow
- 0.5: RGB(0, 255, 0) - Green
- 0.666: RGB(0, 0, 255) - Blue
- 0.833: RGB(128, 0, 255) - Indigo
- 1.0: RGB(255, 0, 255) - Violet
```

### 2. Campfire (Small, Warm)
```
Height: 1.5
Width: 0.8
Breadth: 0.8
Intensity: 60
Frequency: 0.8
Slow Motion Speed: 0.4
Enable Heat Shimmer: false
Animate Colors: false

Gradient:
- 0.0: RGB(255, 0, 0) - Red
- 0.4: RGB(255, 128, 0) - Orange
- 0.7: RGB(255, 255, 0) - Yellow
- 1.0: RGB(255, 255, 200) - Light Yellow
```

### 3. Magical Portal (Large, Intense)
```
Height: 5.0
Width: 3.0
Breadth: 3.0
Intensity: 250
Frequency: 1.5
Slow Motion Speed: 0.6
Enable Heat Shimmer: true
Shimmer Intensity: 0.8
Animate Colors: true
Color Cycle Speed: 2.0

Gradient:
- 0.0: RGB(128, 0, 255) - Purple
- 0.33: RGB(0, 128, 255) - Blue
- 0.66: RGB(0, 255, 255) - Cyan
- 1.0: RGB(255, 255, 255) - White
```

### 4. Holy Fire (Divine, Bright)
```
Height: 3.0
Width: 1.5
Breadth: 1.5
Intensity: 150
Frequency: 0.7
Slow Motion Speed: 0.3
Enable Heat Shimmer: true
Shimmer Intensity: 0.6
Animate Colors: true
Color Cycle Speed: 0.5

Gradient:
- 0.0: RGB(255, 255, 200) - Light Yellow
- 0.33: RGB(255, 255, 255) - White
- 0.66: RGB(200, 200, 255) - Light Blue
- 1.0: RGB(255, 255, 255) - White
```

### 5. Ice Fire (Cool Colors)
```
Height: 2.0
Width: 1.0
Breadth: 1.0
Intensity: 100
Frequency: 1.2
Slow Motion Speed: 0.4
Enable Heat Shimmer: false
Animate Colors: true
Color Cycle Speed: 1.5

Gradient:
- 0.0: RGB(0, 128, 255) - Deep Blue
- 0.33: RGB(0, 255, 255) - Cyan
- 0.66: RGB(128, 255, 200) - Light Cyan
- 1.0: RGB(255, 255, 255) - White
```

### 6. Toxic Fire (Green/Yellow)
```
Height: 2.2
Width: 1.3
Breadth: 1.3
Intensity: 140
Frequency: 1.3
Slow Motion Speed: 0.5
Enable Heat Shimmer: true
Shimmer Intensity: 0.7
Animate Colors: true
Color Cycle Speed: 1.2

Gradient:
- 0.0: RGB(0, 255, 0) - Green
- 0.33: RGB(128, 255, 0) - Yellow-Green
- 0.66: RGB(255, 255, 0) - Yellow
- 1.0: RGB(255, 200, 0) - Orange-Yellow
```

### 7. Inferno (Hot, Red/Orange)
```
Height: 4.0
Width: 2.5
Breadth: 2.5
Intensity: 300
Frequency: 2.0
Slow Motion Speed: 0.7
Enable Heat Shimmer: true
Shimmer Intensity: 0.9
Animate Colors: false

Gradient:
- 0.0: RGB(128, 0, 0) - Dark Red
- 0.33: RGB(255, 0, 0) - Red
- 0.66: RGB(255, 128, 0) - Orange
- 1.0: RGB(255, 255, 0) - Yellow
```

### 8. Ethereal Flame (Soft, Pastel)
```
Height: 2.0
Width: 1.0
Breadth: 1.0
Intensity: 80
Frequency: 0.6
Slow Motion Speed: 0.3
Enable Heat Shimmer: false
Animate Colors: true
Color Cycle Speed: 0.8

Gradient:
- 0.0: RGB(255, 200, 200) - Light Pink
- 0.25: RGB(200, 200, 255) - Light Blue
- 0.5: RGB(255, 255, 200) - Light Yellow
- 0.75: RGB(200, 255, 200) - Light Green
- 1.0: RGB(255, 200, 255) - Light Purple
```

## Mobile-Optimized Presets

### Mobile - Low End
```
Height: 2.0
Width: 1.0
Breadth: 1.0
Intensity: 50
Frequency: 1.0
Slow Motion Speed: 0.5
Enable Heat Shimmer: false
Animate Colors: true
Color Cycle Speed: 1.0
Max Particles: 300 (set in Particle System)
Texture Size: 128x128
```

### Mobile - Mid Range
```
Height: 2.5
Width: 1.2
Breadth: 1.2
Intensity: 100
Frequency: 1.0
Slow Motion Speed: 0.5
Enable Heat Shimmer: false
Animate Colors: true
Color Cycle Speed: 1.0
Max Particles: 500 (set in Particle System)
Texture Size: 256x256
```

### Mobile - High End
```
Height: 3.0
Width: 1.5
Breadth: 1.5
Intensity: 150
Frequency: 1.0
Slow Motion Speed: 0.5
Enable Heat Shimmer: true
Shimmer Intensity: 0.4
Animate Colors: true
Color Cycle Speed: 1.0
Max Particles: 800 (set in Particle System)
Texture Size: 256x256
```

## Desktop/Console Presets

### Desktop - Quality
```
Height: 4.0
Width: 2.0
Breadth: 2.0
Intensity: 200
Frequency: 1.2
Slow Motion Speed: 0.5
Enable Heat Shimmer: true
Shimmer Intensity: 0.6
Animate Colors: true
Color Cycle Speed: 1.0
Max Particles: 1000 (set in Particle System)
Texture Size: 512x512
```

### Desktop - Performance
```
Height: 3.0
Width: 1.5
Breadth: 1.5
Intensity: 150
Frequency: 1.0
Slow Motion Speed: 0.5
Enable Heat Shimmer: true
Shimmer Intensity: 0.5
Animate Colors: true
Color Cycle Speed: 1.0
Max Particles: 800 (set in Particle System)
Texture Size: 256x256
```

## Shader Material Settings

### Standard Material
```
Emission Strength: 2.5
Color Intensity: 1.5
Distortion Strength: 0.15
Distortion Speed: 1.0
Softness: 1.0
Shimmer Scale: 2.0
Shimmer Speed: 1.0
Blend Mode: Additive (SrcAlpha, One)
```

### High Quality Material
```
Emission Strength: 3.5
Color Intensity: 2.0
Distortion Strength: 0.2
Distortion Speed: 1.2
Softness: 1.5
Shimmer Scale: 2.5
Shimmer Speed: 1.2
Blend Mode: Additive (SrcAlpha, One)
```

### Performance Material
```
Emission Strength: 2.0
Color Intensity: 1.2
Distortion Strength: 0.1
Distortion Speed: 0.8
Softness: 0.8
Shimmer Scale: 1.5
Shimmer Speed: 0.8
Blend Mode: Additive (SrcAlpha, One)
```

## Animation Presets

### Pulsing Fire
```
Script: Animate intensity between 60 and 180 using sine wave
Period: 2 seconds
Fire Height: Sync with intensity (1.5 to 3.0)
```

### Color Cycling
```
Enable: animateColors = true
Color Cycle Speed: 2.0
Update gradient every frame based on time offset
```

### Dynamic Size
```
Script: Animate width and breadth independently
Width: Sine wave 0.8 to 2.0 over 3 seconds
Breadth: Cosine wave 0.8 to 2.0 over 3 seconds
Height: Sine wave 2.0 to 4.0 over 5 seconds
```

### Burst Effect
```
Trigger burst every 3 seconds
Burst particle count: 80
Intensity spike: 1.5x for 0.5 seconds after burst
```

## Tips for Creating Custom Presets

1. **Start with a base preset** closest to your desired effect
2. **Adjust one parameter at a time** to see its impact
3. **Test on target platform** to ensure performance
4. **Save as prefab variant** once satisfied
5. **Document your settings** for team reference

## Performance Guidelines

| Setting | Low End | Mid Range | High End |
|---------|---------|-----------|----------|
| Intensity | < 60 | 60-120 | 120-300 |
| Max Particles | < 400 | 400-700 | 700-1200 |
| Heat Shimmer | Off | Optional | On |
| Texture Size | 128 | 256 | 256-512 |
| Target FPS | 30 | 60 | 60+ |

## Color Gradient Tips

**Rainbow (Full Spectrum):**
Use 7 colors: Red → Orange → Yellow → Green → Blue → Indigo → Violet

**Warm (Fire):**
Use 3-4 colors: Dark Red → Red → Orange → Yellow

**Cool (Ice):**
Use 3-4 colors: Dark Blue → Blue → Cyan → White

**Mystical:**
Use 4-5 colors: Purple → Blue → Cyan → White → Light Purple

**Toxic:**
Use 3-4 colors: Green → Yellow-Green → Yellow → Orange

**Monochrome:**
Use 2-3 colors: Dark shade → Light shade → White

## Recommended Uses by Preset

| Preset | Best For | Performance Cost |
|--------|----------|------------------|
| Classic Rainbow | General purpose, demos | Medium |
| Campfire | Ambient, environment | Low |
| Magical Portal | Boss fights, transitions | High |
| Holy Fire | Divine spells, healing | Medium |
| Ice Fire | Frost spells, winter themes | Medium |
| Toxic Fire | Poison effects, hazards | Medium |
| Inferno | Intense moments, destruction | High |
| Ethereal Flame | Subtle effects, UI | Low |

---

**Experiment and create your own unique presets!** 🎨🔥
