# Usage Examples - Rainbow Fire Effect

Quick code snippets and common usage patterns for the Rainbow Fire effect.

## Table of Contents
1. [Basic Setup](#basic-setup)
2. [Parameter Adjustment](#parameter-adjustment)
3. [Custom Gradients](#custom-gradients)
4. [Runtime Control](#runtime-control)
5. [Animation Examples](#animation-examples)
6. [Integration Patterns](#integration-patterns)

---

## Basic Setup

### Using Editor Menu (Fastest)
```
1. GameObject > Effects > Create Rainbow Fire
2. Done! Fire is ready to use.
```

### Manual Setup in Code
```csharp
using UnityEngine;

public class FireSetup : MonoBehaviour
{
    void Start()
    {
        // Create GameObject
        GameObject fireObject = new GameObject("RainbowFire");
        
        // Add Particle System
        ParticleSystem ps = fireObject.AddComponent<ParticleSystem>();
        
        // Add Controller
        RainbowFireController controller = fireObject.AddComponent<RainbowFireController>();
        
        // Position the fire
        fireObject.transform.position = new Vector3(0, 0.5f, 0);
    }
}
```

---

## Parameter Adjustment

### Simple Parameter Changes
```csharp
RainbowFireController fire = GetComponent<RainbowFireController>();

// Adjust size
fire.height = 3f;
fire.width = 1.5f;
fire.breadth = 1.5f;

// Adjust behavior
fire.intensity = 150f;
fire.frequency = 1.2f;
fire.slowMotionSpeed = 0.4f;

// Toggle heat shimmer
fire.enableHeatShimmer = true;
fire.shimmerIntensity = 0.7f;
```

### Preset Configurations
```csharp
// Small campfire
fire.height = 1.5f;
fire.width = 0.8f;
fire.intensity = 60f;
fire.slowMotionSpeed = 0.4f;

// Large magical portal
fire.height = 5f;
fire.width = 3f;
fire.intensity = 250f;
fire.enableHeatShimmer = true;
fire.shimmerIntensity = 0.8f;
```

---

## Custom Gradients

### Create Blue Fire
```csharp
Gradient blueGradient = new Gradient();

GradientColorKey[] colorKeys = new GradientColorKey[4];
colorKeys[0] = new GradientColorKey(new Color(0f, 0.5f, 1f), 0f);    // Deep blue
colorKeys[1] = new GradientColorKey(new Color(0f, 1f, 1f), 0.33f);   // Cyan
colorKeys[2] = new GradientColorKey(new Color(0.5f, 1f, 0.8f), 0.66f); // Light cyan
colorKeys[3] = new GradientColorKey(Color.white, 1f);                // White

GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
alphaKeys[0] = new GradientAlphaKey(1f, 0f);
alphaKeys[1] = new GradientAlphaKey(1f, 1f);

blueGradient.SetKeys(colorKeys, alphaKeys);
fire.SetRainbowGradient(blueGradient);
```

### Create Fire-Like Gradient (Red/Orange/Yellow)
```csharp
Gradient warmGradient = new Gradient();

GradientColorKey[] colorKeys = new GradientColorKey[4];
colorKeys[0] = new GradientColorKey(new Color(0.5f, 0f, 0f), 0f);  // Dark red
colorKeys[1] = new GradientColorKey(Color.red, 0.33f);              // Red
colorKeys[2] = new GradientColorKey(new Color(1f, 0.5f, 0f), 0.66f); // Orange
colorKeys[3] = new GradientColorKey(Color.yellow, 1f);              // Yellow

GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
alphaKeys[0] = new GradientAlphaKey(1f, 0f);
alphaKeys[1] = new GradientAlphaKey(0.8f, 1f);

warmGradient.SetKeys(colorKeys, alphaKeys);
fire.SetRainbowGradient(warmGradient);
```

### Gradient from Color Array
```csharp
Color[] colors = { Color.red, Color.yellow, Color.green, Color.cyan, Color.blue };

Gradient gradient = new Gradient();
GradientColorKey[] colorKeys = new GradientColorKey[colors.Length];

for (int i = 0; i < colors.Length; i++)
{
    float time = i / (float)(colors.Length - 1);
    colorKeys[i] = new GradientColorKey(colors[i], time);
}

GradientAlphaKey[] alphaKeys = new GradientAlphaKey[] {
    new GradientAlphaKey(1f, 0f),
    new GradientAlphaKey(1f, 1f)
};

gradient.SetKeys(colorKeys, alphaKeys);
fire.SetRainbowGradient(gradient);
```

---

## Runtime Control

### Trigger Fire Burst
```csharp
public class FireController : MonoBehaviour
{
    public RainbowFireController fire;
    
    void Update()
    {
        // Press Space to trigger burst
        if (Input.GetKeyDown(KeyCode.Space))
        {
            fire.TriggerBurst(100);
        }
    }
}
```

### Toggle Fire On/Off
```csharp
public class FireToggle : MonoBehaviour
{
    public RainbowFireController fire;
    private ParticleSystem ps;
    
    void Start()
    {
        ps = fire.GetComponent<ParticleSystem>();
    }
    
    public void ToggleFire()
    {
        if (ps.isPlaying)
            ps.Stop();
        else
            ps.Play();
    }
}
```

### Intensity Slider Control
```csharp
using UnityEngine;
using UnityEngine.UI;

public class FireIntensitySlider : MonoBehaviour
{
    public RainbowFireController fire;
    public Slider intensitySlider;
    
    void Start()
    {
        intensitySlider.minValue = 10f;
        intensitySlider.maxValue = 300f;
        intensitySlider.value = fire.intensity;
        intensitySlider.onValueChanged.AddListener(OnIntensityChanged);
    }
    
    void OnIntensityChanged(float value)
    {
        fire.intensity = value;
    }
}
```

---

## Animation Examples

### Pulsing Fire Effect
```csharp
public class PulsingFire : MonoBehaviour
{
    public RainbowFireController fire;
    public float pulseSpeed = 2f;
    public float minIntensity = 60f;
    public float maxIntensity = 180f;
    
    void Update()
    {
        float pulse = Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f;
        fire.intensity = Mathf.Lerp(minIntensity, maxIntensity, pulse);
        fire.height = Mathf.Lerp(1.5f, 3f, pulse);
    }
}
```

### Color Cycling Fire
```csharp
public class ColorCyclingFire : MonoBehaviour
{
    public RainbowFireController fire;
    public float cycleSpeed = 1f;
    private float hueOffset = 0f;
    
    void Update()
    {
        hueOffset += Time.deltaTime * cycleSpeed * 0.1f;
        
        Gradient gradient = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[7];
        
        for (int i = 0; i < 7; i++)
        {
            float hue = (i / 6f + hueOffset) % 1f;
            Color color = Color.HSVToRGB(hue, 1f, 1f);
            colorKeys[i] = new GradientColorKey(color, i / 6f);
        }
        
        gradient.SetKeys(colorKeys, new GradientAlphaKey[] {
            new GradientAlphaKey(1f, 0f),
            new GradientAlphaKey(1f, 1f)
        });
        
        fire.SetRainbowGradient(gradient);
    }
}
```

### Growing Fire
```csharp
public class GrowingFire : MonoBehaviour
{
    public RainbowFireController fire;
    public float growthDuration = 3f;
    private float startTime;
    
    void Start()
    {
        startTime = Time.time;
        fire.height = 0.1f;
        fire.intensity = 10f;
    }
    
    void Update()
    {
        float elapsed = Time.time - startTime;
        float progress = Mathf.Clamp01(elapsed / growthDuration);
        
        fire.height = Mathf.Lerp(0.1f, 3f, progress);
        fire.width = Mathf.Lerp(0.1f, 1.5f, progress);
        fire.breadth = Mathf.Lerp(0.1f, 1.5f, progress);
        fire.intensity = Mathf.Lerp(10f, 150f, progress);
    }
}
```

---

## Integration Patterns

### Player Spell Effect
```csharp
public class FireSpell : MonoBehaviour
{
    public GameObject fireEffectPrefab;
    public float spellDuration = 3f;
    
    public void CastFireSpell(Vector3 targetPosition)
    {
        // Instantiate fire effect
        GameObject fireObj = Instantiate(fireEffectPrefab, targetPosition, Quaternion.identity);
        RainbowFireController fire = fireObj.GetComponent<RainbowFireController>();
        
        // Configure for spell effect
        fire.height = 2f;
        fire.intensity = 200f;
        fire.animateColors = true;
        fire.colorCycleSpeed = 2f;
        
        // Destroy after duration
        Destroy(fireObj, spellDuration);
    }
}
```

### Environmental Fire with Day/Night
```csharp
public class DayNightFire : MonoBehaviour
{
    public RainbowFireController fire;
    public Light fireLight;
    
    [Range(0f, 1f)]
    public float timeOfDay = 0.5f; // 0 = midnight, 0.5 = noon, 1 = midnight
    
    void Update()
    {
        // Fire more visible at night
        bool isNight = timeOfDay < 0.25f || timeOfDay > 0.75f;
        float nightFactor = isNight ? 1f : 0.5f;
        
        fire.intensity = Mathf.Lerp(80f, 150f, nightFactor);
        
        if (fireLight != null)
        {
            fireLight.intensity = Mathf.Lerp(0.5f, 2f, nightFactor);
        }
    }
}
```

### Distance-Based LOD
```csharp
public class FireLOD : MonoBehaviour
{
    public RainbowFireController fire;
    public Camera mainCamera;
    public float maxDistance = 20f;
    
    void Update()
    {
        float distance = Vector3.Distance(mainCamera.transform.position, transform.position);
        float distanceRatio = Mathf.Clamp01(distance / maxDistance);
        
        // Reduce quality with distance
        fire.intensity = Mathf.Lerp(200f, 50f, distanceRatio);
        fire.enableHeatShimmer = distance < maxDistance * 0.5f;
        
        // Disable when too far
        if (distance > maxDistance)
        {
            fire.GetComponent<ParticleSystem>().Stop();
        }
        else if (!fire.GetComponent<ParticleSystem>().isPlaying)
        {
            fire.GetComponent<ParticleSystem>().Play();
        }
    }
}
```

### Interactive Fire (Mouse Click)
```csharp
public class InteractiveFire : MonoBehaviour
{
    public RainbowFireController fire;
    private float normalIntensity;
    
    void Start()
    {
        normalIntensity = fire.intensity;
    }
    
    void OnMouseDown()
    {
        // Flare up on click
        StartCoroutine(FlareUp());
    }
    
    System.Collections.IEnumerator FlareUp()
    {
        fire.TriggerBurst(100);
        fire.intensity = normalIntensity * 2f;
        
        yield return new WaitForSeconds(0.5f);
        
        // Return to normal
        float elapsed = 0f;
        float duration = 1f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fire.intensity = Mathf.Lerp(normalIntensity * 2f, normalIntensity, elapsed / duration);
            yield return null;
        }
    }
}
```

### Object Pool for Multiple Fires
```csharp
using System.Collections.Generic;
using UnityEngine;

public class FirePool : MonoBehaviour
{
    public GameObject firePrefab;
    public int poolSize = 10;
    private Queue<GameObject> firePool = new Queue<GameObject>();
    
    void Start()
    {
        // Pre-instantiate fires
        for (int i = 0; i < poolSize; i++)
        {
            GameObject fire = Instantiate(firePrefab);
            fire.SetActive(false);
            firePool.Enqueue(fire);
        }
    }
    
    public GameObject GetFire(Vector3 position)
    {
        GameObject fire;
        
        if (firePool.Count > 0)
        {
            fire = firePool.Dequeue();
        }
        else
        {
            fire = Instantiate(firePrefab);
        }
        
        fire.transform.position = position;
        fire.SetActive(true);
        fire.GetComponent<ParticleSystem>().Play();
        
        return fire;
    }
    
    public void ReturnFire(GameObject fire)
    {
        fire.GetComponent<ParticleSystem>().Stop();
        fire.SetActive(false);
        firePool.Enqueue(fire);
    }
}
```

---

## Quick Reference

### Common Parameter Ranges

| Parameter | Minimum | Maximum | Recommended |
|-----------|---------|---------|-------------|
| Height | 0.5 | 10 | 2-3 |
| Width | 0.5 | 5 | 1-1.5 |
| Breadth | 0.5 | 5 | 1-1.5 |
| Intensity | 10 | 500 | 100-150 |
| Frequency | 0.1 | 5 | 0.8-1.2 |
| Slow Motion | 0.1 | 2 | 0.4-0.6 |
| Shimmer Intensity | 0 | 1 | 0.5-0.7 |

### Performance Tips
- Keep intensity < 100 for mobile
- Disable shimmer on low-end devices
- Use object pooling for multiple fires
- Implement LOD based on distance
- Test on target device

---

**Need more examples?** Check out RainbowFireExample.cs for additional preset configurations and animations!
