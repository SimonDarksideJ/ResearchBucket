using UnityEngine;

/// <summary>
/// Generates a particle texture for the rainbow fire effect at runtime.
/// Can be used as an editor utility or runtime generator.
/// </summary>
public class RainbowFireTextureGenerator : MonoBehaviour
{
    [Header("Texture Settings")]
    [Tooltip("Resolution of the generated texture (power of 2)")]
    public int textureSize = 256;
    
    [Tooltip("Softness of the particle edges (0 = hard, 1 = very soft)")]
    [Range(0f, 1f)]
    public float edgeSoftness = 0.8f;
    
    [Tooltip("Core brightness (1 = white center)")]
    [Range(0f, 2f)]
    public float coreBrightness = 1.5f;
    
    [Tooltip("Generate texture on start")]
    public bool generateOnStart = false;

    /// <summary>
    /// Generates a radial gradient particle texture
    /// </summary>
    public Texture2D GenerateParticleTexture()
    {
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, true);
        texture.name = "GeneratedFireParticle";
        
        float center = textureSize * 0.5f;
        float maxDistance = center * Mathf.Sqrt(2f);
        
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                // Calculate distance from center
                float dx = x - center;
                float dy = y - center;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float normalizedDistance = distance / center;
                
                // Create radial gradient with soft falloff
                float alpha = 1f - Mathf.Pow(normalizedDistance, 1f - edgeSoftness);
                alpha = Mathf.Clamp01(alpha);
                
                // Apply core brightness boost
                float brightness = Mathf.Lerp(coreBrightness, 1f, normalizedDistance);
                brightness = Mathf.Clamp01(brightness);
                
                // Add subtle noise for organic feel
                float noise = Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 0.1f;
                brightness += noise;
                
                // Set pixel color (white with varying alpha)
                Color pixelColor = new Color(brightness, brightness, brightness, alpha);
                texture.SetPixel(x, y, pixelColor);
            }
        }
        
        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        
        return texture;
    }

    /// <summary>
    /// Generates a noise texture for distortion effects
    /// </summary>
    public Texture2D GenerateNoiseTexture()
    {
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, true);
        texture.name = "GeneratedFireNoise";
        
        float scale = 0.1f;
        
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                // Generate multi-octave Perlin noise
                float noise = 0f;
                float amplitude = 1f;
                float frequency = 1f;
                
                for (int octave = 0; octave < 4; octave++)
                {
                    noise += Mathf.PerlinNoise(x * scale * frequency, y * scale * frequency) * amplitude;
                    amplitude *= 0.5f;
                    frequency *= 2f;
                }
                
                noise = Mathf.Clamp01(noise);
                
                Color pixelColor = new Color(noise, noise, noise, 1f);
                texture.SetPixel(x, y, pixelColor);
            }
        }
        
        texture.Apply();
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;
        
        return texture;
    }

    /// <summary>
    /// Applies the generated texture to the particle system renderer
    /// </summary>
    public void ApplyToParticleSystem()
    {
        var particleSystem = GetComponent<ParticleSystem>();
        if (particleSystem == null)
        {
            Debug.LogWarning("No ParticleSystem component found on this GameObject!");
            return;
        }

        var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        if (renderer == null)
        {
            Debug.LogWarning("No ParticleSystemRenderer component found!");
            return;
        }

        Texture2D particleTexture = GenerateParticleTexture();
        
        if (renderer.material != null)
        {
            Material newMaterial = new Material(renderer.material);
            newMaterial.mainTexture = particleTexture;
            renderer.material = newMaterial;
            
            Debug.Log("Generated and applied particle texture to material.");
        }
        else
        {
            Debug.LogWarning("No material found on ParticleSystemRenderer!");
        }
    }

    void Start()
    {
        if (generateOnStart)
        {
            ApplyToParticleSystem();
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Saves the generated texture as a PNG file in the Assets folder
    /// </summary>
    public void SaveTextureToFile(string fileName = "FireParticle.png")
    {
        Texture2D texture = GenerateParticleTexture();
        byte[] bytes = texture.EncodeToPNG();
        
        string path = System.IO.Path.Combine(Application.dataPath, "Textures", fileName);
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
        System.IO.File.WriteAllBytes(path, bytes);
        
        Debug.Log($"Texture saved to: {path}");
        UnityEditor.AssetDatabase.Refresh();
    }
#endif
}
