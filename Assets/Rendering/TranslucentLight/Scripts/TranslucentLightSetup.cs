using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ResearchBucket.Rendering.TranslucentLight
{
    /// <summary>
    /// Helper script to quickly setup Translucent Light effect on GameObjects.
    /// Can be used both at runtime and in the editor.
    /// </summary>
    public class TranslucentLightSetup : MonoBehaviour
    {
        [Header("Setup Configuration")]
        [Tooltip("Material to apply (leave empty to create from shader)")]
        public Material materialToApply;
        
        [Tooltip("Shader to use if creating new material")]
        public Shader shaderToUse;
        
        [Header("Controller Configuration")]
        [Tooltip("Emission intensity for the effect")]
        [Range(0f, 10f)]
        public float emissionIntensity = 1.5f;
        
        [Tooltip("Core glow intensity")]
        [Range(0f, 10f)]
        public float coreGlowIntensity = 2f;
        
        [Tooltip("Should auto pulse?")]
        public bool autoPulse = true;
        
        [Tooltip("Should auto breathe?")]
        public bool autoBreath = false;
        
        [Tooltip("Color gradient preset")]
        public GradientPreset gradientPreset = GradientPreset.CyanBlue;
        
        public enum GradientPreset
        {
            CyanBlue,
            YellowGold,
            Green,
            PurplePink,
            RedYellow,
            Custom
        }
        
        [Tooltip("Custom gradient (only used if preset is Custom)")]
        public Gradient customGradient;
        
        #if UNITY_EDITOR
        [ContextMenu("Setup Translucent Light")]
        public void SetupTranslucentLight()
        {
            SetupTranslucentLightInternal();
        }
        
        [ContextMenu("Setup on All Children")]
        public void SetupOnAllChildren()
        {
            var renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                SetupOnGameObject(renderer.gameObject);
            }
            Debug.Log($"Setup Translucent Light on {renderers.Length} child objects");
        }
        #endif
        
        private void Start()
        {
            // Auto-setup at runtime if this component exists
            if (Application.isPlaying)
            {
                SetupTranslucentLightInternal();
                // Remove this component after setup (it's just a helper)
                Destroy(this);
            }
        }
        
        private void SetupTranslucentLightInternal()
        {
            SetupOnGameObject(gameObject);
        }
        
        private void SetupOnGameObject(GameObject target)
        {
            // Ensure renderer exists
            var renderer = target.GetComponent<Renderer>();
            if (renderer == null)
            {
                Debug.LogWarning($"No Renderer found on {target.name}. Skipping setup.", target);
                return;
            }
            
            // Setup material
            Material material = materialToApply;
            if (material == null)
            {
                // Try to find the shader
                Shader shader = shaderToUse;
                if (shader == null)
                {
                    shader = Shader.Find("Custom/URP/TranslucentLight");
                }
                
                if (shader == null)
                {
                    Debug.LogError($"Could not find TranslucentLight shader! Please assign shader manually.", target);
                    return;
                }
                
                material = new Material(shader);
                material.name = "TranslucentLight_Runtime";
            }
            
            renderer.sharedMaterial = material;
            
            // Add or get controller
            var controller = target.GetComponent<TranslucentLightController>();
            if (controller == null)
            {
                controller = target.AddComponent<TranslucentLightController>();
            }
            
            // Configure controller
            controller.emissionIntensity = emissionIntensity;
            controller.coreGlowIntensity = coreGlowIntensity;
            controller.autoPulse = autoPulse;
            controller.autoBreath = autoBreath;
            
            // Set gradient
            Gradient gradient = GetGradientFromPreset(gradientPreset);
            controller.SetColorGradient(gradient);
            
            Debug.Log($"Translucent Light setup complete on {target.name}", target);
        }
        
        private Gradient GetGradientFromPreset(GradientPreset preset)
        {
            Gradient gradient = new Gradient();
            GradientColorKey[] colors;
            GradientAlphaKey[] alphas = new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            };
            
            switch (preset)
            {
                case GradientPreset.CyanBlue:
                    colors = new GradientColorKey[] {
                        new GradientColorKey(new Color(0f, 1f, 1f), 0f),
                        new GradientColorKey(new Color(0f, 0.5f, 1f), 1f)
                    };
                    break;
                    
                case GradientPreset.YellowGold:
                    colors = new GradientColorKey[] {
                        new GradientColorKey(new Color(1f, 1f, 0f), 0f),
                        new GradientColorKey(new Color(1f, 0.8f, 0f), 1f)
                    };
                    break;
                    
                case GradientPreset.Green:
                    colors = new GradientColorKey[] {
                        new GradientColorKey(new Color(0.2f, 1f, 0.2f), 0f),
                        new GradientColorKey(new Color(0.5f, 1f, 0.5f), 1f)
                    };
                    break;
                    
                case GradientPreset.PurplePink:
                    colors = new GradientColorKey[] {
                        new GradientColorKey(new Color(0.8f, 0f, 1f), 0f),
                        new GradientColorKey(new Color(1f, 0.2f, 0.8f), 1f)
                    };
                    break;
                    
                case GradientPreset.RedYellow:
                    colors = new GradientColorKey[] {
                        new GradientColorKey(Color.red, 0f),
                        new GradientColorKey(Color.yellow, 1f)
                    };
                    break;
                    
                case GradientPreset.Custom:
                    if (customGradient != null)
                    {
                        return customGradient;
                    }
                    // Fallback to cyan-blue if custom not set
                    goto case GradientPreset.CyanBlue;
                    
                default:
                    colors = new GradientColorKey[] {
                        new GradientColorKey(Color.white, 0f),
                        new GradientColorKey(Color.white, 1f)
                    };
                    break;
            }
            
            gradient.SetKeys(colors, alphas);
            return gradient;
        }
        
        #if UNITY_EDITOR
        /// <summary>
        /// Static utility method to setup translucent light from menu
        /// </summary>
        [MenuItem("GameObject/Effects/Setup Translucent Light", false, 10)]
        private static void SetupFromMenu()
        {
            GameObject[] selectedObjects = Selection.gameObjects;
            
            if (selectedObjects.Length == 0)
            {
                Debug.LogWarning("Please select one or more GameObjects with Renderer components.");
                return;
            }
            
            int setupCount = 0;
            foreach (var obj in selectedObjects)
            {
                var renderer = obj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    var setup = obj.AddComponent<TranslucentLightSetup>();
                    setup.SetupTranslucentLight();
                    DestroyImmediate(setup);
                    setupCount++;
                }
            }
            
            Debug.Log($"Setup Translucent Light on {setupCount} object(s)");
        }
        #endif
    }
}
