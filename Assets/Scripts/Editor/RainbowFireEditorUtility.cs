#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility for creating and configuring Rainbow Fire effects quickly.
/// Accessible from Unity menu: GameObject > Effects > Rainbow Fire
/// </summary>
public class RainbowFireEditorUtility
{
    [MenuItem("GameObject/Effects/Create Rainbow Fire", false, 10)]
    static void CreateRainbowFire(MenuCommand menuCommand)
    {
        // Create main fire GameObject
        GameObject fireObject = new GameObject("RainbowFire");
        GameObjectUtility.SetParentAndAlign(fireObject, menuCommand.context as GameObject);
        Undo.RegisterCreatedObjectUndo(fireObject, "Create Rainbow Fire");
        
        // Add Particle System
        ParticleSystem particleSystem = fireObject.AddComponent<ParticleSystem>();
        
        // Configure particle system with optimal settings
        ConfigureParticleSystem(particleSystem);
        
        // Add RainbowFireController
        RainbowFireController controller = fireObject.AddComponent<RainbowFireController>();
        
        // Create and assign material
        Material fireMaterial = CreateRainbowFireMaterial();
        ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        renderer.material = fireMaterial;
        
        // Select the new object
        Selection.activeObject = fireObject;
        
        Debug.Log("Rainbow Fire created successfully! Configure parameters in the RainbowFireController component.");
    }

    [MenuItem("GameObject/Effects/Create Rainbow Fire with Example", false, 11)]
    static void CreateRainbowFireWithExample(MenuCommand menuCommand)
    {
        CreateRainbowFire(menuCommand);
        
        GameObject fireObject = Selection.activeGameObject;
        if (fireObject != null)
        {
            // Add example script
            fireObject.AddComponent<RainbowFireExample>();
            Debug.Log("Rainbow Fire with Example script created! Select example mode in the inspector.");
        }
    }

    static void ConfigureParticleSystem(ParticleSystem ps)
    {
        // Main Module
        var main = ps.main;
        main.duration = 5.0f;
        main.loop = true;
        main.startDelay = 0f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = Color.white;
        main.gravityModifier = -0.2f;
        main.simulationSpeed = 0.5f;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.playOnAwake = true;
        main.maxParticles = 1000;

        // Emission Module
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 100f;

        // Shape Module
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.position = Vector3.zero;
        shape.rotation = Vector3.zero;
        shape.scale = new Vector3(1f, 0.1f, 1f);

        // Velocity over Lifetime
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.y = new ParticleSystem.MinMaxCurve(1f, 3f);

        // Size over Lifetime
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(0.5f, 0.8f);
        sizeCurve.AddKey(1f, 0.2f);
        size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Rotation over Lifetime
        var rotation = ps.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-45f, 45f);

        // Color over Lifetime (will be overridden by controller)
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;

        // Renderer
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortMode = ParticleSystemSortMode.Distance;
        renderer.minParticleSize = 0f;
        renderer.maxParticleSize = 10f;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.sortingFudge = 0;
    }

    static Material CreateRainbowFireMaterial()
    {
        // Try to find existing shader
        Shader rainbowShader = Shader.Find("URP/Particles/RainbowFire");
        
        if (rainbowShader == null)
        {
            // Fallback to standard URP particle shader
            Debug.LogWarning("RainbowFire shader not found. Using fallback URP particle shader. Please ensure RainbowFire.shader is in your project.");
            rainbowShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            
            if (rainbowShader == null)
            {
                rainbowShader = Shader.Find("Particles/Standard Unlit");
            }
        }

        Material material = new Material(rainbowShader);
        material.name = "RainbowFireMaterial";

        // Set default properties if using RainbowFire shader
        if (material.HasProperty("_EmissionStrength"))
        {
            material.SetFloat("_EmissionStrength", 2.5f);
        }
        if (material.HasProperty("_ColorIntensity"))
        {
            material.SetFloat("_ColorIntensity", 1.5f);
        }
        if (material.HasProperty("_DistortionStrength"))
        {
            material.SetFloat("_DistortionStrength", 0.15f);
        }
        if (material.HasProperty("_DistortionSpeed"))
        {
            material.SetFloat("_DistortionSpeed", 1f);
        }
        if (material.HasProperty("_Softness"))
        {
            material.SetFloat("_Softness", 1f);
        }

        // Try to assign default particle texture
        Texture2D defaultTexture = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd");
        if (defaultTexture != null && material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", defaultTexture);
        }

        // Save material to Assets folder
        string materialPath = "Assets/Materials/RainbowFireMaterial.mat";
        string directory = System.IO.Path.GetDirectoryName(materialPath);
        
        if (!System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }

        // Check if material already exists
        if (System.IO.File.Exists(materialPath))
        {
            // Load existing material instead
            return AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        }

        AssetDatabase.CreateAsset(material, materialPath);
        AssetDatabase.SaveAssets();
        
        return material;
    }

    [MenuItem("Assets/Create/Rainbow Fire/Material", false, 100)]
    static void CreateRainbowFireMaterialAsset()
    {
        Material material = CreateRainbowFireMaterial();
        Selection.activeObject = material;
        EditorGUIUtility.PingObject(material);
        Debug.Log("Rainbow Fire Material created at: Assets/Materials/RainbowFireMaterial.mat");
    }

    [MenuItem("Assets/Create/Rainbow Fire/Particle Texture", false, 101)]
    static void CreateParticleTextureAsset()
    {
        // Create temporary GameObject with texture generator
        GameObject tempObject = new GameObject("TempTextureGenerator");
        RainbowFireTextureGenerator generator = tempObject.AddComponent<RainbowFireTextureGenerator>();
        
        generator.textureSize = 256;
        generator.edgeSoftness = 0.8f;
        generator.coreBrightness = 1.5f;
        
        // Generate and save texture
        Texture2D texture = generator.GenerateParticleTexture();
        byte[] bytes = texture.EncodeToPNG();
        
        string path = "Assets/Textures/RainbowFireParticle.png";
        string directory = System.IO.Path.GetDirectoryName(path);
        
        if (!System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }
        
        System.IO.File.WriteAllBytes(path, bytes);
        AssetDatabase.Refresh();
        
        // Configure import settings
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 256;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
        
        // Clean up
        Object.DestroyImmediate(tempObject);
        
        // Select created texture
        Texture2D createdTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        Selection.activeObject = createdTexture;
        EditorGUIUtility.PingObject(createdTexture);
        
        Debug.Log($"Rainbow Fire Particle Texture created at: {path}");
    }
}

/// <summary>
/// Custom inspector for RainbowFireController to add useful buttons and info
/// </summary>
[CustomEditor(typeof(RainbowFireController))]
public class RainbowFireControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        RainbowFireController controller = (RainbowFireController)target;

        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Reset to Default"))
        {
            Undo.RecordObject(controller, "Reset Rainbow Fire");
            controller.height = 2.5f;
            controller.width = 1.2f;
            controller.breadth = 1.2f;
            controller.intensity = 120f;
            controller.frequency = 1f;
            controller.slowMotionSpeed = 0.5f;
            controller.animateColors = true;
            controller.colorCycleSpeed = 1f;
            controller.enableHeatShimmer = true;
            controller.shimmerIntensity = 0.5f;
            EditorUtility.SetDirty(controller);
        }

        if (GUILayout.Button("Trigger Burst"))
        {
            if (Application.isPlaying)
            {
                controller.TriggerBurst(100);
            }
            else
            {
                Debug.LogWarning("Burst can only be triggered in Play mode");
            }
        }

        EditorGUILayout.EndHorizontal();

        if (Application.isPlaying)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("Fire effect is running. Adjust parameters to see changes in real-time.", MessageType.Info);
        }
    }
}
#endif
