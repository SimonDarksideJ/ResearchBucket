using UnityEngine;
using UnityEditor;

namespace ResearchBucket.Rendering.TranslucentLight.Editor
{
    [CustomEditor(typeof(TranslucentLightController))]
    public class TranslucentLightControllerEditor : UnityEditor.Editor
    {
        private SerializedProperty colorGradientProp;
        private SerializedProperty emissionIntensityProp;
        private SerializedProperty coreGlowIntensityProp;
        private SerializedProperty coreGlowRadiusProp;
        private SerializedProperty transparencyProp;
        private SerializedProperty autoPulseProp;
        private SerializedProperty pulseIntervalProp;
        private SerializedProperty pulseDurationProp;
        private SerializedProperty pulseMaxIntensityProp;
        private SerializedProperty autoBreathProp;
        private SerializedProperty breathIntervalProp;
        private SerializedProperty breathDurationProp;
        private SerializedProperty breathMaxIntensityProp;
        private SerializedProperty fresnelPowerProp;
        private SerializedProperty fresnelIntensityProp;
        
        private void OnEnable()
        {
            colorGradientProp = serializedObject.FindProperty("colorGradient");
            emissionIntensityProp = serializedObject.FindProperty("emissionIntensity");
            coreGlowIntensityProp = serializedObject.FindProperty("coreGlowIntensity");
            coreGlowRadiusProp = serializedObject.FindProperty("coreGlowRadius");
            transparencyProp = serializedObject.FindProperty("transparency");
            autoPulseProp = serializedObject.FindProperty("autoPulse");
            pulseIntervalProp = serializedObject.FindProperty("pulseInterval");
            pulseDurationProp = serializedObject.FindProperty("pulseDuration");
            pulseMaxIntensityProp = serializedObject.FindProperty("pulseMaxIntensity");
            autoBreathProp = serializedObject.FindProperty("autoBreath");
            breathIntervalProp = serializedObject.FindProperty("breathInterval");
            breathDurationProp = serializedObject.FindProperty("breathDuration");
            breathMaxIntensityProp = serializedObject.FindProperty("breathMaxIntensity");
            fresnelPowerProp = serializedObject.FindProperty("fresnelPower");
            fresnelIntensityProp = serializedObject.FindProperty("fresnelIntensity");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            TranslucentLightController controller = (TranslucentLightController)target;
            
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Translucent Light Controller\nControls shader effects with pulsing and breathing capabilities.\nOptimized for mobile devices.", MessageType.Info);
            EditorGUILayout.Space();
            
            // Color Settings
            EditorGUILayout.LabelField("Color Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(colorGradientProp);
            EditorGUILayout.Space();
            
            // Emission Settings
            EditorGUILayout.LabelField("Emission Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(emissionIntensityProp);
            EditorGUILayout.PropertyField(coreGlowIntensityProp);
            EditorGUILayout.PropertyField(coreGlowRadiusProp);
            EditorGUILayout.Space();
            
            // Transparency Settings
            EditorGUILayout.LabelField("Transparency Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(transparencyProp);
            EditorGUILayout.Space();
            
            // Pulse Settings
            EditorGUILayout.LabelField("Pulse Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(autoPulseProp);
            if (autoPulseProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(pulseIntervalProp);
                EditorGUILayout.PropertyField(pulseDurationProp);
                EditorGUILayout.PropertyField(pulseMaxIntensityProp);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();
            
            // Breathing Settings
            EditorGUILayout.LabelField("Breathing Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(autoBreathProp);
            if (autoBreathProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(breathIntervalProp);
                EditorGUILayout.PropertyField(breathDurationProp);
                EditorGUILayout.PropertyField(breathMaxIntensityProp);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();
            
            // Fresnel Settings
            EditorGUILayout.LabelField("Fresnel Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(fresnelPowerProp);
            EditorGUILayout.PropertyField(fresnelIntensityProp);
            EditorGUILayout.Space();
            
            // Control Buttons
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Pulse"))
                {
                    controller.Pulse();
                }
                if (GUILayout.Button("Breath"))
                {
                    controller.Breath();
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Start Auto Pulse"))
                {
                    controller.StartAutoPulse();
                }
                if (GUILayout.Button("Stop Auto Pulse"))
                {
                    controller.StopAutoPulse();
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Start Auto Breath"))
                {
                    controller.StartAutoBreath();
                }
                if (GUILayout.Button("Stop Auto Breath"))
                {
                    controller.StopAutoBreath();
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space();
                
                if (GUILayout.Button("Report Environment", GUILayout.Height(30)))
                {
                    controller.ReportEnvironment();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Runtime controls available during Play mode", MessageType.Info);
            }
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}
