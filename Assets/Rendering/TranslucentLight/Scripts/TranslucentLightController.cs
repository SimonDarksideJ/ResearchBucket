using UnityEngine;
using System.Collections;

namespace ResearchBucket.Rendering.TranslucentLight
{
    /// <summary>
    /// Controls a translucent light effect for a model with pulsing and breathing capabilities.
    /// Optimized for mobile with multiple instances in mind.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class TranslucentLightController : MonoBehaviour
    {
        #region Inspector Properties
        
        [Header("Color Settings")]
        [Tooltip("Gradient to color the shader effect")]
        public Gradient colorGradient = CreateDefaultGradient();
        
        [Header("Emission Settings")]
        [Range(0f, 10f)]
        [Tooltip("Base emission intensity")]
        public float emissionIntensity = 1f;
        
        [Range(0f, 10f)]
        [Tooltip("Core glow intensity from center")]
        public float coreGlowIntensity = 2f;
        
        [Range(0.1f, 5f)]
        [Tooltip("Radius of the core glow effect")]
        public float coreGlowRadius = 1f;
        
        [Header("Transparency Settings")]
        [Range(0f, 1f)]
        [Tooltip("Base transparency level")]
        public float transparency = 0.5f;
        
        [Header("Pulse Settings")]
        [Tooltip("Enable automatic pulsing")]
        public bool autoPulse = false;
        
        [Range(0.1f, 10f)]
        [Tooltip("Interval between automatic pulses in seconds")]
        public float pulseInterval = 1f;
        
        [Range(0.1f, 5f)]
        [Tooltip("Duration of each pulse")]
        public float pulseDuration = 0.5f;
        
        [Range(0f, 10f)]
        [Tooltip("Maximum intensity multiplier during pulse")]
        public float pulseMaxIntensity = 3f;
        
        [Header("Breathing Settings")]
        [Tooltip("Enable automatic breathing effect")]
        public bool autoBreath = false;
        
        [Range(0.1f, 10f)]
        [Tooltip("Interval between breathing cycles in seconds")]
        public float breathInterval = 1f;
        
        [Range(0.1f, 5f)]
        [Tooltip("Duration of each breathing cycle")]
        public float breathDuration = 2f;
        
        [Range(0f, 10f)]
        [Tooltip("Maximum intensity multiplier during breath")]
        public float breathMaxIntensity = 2f;
        
        [Header("Fresnel Settings")]
        [Range(0.1f, 10f)]
        [Tooltip("Fresnel power for edge lighting")]
        public float fresnelPower = 3f;
        
        [Range(0f, 5f)]
        [Tooltip("Fresnel intensity")]
        public float fresnelIntensity = 1f;
        
        [Header("Manager Settings")]
        [Tooltip("Automatically register with TranslucentLightManager (if present)")]
        public bool registerWithManager = true;
        
        #endregion
        
        #region Private Fields
        
        private Renderer targetRenderer;
        private Material materialInstance;
        private Coroutine pulseCoroutine;
        private Coroutine breathCoroutine;
        private float currentIntensityMultiplier = 1f;
        private bool isPulsing = false;
        private bool isBreathing = false;
        
        // Shader property IDs for performance
        private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");
        private static readonly int EmissionIntensityID = Shader.PropertyToID("_EmissionIntensity");
        private static readonly int CoreGlowIntensityID = Shader.PropertyToID("_CoreGlowIntensity");
        private static readonly int CoreGlowRadiusID = Shader.PropertyToID("_CoreGlowRadius");
        private static readonly int TransparencyID = Shader.PropertyToID("_Transparency");
        private static readonly int FresnelPowerID = Shader.PropertyToID("_FresnelPower");
        private static readonly int FresnelIntensityID = Shader.PropertyToID("_FresnelIntensity");
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeController();
        }
        
        private void Start()
        {
            UpdateShaderProperties();
            
            // Register with manager if desired
            if (registerWithManager)
            {
                TranslucentLightManager.Instance.RegisterLight(this);
            }
            
            if (autoPulse)
            {
                StartAutoPulse();
            }
            
            if (autoBreath)
            {
                StartAutoBreath();
            }
        }
        
        private void OnValidate()
        {
            if (Application.isPlaying && materialInstance != null)
            {
                UpdateShaderProperties();
            }
        }
        
        private void OnDestroy()
        {
            // Unregister from manager
            if (registerWithManager && TranslucentLightManager.Instance != null)
            {
                TranslucentLightManager.Instance.UnregisterLight(this);
            }
            
            // Clean up material instance
            if (materialInstance != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(materialInstance);
                }
                else
                {
                    DestroyImmediate(materialInstance);
                }
            }
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeController()
        {
            targetRenderer = GetComponent<Renderer>();
            
            if (targetRenderer == null)
            {
                Debug.LogError($"TranslucentLightController on {gameObject.name} requires a Renderer component!", this);
                return;
            }
            
            // Create material instance to avoid affecting other objects
            if (targetRenderer.sharedMaterial != null)
            {
                materialInstance = new Material(targetRenderer.sharedMaterial);
                targetRenderer.material = materialInstance;
            }
            else
            {
                Debug.LogWarning($"TranslucentLightController on {gameObject.name} has no material assigned!", this);
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Triggers a single pulse effect
        /// </summary>
        public void Pulse()
        {
            if (!isPulsing)
            {
                if (pulseCoroutine != null)
                {
                    StopCoroutine(pulseCoroutine);
                }
                pulseCoroutine = StartCoroutine(PulseCoroutine());
            }
        }
        
        /// <summary>
        /// Triggers a single breathing effect
        /// </summary>
        public void Breath()
        {
            if (!isBreathing)
            {
                if (breathCoroutine != null)
                {
                    StopCoroutine(breathCoroutine);
                }
                breathCoroutine = StartCoroutine(BreathCoroutine());
            }
        }
        
        /// <summary>
        /// Starts automatic pulsing
        /// </summary>
        public void StartAutoPulse()
        {
            autoPulse = true;
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
            }
            pulseCoroutine = StartCoroutine(AutoPulseCoroutine());
        }
        
        /// <summary>
        /// Stops automatic pulsing
        /// </summary>
        public void StopAutoPulse()
        {
            autoPulse = false;
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
                pulseCoroutine = null;
            }
        }
        
        /// <summary>
        /// Starts automatic breathing
        /// </summary>
        public void StartAutoBreath()
        {
            autoBreath = true;
            if (breathCoroutine != null)
            {
                StopCoroutine(breathCoroutine);
            }
            breathCoroutine = StartCoroutine(AutoBreathCoroutine());
        }
        
        /// <summary>
        /// Stops automatic breathing
        /// </summary>
        public void StopAutoBreath()
        {
            autoBreath = false;
            if (breathCoroutine != null)
            {
                StopCoroutine(breathCoroutine);
                breathCoroutine = null;
            }
        }
        
        /// <summary>
        /// Sets the color gradient
        /// </summary>
        public void SetColorGradient(Gradient gradient)
        {
            if (gradient == null)
            {
                Debug.LogWarning("Attempted to set null gradient. Using default gradient instead.", this);
                colorGradient = CreateDefaultGradient();
            }
            else
            {
                colorGradient = gradient;
            }
            UpdateShaderProperties();
        }
        
        /// <summary>
        /// Evaluates the gradient at a specific time (0-1)
        /// </summary>
        public Color EvaluateGradient(float time)
        {
            return colorGradient.Evaluate(time);
        }
        
        #endregion
        
        #region Context Menu Methods
        
        [ContextMenu("Report Environment")]
        public void ReportEnvironment()
        {
            Debug.Log("=== Translucent Light Controller Environment Report ===");
            Debug.Log($"GameObject: {gameObject.name}");
            Debug.Log($"Position: {transform.position}");
            Debug.Log($"Rotation: {transform.rotation.eulerAngles}");
            Debug.Log($"Scale: {transform.localScale}");
            Debug.Log("--- Renderer Info ---");
            Debug.Log($"Renderer Type: {targetRenderer?.GetType().Name}");
            Debug.Log($"Material: {materialInstance?.name}");
            Debug.Log($"Shader: {materialInstance?.shader?.name}");
            Debug.Log("--- Effect Settings ---");
            Debug.Log($"Emission Intensity: {emissionIntensity}");
            Debug.Log($"Core Glow Intensity: {coreGlowIntensity}");
            Debug.Log($"Core Glow Radius: {coreGlowRadius}");
            Debug.Log($"Transparency: {transparency}");
            Debug.Log($"Current Intensity Multiplier: {currentIntensityMultiplier}");
            Debug.Log("--- Auto Settings ---");
            Debug.Log($"Auto Pulse: {autoPulse} (Interval: {pulseInterval}s, Duration: {pulseDuration}s)");
            Debug.Log($"Auto Breath: {autoBreath} (Interval: {breathInterval}s, Duration: {breathDuration}s)");
            Debug.Log("--- State ---");
            Debug.Log($"Is Pulsing: {isPulsing}");
            Debug.Log($"Is Breathing: {isBreathing}");
            Debug.Log("=== End Report ===");
        }
        
        [ContextMenu("Test Pulse")]
        public void TestPulse()
        {
            Pulse();
        }
        
        [ContextMenu("Test Breath")]
        public void TestBreath()
        {
            Breath();
        }
        
        [ContextMenu("Reset Effect")]
        public void ResetEffect()
        {
            StopAutoPulse();
            StopAutoBreath();
            currentIntensityMultiplier = 1f;
            UpdateShaderProperties();
            Debug.Log("Effect reset to default state");
        }
        
        #endregion
        
        #region Private Methods
        
        private void UpdateShaderProperties()
        {
            if (materialInstance == null) return;
            
            // Get color from gradient (middle of gradient for base color)
            Color baseColor = colorGradient.Evaluate(0.5f);
            Color emissionColor = colorGradient.Evaluate(0.7f);
            
            // Apply properties to shader
            materialInstance.SetColor(BaseColorID, baseColor);
            materialInstance.SetColor(EmissionColorID, emissionColor);
            materialInstance.SetFloat(EmissionIntensityID, emissionIntensity * currentIntensityMultiplier);
            materialInstance.SetFloat(CoreGlowIntensityID, coreGlowIntensity * currentIntensityMultiplier);
            materialInstance.SetFloat(CoreGlowRadiusID, coreGlowRadius);
            materialInstance.SetFloat(TransparencyID, transparency);
            materialInstance.SetFloat(FresnelPowerID, fresnelPower);
            materialInstance.SetFloat(FresnelIntensityID, fresnelIntensity);
        }
        
        private static Gradient CreateDefaultGradient()
        {
            Gradient gradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[2];
            colorKeys[0] = new GradientColorKey(Color.cyan, 0f);
            colorKeys[1] = new GradientColorKey(Color.blue, 1f);
            
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);
            
            gradient.SetKeys(colorKeys, alphaKeys);
            return gradient;
        }
        
        #endregion
        
        #region Coroutines
        
        private IEnumerator PulseCoroutine()
        {
            isPulsing = true;
            float elapsed = 0f;
            
            while (elapsed < pulseDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / pulseDuration;
                
                // Quick rise, slow fall
                float intensity = t < 0.3f 
                    ? Mathf.Lerp(1f, pulseMaxIntensity, t / 0.3f)
                    : Mathf.Lerp(pulseMaxIntensity, 1f, (t - 0.3f) / 0.7f);
                
                currentIntensityMultiplier = intensity;
                UpdateShaderProperties();
                
                yield return null;
            }
            
            currentIntensityMultiplier = 1f;
            UpdateShaderProperties();
            isPulsing = false;
        }
        
        private IEnumerator BreathCoroutine()
        {
            isBreathing = true;
            float elapsed = 0f;
            
            while (elapsed < breathDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / breathDuration;
                
                // Sinusoidal modulation for smooth breathing effect
                float intensity = Mathf.Lerp(1f, breathMaxIntensity, 
                    (Mathf.Sin(t * Mathf.PI * 2f - Mathf.PI / 2f) + 1f) / 2f);
                
                currentIntensityMultiplier = intensity;
                UpdateShaderProperties();
                
                yield return null;
            }
            
            currentIntensityMultiplier = 1f;
            UpdateShaderProperties();
            isBreathing = false;
        }
        
        private IEnumerator AutoPulseCoroutine()
        {
            while (autoPulse)
            {
                yield return StartCoroutine(PulseCoroutine());
                yield return new WaitForSeconds(pulseInterval);
            }
        }
        
        private IEnumerator AutoBreathCoroutine()
        {
            while (autoBreath)
            {
                yield return StartCoroutine(BreathCoroutine());
                yield return new WaitForSeconds(breathInterval);
            }
        }
        
        #endregion
    }
}
