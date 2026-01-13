# Rainbow Fire Particle Texture

This directory should contain a soft, radial gradient particle texture.
For Unity 6000.3, use a 256x256 PNG texture with:
- White center fading to transparent edges
- Soft, circular gradient
- Alpha channel for transparency

Recommended texture settings in Unity:
- Texture Type: Default
- Alpha Source: Input Texture Alpha
- Alpha Is Transparency: Enabled
- Wrap Mode: Clamp
- Filter Mode: Bilinear
- Max Size: 256 (for mobile optimization)

You can create this texture using:
1. Unity's built-in particle textures (Default-Particle)
2. Photoshop/GIMP with radial gradient tool
3. Procedural generation in Unity
4. The provided RainbowFireTextureGenerator.cs script

For immediate use, Unity's "Default-Particle" texture works well with this shader.
