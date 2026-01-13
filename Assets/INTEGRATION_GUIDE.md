# Integration Guide - Rainbow Fire Effect

This guide helps you integrate the Rainbow Fire effect into your existing Unity 6000.3 URP project.

## Prerequisites

Before integrating, ensure you have:
- Unity 6000.3 LTS or later installed
- Universal Render Pipeline (URP) configured in your project
- Basic familiarity with Unity's Inspector and scripting

## Step-by-Step Integration

### 1. Import Files

Copy the following folders from this repository to your Unity project:

```
Your Project/Assets/
├── Scripts/
│   ├── RainbowFireController.cs
│   ├── RainbowFireExample.cs
│   ├── RainbowFireTextureGenerator.cs
│   └── Editor/
│       └── RainbowFireEditorUtility.cs
├── Shaders/
│   └── RainbowFire.shader
```

**Note**: Unity will automatically generate `.meta` files for all imported assets.

### 2. Verify Shader Import

After importing:
1. Check Unity Console for any shader compilation errors
2. Go to `Edit > Project Settings > Graphics`
3. Verify URP is set as the active pipeline
4. The shader should appear in shader list as `URP/Particles/RainbowFire`

### 3. Create Your First Rainbow Fire

#### Method A: Using Editor Menu (Fastest)
```
GameObject > Effects > Create Rainbow Fire
```

#### Method B: Manual Creation
1. Create empty GameObject: `GameObject > Create Empty`
2. Name it "RainbowFire"
3. Add Particle System: `Add Component > Particle System`
4. Add Controller: `Add Component > RainbowFireController`
5. Create Material:
   - `Assets > Create > Material`
   - Name: "RainbowFireMaterial"
   - Shader: `URP/Particles/RainbowFire`
6. Assign material to Particle System Renderer

### 4. Configure for Your Scene

Adjust these parameters based on your needs:

**For Small Campfire-like Effect:**
```
Height: 1.5
Width: 0.8
Breadth: 0.8
Intensity: 60
Slow Motion Speed: 0.4
```

**For Large Magical Portal:**
```
Height: 5
Width: 3
Breadth: 3
Intensity: 250
Slow Motion Speed: 0.6
Enable Heat Shimmer: true
```

**For Mobile Game (Performance):**
```
Height: 2
Width: 1
Breadth: 1
Intensity: 80
Enable Heat Shimmer: false
Max Particles: 500 (in Particle System Main module)
```

### 5. Script Integration

If you want to control the fire from your own scripts:

```csharp
using UnityEngine;

public class MyGameController : MonoBehaviour
{
    public RainbowFireController rainbowFire;
    
    void Start()
    {
        // Get reference if not assigned in inspector
        if (rainbowFire == null)
        {
            rainbowFire = FindObjectOfType<RainbowFireController>();
        }
        
        // Customize the fire
        rainbowFire.height = 3f;
        rainbowFire.intensity = 150f;
    }
    
    void OnPlayerAction()
    {
        // Trigger a fire burst on player action
        rainbowFire.TriggerBurst(50);
    }
    
    void OnGameEvent()
    {
        // Change fire colors dynamically
        Gradient newGradient = CreateCustomGradient();
        rainbowFire.SetRainbowGradient(newGradient);
    }
    
    Gradient CreateCustomGradient()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.red, 0f),
                new GradientColorKey(Color.yellow, 0.5f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            }
        );
        return gradient;
    }
}
```

### 6. Create Prefab Variants

To reuse the effect throughout your project:

1. Set up a Rainbow Fire GameObject with desired settings
2. Drag it from Hierarchy to Project window (creates Prefab)
3. Create variants for different use cases:
   - `RainbowFire_Small.prefab` - Subtle effect
   - `RainbowFire_Large.prefab` - Dramatic effect
   - `RainbowFire_Cool.prefab` - Blue/cyan colors
   - `RainbowFire_Warm.prefab` - Red/orange colors

### 7. Optimize for Your Target Platform

#### Mobile Optimization Checklist
- [ ] Set Intensity < 100
- [ ] Disable Heat Shimmer
- [ ] Set Max Particles to 300-500
- [ ] Use 128x128 or 256x256 textures
- [ ] Test on target device

#### Desktop/Console Optimization
- [x] Can use full settings
- [x] Consider LOD system for multiple fires
- [x] Enable all visual features

### 8. Lighting Integration

The fire emits light visually but doesn't emit actual light. To add real lighting:

```csharp
// Add to RainbowFireController or create separate script
using UnityEngine;

public class FireLighting : MonoBehaviour
{
    public RainbowFireController fireController;
    public Light fireLight;
    
    void Start()
    {
        if (fireLight == null)
        {
            GameObject lightObj = new GameObject("FireLight");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = Vector3.up * (fireController.height * 0.5f);
            
            fireLight = lightObj.AddComponent<Light>();
            fireLight.type = LightType.Point;
            fireLight.range = fireController.width * 3f;
            fireLight.intensity = 2f;
        }
    }
    
    void Update()
    {
        // Flicker effect
        float flicker = Random.Range(0.8f, 1.2f);
        fireLight.intensity = 2f * flicker;
        
        // Change color based on gradient
        float t = Time.time * 0.5f % 1f;
        fireLight.color = fireController.rainbowGradient.Evaluate(t);
    }
}
```

### 9. Audio Integration

Add fire sound effects:

```csharp
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FireAudio : MonoBehaviour
{
    public RainbowFireController fireController;
    public AudioClip fireLoopClip;
    private AudioSource audioSource;
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = fireLoopClip;
        audioSource.loop = true;
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.Play();
    }
    
    void Update()
    {
        // Adjust volume based on intensity
        float normalizedIntensity = fireController.intensity / 200f;
        audioSource.volume = Mathf.Clamp01(normalizedIntensity);
    }
}
```

### 10. Performance Monitoring

Add this script to monitor performance:

```csharp
using UnityEngine;
using UnityEngine.Profiling;

public class FirePerformanceMonitor : MonoBehaviour
{
    public RainbowFireController fireController;
    private ParticleSystem particleSystem;
    
    void Start()
    {
        particleSystem = fireController.GetComponent<ParticleSystem>();
    }
    
    void Update()
    {
        int activeParticles = particleSystem.particleCount;
        
        // Auto-adjust based on performance
        if (Application.targetFrameRate > 0)
        {
            float currentFPS = 1f / Time.deltaTime;
            
            if (currentFPS < Application.targetFrameRate * 0.8f)
            {
                // Reduce intensity if FPS drops
                fireController.intensity *= 0.9f;
                Debug.LogWarning($"Fire intensity reduced due to low FPS: {currentFPS:F1}");
            }
        }
    }
}
```

## Common Integration Scenarios

### Scenario 1: Player Spell Effect
```csharp
public class SpellCaster : MonoBehaviour
{
    public GameObject rainbowFirePrefab;
    private GameObject activeSpell;
    
    public void CastFireSpell(Vector3 targetPosition)
    {
        activeSpell = Instantiate(rainbowFirePrefab, targetPosition, Quaternion.identity);
        var fire = activeSpell.GetComponent<RainbowFireController>();
        fire.height = 2f;
        fire.intensity = 200f;
        
        // Destroy after 3 seconds
        Destroy(activeSpell, 3f);
    }
}
```

### Scenario 2: Environmental Effect
```csharp
public class EnvironmentalFire : MonoBehaviour
{
    public RainbowFireController fire;
    public float dayNightCycle = 0f; // 0-1 value
    
    void Update()
    {
        // Fire more visible at night
        float nightIntensity = Mathf.Lerp(80f, 150f, 1f - dayNightCycle);
        fire.intensity = nightIntensity;
    }
}
```

### Scenario 3: Interactive Fire
```csharp
public class InteractiveFire : MonoBehaviour
{
    public RainbowFireController fire;
    
    void OnMouseDown()
    {
        // User clicks fire - trigger burst
        fire.TriggerBurst(100);
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Something enters fire area
        if (other.CompareTag("Player"))
        {
            fire.intensity *= 1.5f; // Flare up
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            fire.intensity /= 1.5f; // Return to normal
        }
    }
}
```

## Troubleshooting Integration Issues

### Issue: Shader not found after import
**Solution**: 
- Reimport shader: Right-click > Reimport
- Check for compilation errors in Console
- Ensure URP package is installed

### Issue: Performance problems after integration
**Solution**:
- Reduce particle count
- Check if multiple fires are active
- Use profiler to identify bottlenecks
- Consider LOD system

### Issue: Fire doesn't match scene lighting
**Solution**:
- Adjust material emission strength
- Add Point Light component
- Modify color gradient to match scene

### Issue: Fire clips through geometry
**Solution**:
- Enable collision module in Particle System
- Adjust fire position
- Use sorting layers properly

## Best Practices

1. **Prefab Management**: Create prefab variants for different use cases
2. **Object Pooling**: For spawning many fires, use object pooling
3. **LOD System**: Reduce quality based on distance from camera
4. **Testing**: Always test on target platform
5. **Documentation**: Comment your customizations

## Next Steps

- Experiment with different gradient configurations
- Create custom particle textures for unique looks
- Integrate with your game's VFX system
- Add sound effects and lighting
- Profile performance on target devices

## Support Resources

- [Main Documentation](../README.md)
- [Quick Start Guide](../QUICKSTART.md)
- [Technical Details](README.md)
- Unity Documentation: Particle Systems
- Unity Documentation: URP

---

**Happy Integrating!** 🔥🌈
