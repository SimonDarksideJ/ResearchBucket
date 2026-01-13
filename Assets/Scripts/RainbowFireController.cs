using UnityEngine;

/// <summary>
/// Controls a rainbow fire effect with configurable parameters.
/// Designed for Unity 6000.3 with URP, targeting mobile platforms.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class RainbowFireController : MonoBehaviour
{
    [Header("Fire Dimensions")]
    [Tooltip("Height of the fire effect")]
    [Range(0.5f, 10f)]
    public float height = 2f;
    
    [Tooltip("Width of the fire effect")]
    [Range(0.5f, 5f)]
    public float width = 1f;
    
    [Tooltip("Breadth (depth) of the fire effect")]
    [Range(0.5f, 5f)]
    public float breadth = 1f;

    [Header("Fire Behavior")]
    [Tooltip("Intensity of the fire (particle emission rate)")]
    [Range(10f, 500f)]
    public float intensity = 100f;
    
    [Tooltip("Frequency of fire movement and flickering")]
    [Range(0.1f, 5f)]
    public float frequency = 1f;
    
    [Tooltip("Slow motion multiplier (lower = slower)")]
    [Range(0.1f, 2f)]
    public float slowMotionSpeed = 0.5f;

    [Header("Rainbow Colors")]
    [Tooltip("Rainbow gradient for fire colors")]
    public Gradient rainbowGradient;
    
    [Tooltip("Enable color cycling animation")]
    public bool animateColors = true;
    
    [Tooltip("Speed of color cycling")]
    [Range(0.1f, 5f)]
    public float colorCycleSpeed = 1f;

    [Header("Heat Shimmer")]
    [Tooltip("Enable heat shimmer effect above the fire")]
    public bool enableHeatShimmer = true;
    
    [Tooltip("Intensity of heat distortion")]
    [Range(0f, 1f)]
    public float shimmerIntensity = 0.5f;
    
    [Tooltip("Height offset for heat shimmer")]
    [Range(0f, 5f)]
    public float shimmerHeightOffset = 0.5f;

    // Components
    private ParticleSystem fireParticleSystem;
    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.EmissionModule emissionModule;
    private ParticleSystem.ShapeModule shapeModule;
    private ParticleSystem.ColorOverLifetimeModule colorModule;
    private ParticleSystem.VelocityOverLifetimeModule velocityModule;
    private ParticleSystem.SizeOverLifetimeModule sizeModule;
    
    private ParticleSystem heatShimmerParticleSystem;
    private GameObject heatShimmerObject;
    
    private Material fireMaterial;
    private float timeOffset;

    void Awake()
    {
        InitializeComponents();
        SetupRainbowGradient();
    }

    void Start()
    {
        SetupFireParticleSystem();
        SetupHeatShimmer();
        ApplySettings();
    }

    void Update()
    {
        UpdateFireEffect();
        UpdateColorAnimation();
    }

    /// <summary>
    /// Initializes particle system components
    /// </summary>
    private void InitializeComponents()
    {
        fireParticleSystem = GetComponent<ParticleSystem>();
        if (fireParticleSystem == null)
        {
            Debug.LogError("RainbowFireController requires a ParticleSystem component!");
            return;
        }

        mainModule = fireParticleSystem.main;
        emissionModule = fireParticleSystem.emission;
        shapeModule = fireParticleSystem.shape;
        colorModule = fireParticleSystem.colorOverLifetime;
        velocityModule = fireParticleSystem.velocityOverLifetime;
        sizeModule = fireParticleSystem.sizeOverLifetime;
        
        // Get or create material
        var renderer = fireParticleSystem.GetComponent<ParticleSystemRenderer>();
        if (renderer != null && renderer.sharedMaterial != null)
        {
            fireMaterial = new Material(renderer.sharedMaterial);
            renderer.material = fireMaterial;
        }
    }

    /// <summary>
    /// Sets up default rainbow gradient if not configured
    /// </summary>
    private void SetupRainbowGradient()
    {
        if (rainbowGradient == null || rainbowGradient.colorKeys.Length == 0)
        {
            rainbowGradient = new Gradient();
            
            // Create full rainbow spectrum
            GradientColorKey[] colorKeys = new GradientColorKey[7];
            colorKeys[0] = new GradientColorKey(new Color(1f, 0f, 0f), 0f);      // Red
            colorKeys[1] = new GradientColorKey(new Color(1f, 0.5f, 0f), 0.166f); // Orange
            colorKeys[2] = new GradientColorKey(new Color(1f, 1f, 0f), 0.333f);   // Yellow
            colorKeys[3] = new GradientColorKey(new Color(0f, 1f, 0f), 0.5f);     // Green
            colorKeys[4] = new GradientColorKey(new Color(0f, 0f, 1f), 0.666f);   // Blue
            colorKeys[5] = new GradientColorKey(new Color(0.5f, 0f, 1f), 0.833f); // Indigo
            colorKeys[6] = new GradientColorKey(new Color(1f, 0f, 1f), 1f);       // Violet
            
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);
            
            rainbowGradient.SetKeys(colorKeys, alphaKeys);
        }
    }

    /// <summary>
    /// Configures the main fire particle system
    /// </summary>
    private void SetupFireParticleSystem()
    {
        // Main module settings
        mainModule.startLifetime = new ParticleSystem.MinMaxCurve(1f, 3f);
        mainModule.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2f);
        mainModule.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        mainModule.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        mainModule.gravityModifier = -0.2f; // Slight upward drift
        mainModule.simulationSpeed = slowMotionSpeed;
        mainModule.maxParticles = 1000;

        // Shape module - box emitter at base
        shapeModule.enabled = true;
        shapeModule.shapeType = ParticleSystemShapeType.Box;
        shapeModule.position = Vector3.zero;
        
        // Color over lifetime
        colorModule.enabled = true;
        colorModule.color = new ParticleSystem.MinMaxGradient(rainbowGradient);
        
        // Velocity over lifetime for upward movement
        velocityModule.enabled = true;
        velocityModule.space = ParticleSystemSimulationSpace.Local;
        velocityModule.y = new ParticleSystem.MinMaxCurve(1f, 3f);
        
        // Size over lifetime for fire taper
        sizeModule.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(0.5f, 0.8f);
        sizeCurve.AddKey(1f, 0.2f);
        sizeModule.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
    }

    /// <summary>
    /// Sets up the heat shimmer effect
    /// </summary>
    private void SetupHeatShimmer()
    {
        if (!enableHeatShimmer)
            return;

        // Create heat shimmer particle system if it doesn't exist
        if (heatShimmerObject == null)
        {
            heatShimmerObject = new GameObject("HeatShimmer");
            heatShimmerObject.transform.SetParent(transform);
            heatShimmerObject.transform.localPosition = Vector3.up * shimmerHeightOffset;
            
            heatShimmerParticleSystem = heatShimmerObject.AddComponent<ParticleSystem>();
            
            var shimmerMain = heatShimmerParticleSystem.main;
            shimmerMain.startLifetime = new ParticleSystem.MinMaxCurve(2f, 4f);
            shimmerMain.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.5f);
            shimmerMain.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
            shimmerMain.startColor = new Color(1f, 1f, 1f, 0.1f);
            shimmerMain.gravityModifier = -0.1f;
            shimmerMain.simulationSpeed = slowMotionSpeed * 0.7f;
            shimmerMain.maxParticles = 50;
            
            var shimmerEmission = heatShimmerParticleSystem.emission;
            shimmerEmission.rateOverTime = 10f;
            
            var shimmerShape = heatShimmerParticleSystem.shape;
            shimmerShape.enabled = true;
            shimmerShape.shapeType = ParticleSystemShapeType.Box;
            shimmerShape.scale = new Vector3(width * 0.8f, 0.1f, breadth * 0.8f);
            
            // Renderer settings for shimmer
            var shimmerRenderer = heatShimmerParticleSystem.GetComponent<ParticleSystemRenderer>();
            shimmerRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            shimmerRenderer.sortingFudge = -10; // Render behind fire
        }
        
        heatShimmerObject.SetActive(enableHeatShimmer);
    }

    /// <summary>
    /// Applies current settings to particle system
    /// </summary>
    private void ApplySettings()
    {
        if (fireParticleSystem == null)
            return;

        // Update dimensions
        shapeModule.scale = new Vector3(width, 0.1f, breadth);
        
        // Update emission rate based on intensity
        emissionModule.rateOverTime = intensity;
        
        // Update simulation speed for slow motion
        mainModule.simulationSpeed = slowMotionSpeed * frequency;
        
        // Update velocity for height control
        velocityModule.y = new ParticleSystem.MinMaxCurve(height * 0.3f, height * 0.6f);
        
        // Update heat shimmer if enabled
        if (enableHeatShimmer && heatShimmerObject != null)
        {
            heatShimmerObject.transform.localPosition = Vector3.up * (height * 0.5f + shimmerHeightOffset);
            var shimmerShape = heatShimmerParticleSystem.shape;
            shimmerShape.scale = new Vector3(width * 0.8f, 0.1f, breadth * 0.8f);
            
            if (fireMaterial != null)
            {
                fireMaterial.SetFloat("_ShimmerIntensity", shimmerIntensity);
            }
        }
    }

    /// <summary>
    /// Updates fire effect parameters in real-time
    /// </summary>
    private void UpdateFireEffect()
    {
        ApplySettings();
    }

    /// <summary>
    /// Animates color cycling if enabled
    /// </summary>
    private void UpdateColorAnimation()
    {
        if (!animateColors || rainbowGradient == null)
            return;

        timeOffset += Time.deltaTime * colorCycleSpeed * 0.1f;
        
        // Create animated gradient by shifting colors
        if (fireMaterial != null)
        {
            fireMaterial.SetFloat("_ColorOffset", timeOffset);
        }
    }

    /// <summary>
    /// Validates and applies settings when changed in editor
    /// </summary>
    private void OnValidate()
    {
        if (Application.isPlaying && fireParticleSystem != null)
        {
            ApplySettings();
            
            if (heatShimmerObject != null)
            {
                heatShimmerObject.SetActive(enableHeatShimmer);
            }
        }
    }

    /// <summary>
    /// Public method to update rainbow gradient at runtime
    /// </summary>
    public void SetRainbowGradient(Gradient newGradient)
    {
        if (newGradient != null)
        {
            rainbowGradient = newGradient;
            colorModule.color = new ParticleSystem.MinMaxGradient(rainbowGradient);
        }
    }

    /// <summary>
    /// Public method to trigger fire intensity burst
    /// </summary>
    public void TriggerBurst(int particleCount = 50)
    {
        if (fireParticleSystem != null)
        {
            fireParticleSystem.Emit(particleCount);
        }
    }
}
