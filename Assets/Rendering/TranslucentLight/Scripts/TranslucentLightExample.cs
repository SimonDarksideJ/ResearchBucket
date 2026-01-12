using UnityEngine;

namespace ResearchBucket.Rendering.TranslucentLight.Examples
{
    /// <summary>
    /// Example script demonstrating various use cases of the Translucent Light system.
    /// This can be used as a reference for implementing the effect in your own projects.
    /// </summary>
    public class TranslucentLightExample : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Reference to the light controller")]
        public TranslucentLightController lightController;
        
        [Header("Example Configurations")]
        [Tooltip("Which example to demonstrate")]
        public ExampleType exampleType = ExampleType.PulsingCollectible;
        
        public enum ExampleType
        {
            PulsingCollectible,
            BreathingPowerCore,
            InteractiveHighlight,
            ColorCycling,
            RandomPulses
        }
        
        private void Start()
        {
            if (lightController == null)
            {
                lightController = GetComponent<TranslucentLightController>();
            }
            
            if (lightController == null)
            {
                Debug.LogError("TranslucentLightExample requires a TranslucentLightController component!");
                return;
            }
            
            SetupExample();
        }
        
        private void SetupExample()
        {
            switch (exampleType)
            {
                case ExampleType.PulsingCollectible:
                    SetupPulsingCollectible();
                    break;
                case ExampleType.BreathingPowerCore:
                    SetupBreathingPowerCore();
                    break;
                case ExampleType.InteractiveHighlight:
                    SetupInteractiveHighlight();
                    break;
                case ExampleType.ColorCycling:
                    SetupColorCycling();
                    break;
                case ExampleType.RandomPulses:
                    SetupRandomPulses();
                    break;
            }
        }
        
        #region Example Setups
        
        private void SetupPulsingCollectible()
        {
            Debug.Log("Setting up: Pulsing Collectible");
            
            // Bright, attention-grabbing collectible
            lightController.emissionIntensity = 1.5f;
            lightController.coreGlowIntensity = 3f;
            lightController.transparency = 0.4f;
            
            // Quick pulses to attract attention
            lightController.autoPulse = true;
            lightController.pulseInterval = 1f;
            lightController.pulseDuration = 0.5f;
            lightController.pulseMaxIntensity = 3f;
            
            // Yellow-gold gradient for collectible
            Gradient gradient = new Gradient();
            GradientColorKey[] colors = new GradientColorKey[3];
            colors[0] = new GradientColorKey(new Color(1f, 0.8f, 0f), 0f);
            colors[1] = new GradientColorKey(new Color(1f, 1f, 0.5f), 0.5f);
            colors[2] = new GradientColorKey(new Color(1f, 0.9f, 0.2f), 1f);
            gradient.SetKeys(colors, new GradientAlphaKey[] { 
                new GradientAlphaKey(1f, 0f), 
                new GradientAlphaKey(1f, 1f) 
            });
            lightController.SetColorGradient(gradient);
        }
        
        private void SetupBreathingPowerCore()
        {
            Debug.Log("Setting up: Breathing Power Core");
            
            // Strong, powerful glow
            lightController.emissionIntensity = 2f;
            lightController.coreGlowIntensity = 5f;
            lightController.coreGlowRadius = 2f;
            lightController.transparency = 0.6f;
            
            // Slow, steady breathing
            lightController.autoBreath = true;
            lightController.breathInterval = 1f;
            lightController.breathDuration = 3f;
            lightController.breathMaxIntensity = 2f;
            
            // Blue-cyan gradient for energy
            Gradient gradient = new Gradient();
            GradientColorKey[] colors = new GradientColorKey[2];
            colors[0] = new GradientColorKey(new Color(0f, 0.5f, 1f), 0f);
            colors[1] = new GradientColorKey(new Color(0f, 1f, 1f), 1f);
            gradient.SetKeys(colors, new GradientAlphaKey[] { 
                new GradientAlphaKey(1f, 0f), 
                new GradientAlphaKey(1f, 1f) 
            });
            lightController.SetColorGradient(gradient);
        }
        
        private void SetupInteractiveHighlight()
        {
            Debug.Log("Setting up: Interactive Highlight");
            
            // Subtle base glow
            lightController.emissionIntensity = 0.5f;
            lightController.coreGlowIntensity = 1f;
            lightController.transparency = 0.3f;
            
            // No auto effects - triggered by interaction
            lightController.autoPulse = false;
            lightController.autoBreath = false;
            
            // Configure pulse for manual trigger
            lightController.pulseDuration = 0.8f;
            lightController.pulseMaxIntensity = 4f;
            
            // Green gradient for interaction feedback
            Gradient gradient = new Gradient();
            GradientColorKey[] colors = new GradientColorKey[2];
            colors[0] = new GradientColorKey(new Color(0.2f, 1f, 0.2f), 0f);
            colors[1] = new GradientColorKey(new Color(0.5f, 1f, 0.5f), 1f);
            gradient.SetKeys(colors, new GradientAlphaKey[] { 
                new GradientAlphaKey(1f, 0f), 
                new GradientAlphaKey(1f, 1f) 
            });
            lightController.SetColorGradient(gradient);
        }
        
        private void SetupColorCycling()
        {
            Debug.Log("Setting up: Color Cycling");
            
            // Medium intensity
            lightController.emissionIntensity = 1.5f;
            lightController.coreGlowIntensity = 2.5f;
            lightController.transparency = 0.5f;
            
            // Pulse with color changes
            lightController.autoPulse = true;
            lightController.pulseInterval = 2f;
            lightController.pulseDuration = 1f;
            lightController.pulseMaxIntensity = 2.5f;
            
            // Rainbow gradient
            Gradient gradient = new Gradient();
            GradientColorKey[] colors = new GradientColorKey[5];
            colors[0] = new GradientColorKey(Color.red, 0f);
            colors[1] = new GradientColorKey(Color.yellow, 0.25f);
            colors[2] = new GradientColorKey(Color.green, 0.5f);
            colors[3] = new GradientColorKey(Color.cyan, 0.75f);
            colors[4] = new GradientColorKey(Color.blue, 1f);
            gradient.SetKeys(colors, new GradientAlphaKey[] { 
                new GradientAlphaKey(1f, 0f), 
                new GradientAlphaKey(1f, 1f) 
            });
            lightController.SetColorGradient(gradient);
            
            // Start color cycling
            StartCoroutine(CycleColors());
        }
        
        private void SetupRandomPulses()
        {
            Debug.Log("Setting up: Random Pulses");
            
            // Variable intensity
            lightController.emissionIntensity = 1f;
            lightController.coreGlowIntensity = 2f;
            lightController.transparency = 0.4f;
            
            // No auto pulse - we'll control it
            lightController.autoPulse = false;
            lightController.pulseDuration = 0.6f;
            lightController.pulseMaxIntensity = 3.5f;
            
            // Purple-pink gradient
            Gradient gradient = new Gradient();
            GradientColorKey[] colors = new GradientColorKey[2];
            colors[0] = new GradientColorKey(new Color(0.8f, 0f, 1f), 0f);
            colors[1] = new GradientColorKey(new Color(1f, 0.2f, 0.8f), 1f);
            gradient.SetKeys(colors, new GradientAlphaKey[] { 
                new GradientAlphaKey(1f, 0f), 
                new GradientAlphaKey(1f, 1f) 
            });
            lightController.SetColorGradient(gradient);
            
            // Start random pulses
            StartCoroutine(RandomPulses());
        }
        
        #endregion
        
        #region Coroutines
        
        private System.Collections.IEnumerator CycleColors()
        {
            float time = 0f;
            while (exampleType == ExampleType.ColorCycling)
            {
                time += Time.deltaTime * 0.1f; // Slow cycle
                if (time > 1f) time = 0f;
                
                // Update gradient colors based on time
                Color color1 = Color.HSVToRGB(time, 0.8f, 1f);
                Color color2 = Color.HSVToRGB((time + 0.3f) % 1f, 0.8f, 1f);
                
                Gradient gradient = new Gradient();
                GradientColorKey[] colors = new GradientColorKey[2];
                colors[0] = new GradientColorKey(color1, 0f);
                colors[1] = new GradientColorKey(color2, 1f);
                gradient.SetKeys(colors, new GradientAlphaKey[] { 
                    new GradientAlphaKey(1f, 0f), 
                    new GradientAlphaKey(1f, 1f) 
                });
                lightController.SetColorGradient(gradient);
                
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        private System.Collections.IEnumerator RandomPulses()
        {
            while (exampleType == ExampleType.RandomPulses)
            {
                // Random interval between 0.5 and 3 seconds
                float waitTime = Random.Range(0.5f, 3f);
                yield return new WaitForSeconds(waitTime);
                
                if (lightController != null)
                {
                    lightController.Pulse();
                }
            }
        }
        
        #endregion
        
        #region Public Methods (for external triggers)
        
        /// <summary>
        /// Call this when player interacts with the object
        /// </summary>
        public void OnPlayerInteract()
        {
            if (lightController != null)
            {
                lightController.Pulse();
            }
        }
        
        /// <summary>
        /// Call this when player is nearby
        /// </summary>
        public void OnPlayerNearby()
        {
            if (lightController != null && !lightController.autoBreath)
            {
                lightController.Breath();
            }
        }
        
        #endregion
    }
}
