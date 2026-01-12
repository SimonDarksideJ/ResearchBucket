using UnityEngine;
using System.Collections.Generic;

namespace ResearchBucket.Rendering.TranslucentLight
{
    /// <summary>
    /// Global manager for optimizing multiple TranslucentLight instances.
    /// Handles batch updates and shared resources for better mobile performance.
    /// </summary>
    public class TranslucentLightManager : MonoBehaviour
    {
        #region Singleton
        
        private static TranslucentLightManager instance;
        public static TranslucentLightManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<TranslucentLightManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("TranslucentLightManager");
                        instance = go.AddComponent<TranslucentLightManager>();
                    }
                }
                return instance;
            }
        }
        
        #endregion
        
        #region Inspector Properties
        
        [Header("Performance Settings")]
        [Tooltip("Maximum update frequency per second for all lights")]
        [Range(10, 60)]
        public int targetUpdateRate = 30;
        
        [Tooltip("Enable batching for multiple lights")]
        public bool enableBatching = true;
        
        [Tooltip("Maximum number of lights to update per frame")]
        [Range(1, 100)]
        public int maxLightsPerFrame = 10;
        
        [Header("Statistics")]
        [SerializeField]
        private int registeredLights = 0;
        
        [SerializeField]
        private int activeLights = 0;
        
        #endregion
        
        #region Private Fields
        
        private List<TranslucentLightController> registeredControllers = new List<TranslucentLightController>();
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        private void Update()
        {
            // Reserved for future batch update implementation
            // Currently, individual controllers manage their own updates efficiently
            // via Unity's coroutine system
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Register a light controller with the manager
        /// </summary>
        public void RegisterLight(TranslucentLightController controller)
        {
            if (!registeredControllers.Contains(controller))
            {
                registeredControllers.Add(controller);
                UpdateStatistics();
            }
        }
        
        /// <summary>
        /// Unregister a light controller from the manager
        /// </summary>
        public void UnregisterLight(TranslucentLightController controller)
        {
            registeredControllers.Remove(controller);
            UpdateStatistics();
        }
        
        /// <summary>
        /// Get all registered light controllers
        /// </summary>
        public List<TranslucentLightController> GetAllLights()
        {
            return new List<TranslucentLightController>(registeredControllers);
        }
        
        /// <summary>
        /// Pulse all registered lights simultaneously
        /// </summary>
        public void PulseAll()
        {
            foreach (var controller in registeredControllers)
            {
                if (controller != null && controller.enabled)
                {
                    controller.Pulse();
                }
            }
        }
        
        /// <summary>
        /// Make all registered lights breathe simultaneously
        /// </summary>
        public void BreathAll()
        {
            foreach (var controller in registeredControllers)
            {
                if (controller != null && controller.enabled)
                {
                    controller.Breath();
                }
            }
        }
        
        /// <summary>
        /// Set the same gradient for all lights
        /// </summary>
        public void SetGlobalGradient(Gradient gradient)
        {
            foreach (var controller in registeredControllers)
            {
                if (controller != null)
                {
                    controller.SetColorGradient(gradient);
                }
            }
        }
        
        #endregion
        
        #region Context Menu Methods
        
        [ContextMenu("Report All Lights")]
        public void ReportAllLights()
        {
            Debug.Log("=== Translucent Light Manager Report ===");
            Debug.Log($"Registered Lights: {registeredLights}");
            Debug.Log($"Active Lights: {activeLights}");
            Debug.Log($"Target Update Rate: {targetUpdateRate} FPS");
            Debug.Log($"Batching Enabled: {enableBatching}");
            Debug.Log($"Max Lights Per Frame: {maxLightsPerFrame}");
            Debug.Log("--- Light Details ---");
            
            for (int i = 0; i < registeredControllers.Count; i++)
            {
                var controller = registeredControllers[i];
                if (controller != null)
                {
                    Debug.Log($"[{i}] {controller.gameObject.name} - Active: {controller.enabled}");
                }
            }
            
            Debug.Log("=== End Report ===");
        }
        
        [ContextMenu("Pulse All Lights")]
        public void ContextPulseAll()
        {
            PulseAll();
        }
        
        [ContextMenu("Breathe All Lights")]
        public void ContextBreathAll()
        {
            BreathAll();
        }
        
        #endregion
        
        #region Private Methods
        
        private void UpdateLightsBatch()
        {
            // Note: The batch update system is currently a framework for future enhancements.
            // Individual controllers manage their own updates via Unity's coroutine system,
            // which provides good work distribution. Future versions could add:
            // - LOD-based update rates (distant lights update less frequently)
            // - Synchronized effects across multiple lights
            // - Staggered animation starts to reduce frame spikes
            
            // The registered lights list is maintained for global operations like
            // PulseAll(), BreathAll(), and SetGlobalGradient()
        }
        
        private void UpdateStatistics()
        {
            registeredLights = registeredControllers.Count;
            activeLights = 0;
            
            foreach (var controller in registeredControllers)
            {
                if (controller != null && controller.enabled)
                {
                    activeLights++;
                }
            }
        }
        
        #endregion
    }
}
