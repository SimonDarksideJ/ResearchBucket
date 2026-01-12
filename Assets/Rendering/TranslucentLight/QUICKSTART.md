# Quick Start Guide - Translucent Light Shader

Get up and running with the Translucent Light shader in 5 minutes!

## Prerequisites

- Unity 6000.3 LTS (or later)
- Universal Render Pipeline (URP) configured
- Mobile build target (iOS/Android) or standalone

## Step 1: Create a Material

1. In Unity Project window, navigate to: `Assets/Rendering/TranslucentLight/Materials/`
2. Right-click and select: **Create > Material**
3. Name it: `MyTranslucentLight`
4. In the Inspector, click the **Shader** dropdown
5. Select: **Custom/URP/TranslucentLight**

You can also duplicate the provided `TranslucentLight_Default.mat` sample.

## Step 2: Apply to Your Model

1. Select your 3D model in the Hierarchy
2. Find the **Renderer** component (MeshRenderer or SkinnedMeshRenderer)
3. Drag your new material onto the **Materials** section
4. Your model should now have a translucent glow!

## Step 3: Add Controller Script

1. With your model selected, click **Add Component**
2. Search for: **Translucent Light Controller**
3. The component will appear with default settings

## Step 4: Configure the Effect

### Basic Settings (Start Here!)

**Color Gradient**
- Click the gradient bar to open the Gradient Editor
- Set colors for your effect (e.g., cyan to blue for energy)

**Emission Intensity** (slider: 0-10)
- Controls overall brightness
- Start with: `1.0` for subtle, `3.0` for bright

**Core Glow Intensity** (slider: 0-10)
- Controls center light intensity
- Start with: `2.0` for gentle, `5.0` for powerful

**Transparency** (slider: 0-1)
- Controls see-through level
- Start with: `0.5` for balanced effect

### Animation Settings

**Auto Pulse**
- Check this box to enable automatic pulsing
- Set **Pulse Interval**: `1.0` (pulses every second)
- Set **Pulse Duration**: `0.5` (half-second pulse)
- Set **Pulse Max Intensity**: `3.0` (3x brighter at peak)

**Auto Breath**
- Check this box to enable breathing effect
- Set **Breath Interval**: `1.0` (breathes every second)
- Set **Breath Duration**: `2.0` (2-second breath cycle)
- Set **Breath Max Intensity**: `2.0` (2x brighter at peak)

## Step 5: Test It!

### In Editor (Play Mode)

1. Click **Play** button
2. Watch your model pulse or breathe!
3. In the Inspector, look for **Runtime Controls** section
4. Click buttons to test:
   - **Pulse** - Trigger single pulse
   - **Breath** - Trigger single breath
   - **Report Environment** - See debug info in Console

### From Code

```csharp
using ResearchBucket.Rendering.TranslucentLight;

public class MyScript : MonoBehaviour
{
    public TranslucentLightController myLight;
    
    void Update()
    {
        // Press Space to pulse
        if (Input.GetKeyDown(KeyCode.Space))
        {
            myLight.Pulse();
        }
    }
}
```

## Common Configurations

### Collectible Item (Attention-Grabbing)
```
Emission Intensity: 1.5
Core Glow Intensity: 3.0
Transparency: 0.4
Auto Pulse: ✓
Pulse Interval: 1.0
Color: Yellow-Gold gradient
```

### Power Core (Steady Energy)
```
Emission Intensity: 2.0
Core Glow Intensity: 5.0
Transparency: 0.6
Auto Breath: ✓
Breath Duration: 3.0
Color: Cyan-Blue gradient
```

### Interactive Object (Subtle)
```
Emission Intensity: 0.5
Core Glow Intensity: 1.0
Transparency: 0.3
Auto Pulse: ✗ (manual trigger)
Auto Breath: ✗
Color: Green gradient
```

## Performance Tips

### For Many Objects (100+)

1. Add a **TranslucentLightManager** to your scene:
   - Create empty GameObject
   - Add Component: **Translucent Light Manager**
   
2. Configure manager:
   - Enable Batching: ✓
   - Target Update Rate: `30`
   - Max Lights Per Frame: `10`

### Mobile Optimization

- Keep Emission Intensity under 5.0
- Use Auto Pulse OR Auto Breath, not both
- Limit to 50-100 active lights per scene
- Consider LOD systems for distant objects

## Troubleshooting

**Can't see the effect?**
- Ensure URP is active (Edit > Project Settings > Graphics)
- Check material shader is `Custom/URP/TranslucentLight`
- Increase Emission Intensity and Core Glow Intensity
- Check camera is rendering Transparent layer

**Performance issues?**
- Add TranslucentLightManager to your scene
- Reduce number of active lights
- Lower update rates in manager
- Disable auto effects on distant objects

**Animations not working?**
- Ensure you're in Play mode
- Check Auto Pulse/Breath is enabled in Inspector
- Verify pulse/breath intervals aren't too long
- Use Context Menu "Report Environment" to debug

## Next Steps

- Read the full **README.md** for advanced features
- Check **TranslucentLightExample.cs** for code samples
- Experiment with the gradient editor for custom colors
- Try combining with Unity's particle systems!

## Support

For more information, see the complete documentation in:
`Assets/Rendering/TranslucentLight/README.md`

---

**Need help?** Right-click the component and select **"Report Environment"** to see current settings in the Console.
