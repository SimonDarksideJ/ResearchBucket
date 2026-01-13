# Rainbow Fire Effect for Unity 6000.3 URP

A high-quality, configurable rainbow fire particle effect optimized for mobile devices using Unity's Universal Render Pipeline (URP).

## Features

- **Rainbow Gradient Fire**: Fully customizable color gradient with pre-filled rainbow spectrum
- **Configurable Dimensions**: Adjust height, width, and breadth of the fire effect
- **Slow Motion Support**: Control fire speed with slow motion multiplier
- **Adjustable Intensity**: Control particle emission rate and fire density
- **Frequency Control**: Adjust fire movement and flickering patterns
- **Heat Shimmer Effect**: Optional heat distortion above the fire
- **Mobile Optimized**: Designed for URP with mobile performance in mind
- **Real-time Adjustments**: All parameters can be modified at runtime

## Components

### 1. RainbowFireController.cs
Main controller script that manages the fire effect and all its parameters.

**Key Parameters:**
- `height`: Vertical size of the fire (0.5-10)
- `width`: Horizontal size (0.5-5)
- `breadth`: Depth size (0.5-5)
- `intensity`: Particle emission rate (10-500)
- `frequency`: Movement and flicker speed (0.1-5)
- `slowMotionSpeed`: Time scale multiplier (0.1-2)
- `rainbowGradient`: Custom gradient for fire colors
- `enableHeatShimmer`: Toggle heat distortion effect
- `shimmerIntensity`: Strength of heat distortion (0-1)

### 2. RainbowFire.shader
Custom URP HLSL shader optimized for particle rendering with:
- Rainbow color generation
- Procedural distortion and movement
- Heat shimmer effects
- Soft particle support
- Emission control
- Mobile-friendly performance

### 3. RainbowFireTextureGenerator.cs
Utility script to generate particle textures at runtime or in editor.

## Setup Instructions

### Quick Setup (Recommended)

1. **Create Empty GameObject**:
   ```
   GameObject > Create Empty
   Name it "RainbowFire"
   ```

2. **Add Particle System**:
   ```
   Add Component > Effects > Particle System
   ```

3. **Add Controller Script**:
   ```
   Add Component > Scripts > RainbowFireController
   ```

4. **Configure Particle System Renderer**:
   - Set Render Mode to "Billboard"
   - Set Material to one using the RainbowFire shader

5. **Adjust Parameters**:
   - Modify values in the RainbowFireController component
   - All changes are visible in real-time

### Manual Setup (Full Control)

1. **Create Material**:
   ```
   Assets > Create > Material
   Name: "RainbowFireMaterial"
   Shader: URP/Particles/RainbowFire
   ```

2. **Configure Material**:
   - Set Particle Texture (use Default-Particle or generate custom)
   - Adjust Emission Strength (2-5 recommended)
   - Set Soft Particles Factor (0.5-1 recommended)
   - Configure Distortion Strength (0.1-0.3 recommended)

3. **Create GameObject with Particle System**:
   ```
   GameObject > Effects > Particle System
   Name: "RainbowFire"
   ```

4. **Configure Particle System**:
   - **Renderer Module**:
     - Material: RainbowFireMaterial
     - Render Mode: Billboard
     - Sorting Fudge: 0
   
   - **Main Module**:
     - Duration: Looping
     - Start Lifetime: 2-3 seconds
     - Start Speed: 1-2
     - Start Size: 0.5-1
     - 3D Start Rotation: Enabled
     - Gravity Modifier: -0.2 (slight upward drift)
     - Simulation Speed: 0.5 (slow motion)
     - Max Particles: 1000

   - **Emission Module**:
     - Rate over Time: 100-200

   - **Shape Module**:
     - Shape: Box
     - Scale: 1, 0.1, 1
     - Position: 0, 0, 0

5. **Add RainbowFireController Script**:
   - Add Component > Scripts > RainbowFireController
   - Script will auto-configure particle system
   - Adjust parameters as needed

## Usage Examples

### Basic Usage

```csharp
// Get reference to controller
RainbowFireController fireController = GetComponent<RainbowFireController>();

// Adjust fire size
fireController.height = 3f;
fireController.width = 1.5f;

// Change intensity
fireController.intensity = 150f;

// Enable heat shimmer
fireController.enableHeatShimmer = true;
fireController.shimmerIntensity = 0.7f;
```

### Custom Rainbow Gradient

```csharp
// Create custom gradient
Gradient customGradient = new Gradient();

GradientColorKey[] colorKeys = new GradientColorKey[3];
colorKeys[0] = new GradientColorKey(Color.blue, 0f);
colorKeys[1] = new GradientColorKey(Color.cyan, 0.5f);
colorKeys[2] = new GradientColorKey(Color.white, 1f);

GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
alphaKeys[0] = new GradientAlphaKey(1f, 0f);
alphaKeys[1] = new GradientAlphaKey(1f, 1f);

customGradient.SetKeys(colorKeys, alphaKeys);

// Apply to fire
fireController.SetRainbowGradient(customGradient);
```

### Trigger Fire Burst

```csharp
// Emit burst of particles
fireController.TriggerBurst(100);
```

### Animate Fire Parameters

```csharp
void Update()
{
    // Pulse fire intensity
    float pulse = Mathf.Sin(Time.time * 2f) * 0.5f + 0.5f;
    fireController.intensity = Mathf.Lerp(50f, 200f, pulse);
    
    // Vary height
    fireController.height = Mathf.Lerp(2f, 4f, pulse);
}
```

## Performance Optimization

### Mobile Optimization Tips

1. **Reduce Max Particles**: Lower to 500 or less for better performance
2. **Adjust Emission Rate**: Reduce intensity to 50-100 for mobile devices
3. **Disable Heat Shimmer**: Turn off for low-end devices
4. **Texture Size**: Use 128x128 or 256x256 textures
5. **LOD System**: Reduce particle count based on distance

### Example Performance Script

```csharp
public class FirePerformanceManager : MonoBehaviour
{
    public RainbowFireController fireController;
    public Camera mainCamera;
    public float maxDistance = 20f;
    
    void Update()
    {
        float distance = Vector3.Distance(mainCamera.transform.position, transform.position);
        float distanceRatio = Mathf.Clamp01(distance / maxDistance);
        
        // Reduce intensity based on distance
        fireController.intensity = Mathf.Lerp(200f, 50f, distanceRatio);
        
        // Disable shimmer when far
        fireController.enableHeatShimmer = distance < maxDistance * 0.5f;
    }
}
```

## Shader Properties Reference

| Property | Type | Range | Description |
|----------|------|-------|-------------|
| _MainTex | Texture2D | - | Particle texture (radial gradient) |
| _TintColor | Color | - | Overall color tint |
| _ColorOffset | Float | 0-1 | Rainbow color cycle offset |
| _ColorIntensity | Float | 0-5 | Color saturation/brightness |
| _EmissionStrength | Float | 0-10 | Glow intensity |
| _Softness | Float | 0.01-3 | Soft particle fade distance |
| _DistortionStrength | Float | 0-1 | Fire movement distortion |
| _DistortionSpeed | Float | 0-5 | Animation speed |
| _ShimmerIntensity | Float | 0-1 | Heat distortion strength |
| _ShimmerScale | Float | 0.1-10 | Heat wave scale |
| _ShimmerSpeed | Float | 0-5 | Heat wave speed |

## Troubleshooting

### Fire not visible
- Ensure material is assigned to Particle System Renderer
- Check that particle emission is enabled
- Verify camera can see the particle layer
- Check lighting settings (particles should emit light)

### Poor performance
- Reduce Max Particles in Main Module
- Lower emission rate (intensity)
- Disable heat shimmer
- Use smaller texture size
- Reduce distortion calculations in shader

### Colors not showing correctly
- Verify shader is set to URP/Particles/RainbowFire
- Check Color Over Lifetime module is enabled
- Ensure gradient has proper color keys
- Verify material emission strength is adequate

### Heat shimmer not visible
- Enable shimmer in RainbowFireController
- Increase shimmerIntensity
- Ensure shimmer particles are being emitted
- Check shimmer GameObject is active

## Technical Details

### Why Particle System + Custom Shader?

For Unity 6000.3 URP targeting mobile, this approach was chosen because:

1. **Performance**: Particle systems are highly optimized in Unity 6
2. **Mobile Compatibility**: URP particles perform well on mobile GPUs
3. **Control**: Full control over color, movement, and effects
4. **Integration**: Works seamlessly with Unity's VFX workflow
5. **Scalability**: Easy to adjust quality for different device tiers

### Alternatives Considered

- **Visual Effect Graph (VFX)**: Requires compute shader support, not ideal for all mobile devices
- **Shader Graph**: Limited control over particle simulation, less flexible
- **Pure HLSL**: More complex to set up, harder to maintain

## Credits

Created for Unity 6000.3 with Universal Render Pipeline (URP)
Optimized for mobile platforms

## License

This code is provided as-is for use in Unity projects.
