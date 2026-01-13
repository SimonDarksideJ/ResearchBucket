# Troubleshooting Guide - Rainbow Fire Effect

Common issues and solutions for the Rainbow Fire effect system.

## Table of Contents
1. [Installation Issues](#installation-issues)
2. [Shader Problems](#shader-problems)
3. [Visual Issues](#visual-issues)
4. [Performance Issues](#performance-issues)
5. [Script Errors](#script-errors)
6. [Platform-Specific Issues](#platform-specific-issues)
7. [Integration Issues](#integration-issues)

---

## Installation Issues

### Issue: Shader not found after import
**Symptoms**: Material shows pink/magenta color, shader dropdown shows "Missing (URP/Particles/RainbowFire)"

**Solutions**:
1. Check Console for shader compilation errors
2. Verify Unity version is 6000.3 or later
3. Ensure URP package is installed:
   - `Window > Package Manager > Universal RP`
4. Reimport shader:
   - Right-click RainbowFire.shader > Reimport
5. Refresh asset database:
   - `Assets > Refresh` or `Ctrl+R` (Windows) / `Cmd+R` (Mac)

### Issue: Scripts show compilation errors
**Symptoms**: Red errors in Console, scripts won't attach to GameObjects

**Solutions**:
1. Check Unity version compatibility (6000.3+)
2. Verify all script files are in correct folders
3. Look for missing using statements
4. Clear Unity cache:
   - Close Unity
   - Delete Library folder
   - Reopen project
5. Reimport all scripts

### Issue: Editor menu items not appearing
**Symptoms**: `GameObject > Effects > Create Rainbow Fire` menu not visible

**Solutions**:
1. Verify RainbowFireEditorUtility.cs is in Assets/Scripts/Editor/ folder
2. Check script has #if UNITY_EDITOR directive
3. Wait for Unity to compile scripts
4. Restart Unity Editor

---

## Shader Problems

### Issue: Fire appears completely white or single color
**Symptoms**: No rainbow colors visible, just white or single colored particles

**Solutions**:
1. Check Color Over Lifetime module is enabled in Particle System
2. Verify gradient has multiple color keys:
   - Inspector > RainbowFireController > Rainbow Gradient
3. Ensure material has correct shader selected
4. Check _ColorIntensity isn't too high:
   - Material > Color Intensity (should be 1-2)
5. Verify particle color is white in Particle System Main module

### Issue: Fire is invisible or very dim
**Symptoms**: Can barely see particles or nothing visible at all

**Solutions**:
1. Increase Emission Strength in material:
   - Material > Emission Strength (try 2-5)
2. Check particle alpha in Color Over Lifetime
3. Verify rendering layer/sorting:
   - Particle System Renderer > Sorting Layer
4. Ensure camera can see particle layer:
   - Camera > Culling Mask
5. Check if particles are behind other geometry
6. Increase particle Start Size in Main module

### Issue: Distortion/shimmer looks wrong or broken
**Symptoms**: Particles have weird artifacts, visual glitches

**Solutions**:
1. Reduce Distortion Strength:
   - Material > Distortion Strength (try 0.05-0.2)
2. Lower Distortion Speed:
   - Material > Distortion Speed (try 0.5-1.5)
3. Check texture is assigned correctly
4. Verify shader is using correct texture sampler
5. Test without distortion (set to 0) to isolate issue

### Issue: Shader compilation errors
**Symptoms**: Console shows shader errors, pink materials

**Solutions**:
1. Check URP version compatibility
2. Verify shader syntax for Unity 6000.3
3. Look for missing includes:
   ```hlsl
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   ```
4. Ensure shader target is supported:
   ```hlsl
   #pragma target 3.0
   ```
5. Check for typos in shader properties

---

## Visual Issues

### Issue: Fire doesn't look like fire
**Symptoms**: Effect looks like floating particles, not cohesive fire

**Solutions**:
1. Adjust particle lifetime:
   - Particle System > Main > Start Lifetime (try 2-3)
2. Increase emission rate:
   - RainbowFireController > Intensity (try 100-200)
3. Enable Size Over Lifetime with tapering curve
4. Adjust velocity for upward movement
5. Increase Start Size variation
6. Enable and configure Noise module for organic movement

### Issue: Colors transition too abruptly
**Symptoms**: Hard color changes, no smooth gradient

**Solutions**:
1. Add more color keys to gradient:
   - Inspector > Gradient > Add keys between existing ones
2. Smooth gradient mode:
   - Inspector > Gradient > Mode: Blend
3. Enable Color Cycling:
   - RainbowFireController > Animate Colors = true
4. Adjust Color Cycle Speed to slower value
5. Check particle lifetime allows color progression

### Issue: Heat shimmer not visible
**Symptoms**: Shimmer enabled but no visible effect

**Solutions**:
1. Increase Shimmer Intensity:
   - RainbowFireController > Shimmer Intensity (try 0.7-1.0)
2. Verify shimmer GameObject is active:
   - Check Hierarchy for HeatShimmer child object
3. Adjust shimmer height offset:
   - RainbowFireController > Shimmer Height Offset
4. Check shimmer particles are emitting:
   - Select HeatShimmer > Particle System > Emission
5. Increase shimmer particle size and count

### Issue: Particles clip through geometry
**Symptoms**: Fire particles visible through walls/floors

**Solutions**:
1. Enable Collision module:
   - Particle System > Collision > Enable
   - Type: World
   - Add colliders to geometry
2. Adjust sorting order:
   - Renderer > Sorting Fudge
3. Use proper rendering layers
4. Enable depth testing:
   - Material > Z Test: LEqual
5. Adjust particle spawn position

---

## Performance Issues

### Issue: Low FPS with fire effect
**Symptoms**: Frame rate drops when fire is active

**Solutions**:
1. Reduce particle count:
   - RainbowFireController > Intensity (try < 100)
   - Particle System > Main > Max Particles (try < 500)
2. Disable heat shimmer:
   - RainbowFireController > Enable Heat Shimmer = false
3. Reduce texture size:
   - Texture import settings > Max Size: 128 or 256
4. Lower distortion calculations:
   - Material > Distortion Strength = 0
5. Reduce active fire instances:
   - Use object pooling
   - Implement LOD system

### Issue: Memory usage too high
**Symptoms**: Game uses excessive RAM

**Solutions**:
1. Use smaller particle textures (128x128)
2. Reduce Max Particles setting
3. Limit number of simultaneous fires
4. Enable particle system culling
5. Implement object pooling for reuse

### Issue: GPU bottleneck
**Symptoms**: GPU usage very high, thermal throttling

**Solutions**:
1. Reduce overdraw:
   - Lower particle size
   - Reduce particle count
   - Use simpler shader variant
2. Optimize material:
   - Reduce distortion complexity
   - Disable shimmer on mobile
3. Use LOD system based on distance
4. Reduce fill rate:
   - Smaller particles
   - Lower alpha blending

---

## Script Errors

### Issue: NullReferenceException on start
**Symptoms**: Console shows null reference error when scene starts

**Solutions**:
1. Verify all required components are attached:
   - ParticleSystem must be on same GameObject
2. Check initialization order:
   - Use Awake() for setup, Start() for configuration
3. Verify prefab references are assigned
4. Check if ParticleSystem is disabled at start
5. Add null checks in custom scripts:
   ```csharp
   if (fireController == null)
   {
       fireController = GetComponent<RainbowFireController>();
   }
   ```

### Issue: Parameters don't update at runtime
**Symptoms**: Changing values in inspector has no effect while playing

**Solutions**:
1. Ensure OnValidate() is implemented
2. Check if values are cached and not refreshed
3. Verify Update() or ApplySettings() is being called
4. Look for readonly fields or properties
5. Check if particle system is actually running

### Issue: Gradient not applying correctly
**Symptoms**: Custom gradient doesn't show on particles

**Solutions**:
1. Verify Color Over Lifetime module is enabled
2. Check gradient is not null
3. Ensure SetRainbowGradient() is called after changes
4. Verify gradient has both color and alpha keys
5. Check particle lifetime allows gradient progression

---

## Platform-Specific Issues

### Mobile (iOS/Android)

**Issue**: Effect too slow on mobile
**Solutions**:
- Reduce intensity to < 100
- Disable heat shimmer
- Lower max particles to 300-500
- Use 128x128 textures
- Simplify shader (reduce distortion)

**Issue**: Effect doesn't appear on mobile
**Solutions**:
- Check graphics API compatibility
- Verify URP rendering on device
- Test shader model support
- Check texture format compatibility
- Verify particle system isn't culled

### Desktop (Windows/Mac/Linux)

**Issue**: Performance too variable
**Solutions**:
- Implement quality settings
- Use VSync appropriately
- Add LOD system
- Profile with Unity Profiler
- Check background processes

### Console (PlayStation/Xbox/Switch)

**Issue**: Platform-specific rendering issues
**Solutions**:
- Test on actual hardware
- Check platform-specific URP settings
- Verify shader compatibility
- Adjust for platform performance profile

---

## Integration Issues

### Issue: Fire conflicts with existing particle systems
**Symptoms**: Other particle systems behave strangely when fire is active

**Solutions**:
1. Check sorting layers don't conflict
2. Verify no material sharing between systems
3. Use unique materials for each effect
4. Check particle collision settings
5. Ensure no global simulation space conflicts

### Issue: Fire doesn't work with post-processing
**Symptoms**: Fire looks wrong with post-processing effects active

**Solutions**:
1. Adjust bloom settings in post-processing
2. Check HDR settings on camera
3. Verify rendering order
4. Adjust particle emission strength
5. Test with individual post-processing effects

### Issue: Prefab instantiation issues
**Symptoms**: Instantiated fires don't work correctly

**Solutions**:
1. Ensure all dependencies are in prefab
2. Check material references aren't lost
3. Verify scripts initialize properly
4. Test prefab isolation
5. Check for missing component references

---

## Debug Checklist

When troubleshooting, systematically check:

- [ ] Unity version is 6000.3 or later
- [ ] URP package is installed and active
- [ ] Shader compiles without errors
- [ ] Material assigned to Particle System Renderer
- [ ] ParticleSystem component present on GameObject
- [ ] RainbowFireController script attached
- [ ] Particle emission is enabled and running
- [ ] Camera can see particle layer
- [ ] No console errors
- [ ] Scene lighting doesn't hide particles
- [ ] Particle texture is assigned
- [ ] Gradient has color keys
- [ ] Performance settings appropriate for platform

---

## Getting Additional Help

If issues persist:

1. **Check Documentation**:
   - README.md for overview
   - Assets/README.md for technical details
   - QUICKSTART.md for setup guide

2. **Review Examples**:
   - RainbowFireExample.cs shows working implementations
   - CONFIGURATION_PRESETS.md has tested settings

3. **Use Unity Profiler**:
   - `Window > Analysis > Profiler`
   - Check rendering, scripts, and memory

4. **Test Systematically**:
   - Create minimal test scene
   - Add components one at a time
   - Isolate the issue

5. **Check Unity Documentation**:
   - Particle Systems
   - URP rendering
   - Shader programming

---

## Common Error Messages and Solutions

### "Shader is not supported on this GPU"
- Update graphics drivers
- Check shader model compatibility
- Use fallback shader

### "Particle system exceeds safe limit"
- Reduce Max Particles
- Lower emission rate
- Check for infinite emission

### "Material doesn't have property '_PropertyName'"
- Verify correct shader assigned
- Check shader property spelling
- Update material after shader changes

### "DLL not found" or "Assembly reference missing"
- Verify URP package installed
- Check Unity version compatibility
- Reimport project

---

**Still having issues?** Review the inline code comments in the scripts for additional guidance.
