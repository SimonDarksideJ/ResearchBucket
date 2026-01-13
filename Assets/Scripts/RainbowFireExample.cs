using UnityEngine;

/// <summary>
/// Example script demonstrating various use cases of the Rainbow Fire effect.
/// Attach to a GameObject with RainbowFireController to see examples in action.
/// </summary>
[RequireComponent(typeof(RainbowFireController))]
public class RainbowFireExample : MonoBehaviour
{
    [Header("Example Mode")]
    [Tooltip("Select which example to demonstrate")]
    public ExampleMode exampleMode = ExampleMode.Standard;
    
    [Header("Animation Settings")]
    public bool enableAnimation = true;
    public float animationSpeed = 1f;

    private RainbowFireController fireController;
    private float timeAccumulator;

    public enum ExampleMode
    {
        Standard,           // Standard rainbow fire
        PulsingFire,       // Pulsing intensity
        ColorCycling,      // Animated color changes
        DynamicSize,       // Changing dimensions
        IntenseBurst,      // High intensity with bursts
        SubtleFlame,       // Low intensity, gentle
        CoolColors,        // Blue/cyan gradient
        WarmColors,        // Red/orange/yellow gradient
        Custom             // User-defined in inspector
    }

    void Start()
    {
        fireController = GetComponent<RainbowFireController>();
        ApplyExampleMode();
    }

    void Update()
    {
        if (!enableAnimation)
            return;

        timeAccumulator += Time.deltaTime * animationSpeed;

        switch (exampleMode)
        {
            case ExampleMode.PulsingFire:
                AnimatePulsingFire();
                break;
            case ExampleMode.ColorCycling:
                AnimateColorCycling();
                break;
            case ExampleMode.DynamicSize:
                AnimateDynamicSize();
                break;
            case ExampleMode.IntenseBurst:
                AnimateIntenseBurst();
                break;
        }
    }

    /// <summary>
    /// Applies settings based on selected example mode
    /// </summary>
    private void ApplyExampleMode()
    {
        switch (exampleMode)
        {
            case ExampleMode.Standard:
                SetupStandardFire();
                break;
            case ExampleMode.PulsingFire:
                SetupPulsingFire();
                break;
            case ExampleMode.ColorCycling:
                SetupColorCycling();
                break;
            case ExampleMode.DynamicSize:
                SetupDynamicSize();
                break;
            case ExampleMode.IntenseBurst:
                SetupIntenseBurst();
                break;
            case ExampleMode.SubtleFlame:
                SetupSubtleFlame();
                break;
            case ExampleMode.CoolColors:
                SetupCoolColors();
                break;
            case ExampleMode.WarmColors:
                SetupWarmColors();
                break;
            case ExampleMode.Custom:
                // Use inspector values
                break;
        }
    }

    private void SetupStandardFire()
    {
        fireController.height = 2.5f;
        fireController.width = 1.2f;
        fireController.breadth = 1.2f;
        fireController.intensity = 120f;
        fireController.frequency = 1f;
        fireController.slowMotionSpeed = 0.5f;
        fireController.enableHeatShimmer = true;
        fireController.shimmerIntensity = 0.5f;
        fireController.animateColors = true;
        fireController.colorCycleSpeed = 1f;
    }

    private void SetupPulsingFire()
    {
        fireController.height = 2f;
        fireController.width = 1f;
        fireController.breadth = 1f;
        fireController.intensity = 100f;
        fireController.frequency = 1.5f;
        fireController.slowMotionSpeed = 0.6f;
        fireController.enableHeatShimmer = true;
        fireController.shimmerIntensity = 0.4f;
    }

    private void SetupColorCycling()
    {
        fireController.height = 3f;
        fireController.width = 1.5f;
        fireController.breadth = 1.5f;
        fireController.intensity = 150f;
        fireController.frequency = 1f;
        fireController.slowMotionSpeed = 0.4f;
        fireController.enableHeatShimmer = true;
        fireController.animateColors = true;
        fireController.colorCycleSpeed = 2f;
    }

    private void SetupDynamicSize()
    {
        fireController.intensity = 120f;
        fireController.frequency = 1.2f;
        fireController.slowMotionSpeed = 0.5f;
        fireController.enableHeatShimmer = true;
    }

    private void SetupIntenseBurst()
    {
        fireController.height = 4f;
        fireController.width = 2f;
        fireController.breadth = 2f;
        fireController.intensity = 250f;
        fireController.frequency = 2f;
        fireController.slowMotionSpeed = 0.7f;
        fireController.enableHeatShimmer = true;
        fireController.shimmerIntensity = 0.8f;
    }

    private void SetupSubtleFlame()
    {
        fireController.height = 1.5f;
        fireController.width = 0.8f;
        fireController.breadth = 0.8f;
        fireController.intensity = 50f;
        fireController.frequency = 0.5f;
        fireController.slowMotionSpeed = 0.3f;
        fireController.enableHeatShimmer = false;
    }

    private void SetupCoolColors()
    {
        Gradient coolGradient = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[4];
        colorKeys[0] = new GradientColorKey(new Color(0f, 0.5f, 1f), 0f);    // Deep blue
        colorKeys[1] = new GradientColorKey(new Color(0f, 1f, 1f), 0.33f);   // Cyan
        colorKeys[2] = new GradientColorKey(new Color(0.5f, 1f, 0.8f), 0.66f); // Light cyan
        colorKeys[3] = new GradientColorKey(new Color(1f, 1f, 1f), 1f);      // White

        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(1f, 0f);
        alphaKeys[1] = new GradientAlphaKey(1f, 1f);

        coolGradient.SetKeys(colorKeys, alphaKeys);
        fireController.SetRainbowGradient(coolGradient);

        fireController.height = 2.5f;
        fireController.width = 1.2f;
        fireController.breadth = 1.2f;
        fireController.intensity = 120f;
        fireController.slowMotionSpeed = 0.4f;
        fireController.enableHeatShimmer = true;
    }

    private void SetupWarmColors()
    {
        Gradient warmGradient = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[4];
        colorKeys[0] = new GradientColorKey(new Color(1f, 0f, 0f), 0f);      // Red
        colorKeys[1] = new GradientColorKey(new Color(1f, 0.5f, 0f), 0.33f); // Orange
        colorKeys[2] = new GradientColorKey(new Color(1f, 1f, 0f), 0.66f);   // Yellow
        colorKeys[3] = new GradientColorKey(new Color(1f, 1f, 0.8f), 1f);    // Light yellow

        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(1f, 0f);
        alphaKeys[1] = new GradientAlphaKey(1f, 1f);

        warmGradient.SetKeys(colorKeys, alphaKeys);
        fireController.SetRainbowGradient(warmGradient);

        fireController.height = 2.5f;
        fireController.width = 1.2f;
        fireController.breadth = 1.2f;
        fireController.intensity = 130f;
        fireController.slowMotionSpeed = 0.5f;
        fireController.enableHeatShimmer = true;
    }

    private void AnimatePulsingFire()
    {
        float pulse = Mathf.Sin(timeAccumulator * 2f) * 0.5f + 0.5f;
        fireController.intensity = Mathf.Lerp(60f, 180f, pulse);
        fireController.height = Mathf.Lerp(1.5f, 3f, pulse);
    }

    private void AnimateColorCycling()
    {
        // Color cycling is handled by the controller's animateColors setting
        // Additional custom logic can be added here
    }

    private void AnimateDynamicSize()
    {
        float wave1 = Mathf.Sin(timeAccumulator * 1.5f) * 0.5f + 0.5f;
        float wave2 = Mathf.Cos(timeAccumulator * 2f) * 0.5f + 0.5f;
        
        fireController.height = Mathf.Lerp(2f, 4f, wave1);
        fireController.width = Mathf.Lerp(0.8f, 2f, wave2);
        fireController.breadth = Mathf.Lerp(0.8f, 2f, 1f - wave2);
    }

    private void AnimateIntenseBurst()
    {
        // Trigger burst every 3 seconds
        if (Mathf.Floor(timeAccumulator / 3f) > Mathf.Floor((timeAccumulator - Time.deltaTime * animationSpeed) / 3f))
        {
            fireController.TriggerBurst(80);
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying && fireController != null)
        {
            ApplyExampleMode();
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Draws gizmo to visualize fire boundaries
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (fireController == null)
            fireController = GetComponent<RainbowFireController>();

        if (fireController != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 size = new Vector3(fireController.width, fireController.height, fireController.breadth);
            Vector3 center = transform.position + Vector3.up * (fireController.height * 0.5f);
            Gizmos.DrawWireCube(center, size);

            if (fireController.enableHeatShimmer)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
                Vector3 shimmerPos = transform.position + Vector3.up * (fireController.height + fireController.shimmerHeightOffset);
                Gizmos.DrawWireCube(shimmerPos, new Vector3(fireController.width * 0.8f, 0.5f, fireController.breadth * 0.8f));
            }
        }
    }
#endif
}
