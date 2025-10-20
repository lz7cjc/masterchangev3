using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class VolumetricFogSettings
{
    [Header("Fog Volume")]
    public Vector3 fogVolumeSize = new Vector3(10f, 10f, 10f);
    public Vector3 fogVolumeOffset = Vector3.zero;

    [Header("Fog Density")]
    public float baseFogDensity = 50f; // Much lower - more subtle
    public float maxFogDensity = 200f; // Much lower
    public AnimationCurve densityByHeight = new AnimationCurve(new Keyframe[]
    {
        new Keyframe(0f, 0f),      // Bottom: clear
        new Keyframe(0.33f, 0f),   // 1/3 up: still clear  
        new Keyframe(0.5f, 0.3f),  // Halfway: light fog
        new Keyframe(0.75f, 0.7f), // 3/4 up: heavy fog
        new Keyframe(1f, 1f)       // Top: maximum fog
    });

    [Header("Fog Appearance")]
    public Color fogColor = new Color(0.9f, 0.9f, 1f, 0.05f); // Much more transparent!
    public float fogParticleSize = 0.5f; // Much smaller particles
    public float fogParticleLifetime = 20f;

    [Header("Fog Movement")]
    public bool enableWind = true;
    public Vector3 windDirection = new Vector3(0.5f, 0.1f, 0.2f);
    public float windStrength = 0.5f;
    public float windTurbulence = 0.2f;
}

public class VolumetricFogController : MonoBehaviour
{
    [Header("Fog Configuration")]
    public VolumetricFogSettings fogSettings = new VolumetricFogSettings();

    [Header("Auto-Setup")]
    public bool autoFitToObject = true;
    public Transform targetObject;

    [Header("Performance Settings")]
    public bool enableVROptimizations = false;
    public bool useLightweightFog = false;
    public int maxParticlesPerFogVolume = 50; // Per instance, not total

    [Header("Scale Override (for tiny prefabs)")]
    public bool useCustomScale = false;
    public Vector3 customFogVolumeSize = new Vector3(12f, 15f, 12f);

    [Header("Prefab Setup")]
    public bool setupOnAwake = true;
    public bool debugPositioning = false;

    private ParticleSystem fogParticleSystem;
    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.ShapeModule shapeModule;
    private ParticleSystem.EmissionModule emissionModule;
    private ParticleSystem.VelocityOverLifetimeModule velocityModule;
    private ParticleSystem.ColorOverLifetimeModule colorModule;
    private ParticleSystem.SizeOverLifetimeModule sizeModule;

    void Start()
    {
        if (setupOnAwake)
        {
            SetupPrefabFog();
        }
    }

    void SetupPrefabFog()
    {
        if (targetObject == null)
            targetObject = transform;

        // Apply VR optimizations if enabled
        if (enableVROptimizations)
        {
            ApplyVROptimizations();
        }

        SetupVolumetricFog();

        // Handle prefab positioning properly
        if (autoFitToObject && targetObject != null)
        {
            AutoFitFogToPrefab();
        }

        ApplyFogSettings();

        if (debugPositioning)
        {
            Debug.Log($"Fog setup complete for prefab: {gameObject.name}");
            Debug.Log($"Fog volume: {fogSettings.fogVolumeSize}, Offset: {fogSettings.fogVolumeOffset}");
        }
    }

    void ApplyVROptimizations()
    {
        Debug.Log("Applying VR performance optimizations...");

        // Reduce particle counts dramatically for VR
        fogSettings.baseFogDensity = 20f; // Very low for VR
        fogSettings.maxFogDensity = maxParticlesPerFogVolume; // User-defined VR limit

        // Larger particles to compensate for fewer particles
        fogSettings.fogParticleSize = 1.2f;

        // Longer lifetime to maintain visual density
        fogSettings.fogParticleLifetime = 30f;

        // Reduce wind complexity for performance
        fogSettings.windTurbulence = 0.05f;

        Debug.Log($"VR optimizations applied: Max particles = {maxParticlesPerFogVolume}");
    }

    void AutoFitFogToPrefab()
    {
        if (useCustomScale)
        {
            // Account for prefab transform scale
            Vector3 scaledFogSize = Vector3.Scale(customFogVolumeSize, transform.lossyScale);
            fogSettings.fogVolumeSize = scaledFogSize;
            fogSettings.fogVolumeOffset = Vector3.zero; // Centered on prefab

            if (debugPositioning)
            {
                Debug.Log($"Custom fog scale: {customFogVolumeSize}");
                Debug.Log($"Prefab scale: {transform.lossyScale}");
                Debug.Log($"Final scaled fog size: {scaledFogSize}");
            }
            return;
        }

        // Get bounds relative to this prefab's root
        Bounds bounds = GetPrefabBounds();

        // Check if bounds are too small (common issue with imported models)
        if (bounds.size.magnitude < 5f)
        {
            Debug.LogWarning($"Prefab bounds very small ({bounds.size}). Using minimum fog volume.");
            Vector3 minSize = new Vector3(8f, 12f, 8f);
            // Scale the minimum size by the prefab's scale
            fogSettings.fogVolumeSize = Vector3.Scale(minSize, transform.lossyScale);
        }
        else
        {
            // Scale up the fog volume and account for prefab scale
            Vector3 baseSize = bounds.size * 2f + Vector3.one * 3f;
            fogSettings.fogVolumeSize = Vector3.Scale(baseSize, transform.lossyScale);
        }

        // Position fog at the center of the prefab in local space
        fogSettings.fogVolumeOffset = Vector3.zero; // Always center on prefab

        if (debugPositioning)
        {
            Debug.Log($"Prefab bounds: {bounds}");
            Debug.Log($"Prefab scale: {transform.lossyScale}");
            Debug.Log($"Bounds size magnitude: {bounds.size.magnitude}");
            Debug.Log($"Final fog volume size: {fogSettings.fogVolumeSize}");
            Debug.Log($"Fog offset: {fogSettings.fogVolumeOffset}");
        }
    }

    Bounds GetPrefabBounds()
    {
        // Get all renderers within this prefab (children of this transform)
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            // Fallback for prefabs without renderers
            return new Bounds(transform.position, Vector3.one * 10f);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    void SetupVolumetricFog()
    {
        Debug.Log("Setting up volumetric fog...");

        // Create particle system if it doesn't exist
        fogParticleSystem = GetComponent<ParticleSystem>();
        if (fogParticleSystem == null)
        {
            GameObject fogGO = new GameObject("VolumetricFog");
            fogGO.transform.SetParent(transform);
            fogGO.transform.localPosition = Vector3.zero;
            fogParticleSystem = fogGO.AddComponent<ParticleSystem>();
            Debug.Log("Created new particle system: " + fogGO.name);
        }
        else
        {
            Debug.Log("Using existing particle system");
        }

        // Get particle system modules
        mainModule = fogParticleSystem.main;
        shapeModule = fogParticleSystem.shape;
        emissionModule = fogParticleSystem.emission;
        velocityModule = fogParticleSystem.velocityOverLifetime;
        colorModule = fogParticleSystem.colorOverLifetime;
        sizeModule = fogParticleSystem.sizeOverLifetime;

        // Enable required modules
        velocityModule.enabled = true;
        colorModule.enabled = true;
        sizeModule.enabled = true;

        // Create URP-compatible material for particles
        CreateURPFogMaterial();
    }

    void CreateURPFogMaterial()
    {
        // Create a soft circular texture for realistic fog
        Texture2D fogTexture = CreateSoftCircleTexture(64);

        // Try multiple URP particle shaders in order of preference
        Shader urpShader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                          ?? Shader.Find("Sprites/Default")
                          ?? Shader.Find("UI/Default");

        if (urpShader != null)
        {
            Material fogMaterial = new Material(urpShader);
            fogMaterial.name = "VolumetricFogMaterial";
            fogMaterial.mainTexture = fogTexture;

            // Set material properties for soft, realistic fog
            if (urpShader.name.Contains("Universal Render Pipeline"))
            {
                // URP particle shader settings for soft fog
                fogMaterial.SetFloat("_Surface", 1); // Transparent
                fogMaterial.SetFloat("_Blend", 0); // Alpha blend
                fogMaterial.SetFloat("_AlphaClip", 0);
                fogMaterial.SetFloat("_SrcBlend", 5); // SrcAlpha
                fogMaterial.SetFloat("_DstBlend", 10); // OneMinusSrcAlpha
                fogMaterial.SetFloat("_ZWrite", 0);

                // Set soft particle properties
                fogMaterial.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.3f)); // Semi-transparent white
                if (fogMaterial.HasProperty("_Color"))
                    fogMaterial.SetColor("_Color", new Color(1f, 1f, 1f, 0.3f));
            }
            else
            {
                // Fallback shader settings
                fogMaterial.color = new Color(1f, 1f, 1f, 0.3f);
            }

            fogMaterial.renderQueue = 3000;

            // Apply the material to the particle system
            var renderer = fogParticleSystem.GetComponent<ParticleSystemRenderer>();
            renderer.material = fogMaterial;

            // Set renderer properties for soft fog
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = 0; // Don't force on top

            Debug.Log($"Created soft fog material with circular texture using shader: {urpShader.name}");
        }
        else
        {
            Debug.LogWarning("Could not find suitable URP particle shader. Using default material.");
        }
    }

    Texture2D CreateSoftCircleTexture(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float distance = Vector2.Distance(pos, center);

                // Create soft circular gradient
                float alpha = 1f - Mathf.Clamp01(distance / radius);
                alpha = Mathf.Pow(alpha, 2f); // Make it softer

                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        return texture;
    }

    void ApplyFogSettings()
    {
        if (fogParticleSystem == null)
        {
            Debug.LogError("Cannot apply fog settings - particle system is null!");
            return;
        }

        Debug.Log($"Applying fog settings: Volume Size={fogSettings.fogVolumeSize}, Density={fogSettings.baseFogDensity}");

        // Main module settings
        mainModule.startLifetime = fogSettings.fogParticleLifetime;
        mainModule.startSpeed = 0f;
        mainModule.startSize = fogSettings.fogParticleSize;
        mainModule.startColor = fogSettings.fogColor;
        // Apply VR optimizations to main module
        if (enableVROptimizations)
        {
            mainModule.maxParticles = maxParticlesPerFogVolume; // Strict VR limit
            Debug.Log($"VR optimization: Limited to {maxParticlesPerFogVolume} particles");
        }
        else
        {
            mainModule.maxParticles = Mathf.RoundToInt(fogSettings.maxFogDensity * 2);
        }
        mainModule.simulationSpace = ParticleSystemSimulationSpace.World;

        // Force better visibility settings
        mainModule.prewarm = false; // Disable prewarm for better control
        mainModule.startRotation = 0f;
        mainModule.startRotation3D = false;

        Debug.Log($"Particle settings: Size={fogSettings.fogParticleSize}, Color={fogSettings.fogColor}, MaxParticles={mainModule.maxParticles}");

        // Shape module - box shape for volumetric fog
        shapeModule.enabled = true;
        shapeModule.shapeType = ParticleSystemShapeType.Box;
        shapeModule.scale = fogSettings.fogVolumeSize;
        shapeModule.position = fogSettings.fogVolumeOffset;

        Debug.Log($"Shape settings: Scale={shapeModule.scale}, Position={shapeModule.position}");

        // Emission with height-based density - much more particles for smooth effect
        emissionModule.enabled = true;
        emissionModule.rateOverTime = fogSettings.baseFogDensity * 2f; // Double the base rate

        Debug.Log($"Emission rate: {emissionModule.rateOverTime}");

        // Create height-based emission bursts
        CreateHeightBasedEmission();

        // Wind effect
        if (fogSettings.enableWind)
        {
            velocityModule.enabled = true;
            velocityModule.space = ParticleSystemSimulationSpace.World;

            // Set constant wind velocity
            velocityModule.x = new ParticleSystem.MinMaxCurve(fogSettings.windDirection.x * fogSettings.windStrength);
            velocityModule.y = new ParticleSystem.MinMaxCurve(fogSettings.windDirection.y * fogSettings.windStrength);
            velocityModule.z = new ParticleSystem.MinMaxCurve(fogSettings.windDirection.z * fogSettings.windStrength);

            // Add turbulence for more natural movement
            if (fogSettings.windTurbulence > 0)
            {
                var noise = fogParticleSystem.noise;
                noise.enabled = true;
                noise.strength = fogSettings.windTurbulence;
                noise.frequency = 0.1f;
                noise.scrollSpeed = 0.5f;
            }

            Debug.Log($"Wind applied: Direction={fogSettings.windDirection}, Strength={fogSettings.windStrength}");
        }
        else
        {
            velocityModule.enabled = false;
            var noise = fogParticleSystem.noise;
            noise.enabled = false;
        }

        // Fade in/out over lifetime
        Gradient colorGradient = new Gradient();
        colorGradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(fogSettings.fogColor, 0.0f),
                new GradientColorKey(fogSettings.fogColor, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(fogSettings.fogColor.a, 0.3f),
                new GradientAlphaKey(fogSettings.fogColor.a, 0.7f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        colorModule.color = colorGradient;

        // Size over lifetime for more natural fog
        AnimationCurve sizeCurve = AnimationCurve.Linear(0, 0.5f, 1, 1.5f);
        sizeModule.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        Debug.Log("Fog settings applied successfully!");
    }

    void CreateHeightBasedEmission()
    {
        // Clear existing bursts
        emissionModule.SetBursts(new ParticleSystem.Burst[0]);

        int layerCount = 20; // Many more layers for ultra-smooth gradient
        ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[layerCount];

        for (int i = 0; i < layerCount; i++)
        {
            float heightRatio = (float)i / (layerCount - 1);
            float densityMultiplier = fogSettings.densityByHeight.Evaluate(heightRatio);

            // Skip layers with no fog (bottom clear area)
            if (densityMultiplier <= 0.01f)
            {
                bursts[i] = new ParticleSystem.Burst(i * 0.05f, 0);
                continue;
            }

            // Create many small bursts for smooth, subtle fog
            float burstTime = i * 0.05f; // Frequent small bursts
            float burstCount = fogSettings.baseFogDensity * densityMultiplier * 0.05f; // Much smaller bursts

            bursts[i] = new ParticleSystem.Burst(burstTime, (short)Mathf.Max(1, burstCount));
        }

        emissionModule.SetBursts(bursts);

        Debug.Log($"Created {layerCount} ultra-smooth height-based fog layers");
    }

    [ContextMenu("Auto-Fit Fog to Object")]
    public void AutoFitFogToObject()
    {
        if (targetObject == null) return;

        // Get bounds of target object
        Bounds bounds = GetObjectBounds(targetObject);

        // Set fog volume to encompass the object with some padding
        fogSettings.fogVolumeSize = bounds.size + Vector3.one * 2f; // 2 unit padding
        fogSettings.fogVolumeOffset = bounds.center - transform.position;

        // Position this fog controller at the object's position
        transform.position = bounds.center - fogSettings.fogVolumeOffset;

        Debug.Log($"Auto-fitted fog volume: Size={fogSettings.fogVolumeSize}, Center={bounds.center}");
        Debug.Log($"Fog controller position: {transform.position}");
        Debug.Log($"Fog volume offset: {fogSettings.fogVolumeOffset}");

        if (Application.isPlaying)
            ApplyFogSettings();
    }

    [ContextMenu("Test Extreme Fog (Debugging)")]
    public void TestExtremeFog()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Test Extreme Fog only works in Play Mode!");
            return;
        }

        Debug.Log("Applying extreme fog settings for visibility test...");

        // Set extreme settings that should be very visible
        fogSettings.baseFogDensity = 1000f;
        fogSettings.maxFogDensity = 2000f;
        fogSettings.fogColor = Color.magenta; // Bright magenta - very visible!
        fogSettings.fogParticleSize = 5f; // Large particles
        fogSettings.fogParticleLifetime = 20f; // Long lifetime

        // Small fog volume right around the object
        fogSettings.fogVolumeSize = Vector3.one * 5f;
        fogSettings.fogVolumeOffset = Vector3.zero;

        ApplyFogSettings();

        Debug.Log("Extreme fog applied! You should see bright magenta particles.");
    }

    [ContextMenu("Fix Fog Position (Simple)")]
    public void FixFogPositionSimple()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Fix Fog Position only works in Play Mode!");
            return;
        }

        Debug.Log("Fixing fog position to camera view...");

        // Position fog controller at camera position for testing
        Camera cam = Camera.main ?? FindFirstObjectByType<Camera>();
        if (cam != null)
        {
            transform.position = cam.transform.position + cam.transform.forward * 5f;
            Debug.Log($"Moved fog controller to: {transform.position}");
        }

        // Set simple fog settings
        fogSettings.fogVolumeSize = Vector3.one * 10f;
        fogSettings.fogVolumeOffset = Vector3.zero;
        fogSettings.baseFogDensity = 500f;
        fogSettings.fogColor = Color.white;
        fogSettings.fogParticleSize = 3f;

        ApplyFogSettings();

        Debug.Log("Fog positioned in front of camera. You should see white particles!");
    }

    [ContextMenu("Reset to Normal Fog Settings")]
    public void ResetToNormalFogSettings()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Reset Fog Settings only works in Play Mode!");
            return;
        }

        Debug.Log("Resetting to normal fog settings...");

        // Reset to normal, subtle fog settings
        fogSettings.baseFogDensity = 100f;
        fogSettings.maxFogDensity = 500f;
        fogSettings.fogColor = new Color(0.8f, 0.8f, 0.9f, 0.3f); // Light blue-gray
        fogSettings.fogParticleSize = 2f;
        fogSettings.fogParticleLifetime = 10f;

        // Re-create URP material and apply settings
        CreateURPFogMaterial();
        ApplyFogSettings();

        Debug.Log("Normal fog settings applied!");
    }

    [ContextMenu("Setup URP Fog (Recommended)")]
    public void SetupURPFog()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Setup URP Fog only works in Play Mode!");
            return;
        }

        Debug.Log("Setting up optimized URP fog...");

        // URP-optimized fog settings
        fogSettings.baseFogDensity = 50f;
        fogSettings.maxFogDensity = 200f;
        fogSettings.fogColor = new Color(0.9f, 0.9f, 1.0f, 0.4f); // Light white-blue
        fogSettings.fogParticleSize = 1.5f;
        fogSettings.fogParticleLifetime = 8f;
        fogSettings.enableWind = true;
        fogSettings.windStrength = 0.3f;

        // Create proper URP material
        CreateURPFogMaterial();
        ApplyFogSettings();

        Debug.Log("URP fog setup complete! You should see subtle white fog particles.");
    }

    [ContextMenu("Resize Fog Volume to Staircase")]
    public void ResizeFogVolumeToStaircase()
    {
        Debug.Log("Manually resizing fog volume...");

        // Set typical staircase dimensions
        fogSettings.fogVolumeSize = new Vector3(8f, 12f, 8f); // Width, Height, Depth
        fogSettings.fogVolumeOffset = Vector3.zero;

        // Position at target object if available
        if (targetObject != null)
        {
            transform.position = targetObject.position;
        }

        if (Application.isPlaying)
        {
            ApplyFogSettings();
        }

        Debug.Log($"Fog volume resized to: {fogSettings.fogVolumeSize}");
        Debug.Log("You can manually adjust 'Fog Volume Size' in the inspector for fine-tuning.");
    }

    [ContextMenu("Setup Staircase Gradient Fog")]
    public void SetupStaircaseGradientFog()
    {
        Debug.Log("Setting up staircase gradient fog (clear bottom → foggy top)...");

        // Set the density curve: clear at bottom, fog starts at 1/3 height, max at top
        Keyframe[] keys = new Keyframe[]
        {
            new Keyframe(0f, 0f),      // Bottom: 0% fog (completely clear)
            new Keyframe(0.33f, 0f),   // 1/3 up: still clear
            new Keyframe(0.5f, 0.3f),  // Halfway: light fog starts
            new Keyframe(0.75f, 0.7f), // 3/4 up: heavy fog
            new Keyframe(1f, 1f)       // Top: maximum fog (almost hide staircase)
        };

        fogSettings.densityByHeight = new AnimationCurve(keys);

        // Set good density values for visibility
        fogSettings.baseFogDensity = 80f;
        fogSettings.maxFogDensity = 400f;

        // Make fog more visible
        fogSettings.fogColor = new Color(0.85f, 0.85f, 0.95f, 0.6f); // Light blue-white, more opaque
        fogSettings.fogParticleSize = 2.5f;
        fogSettings.fogParticleLifetime = 12f;

        // Gentle wind for natural movement
        fogSettings.enableWind = true;
        fogSettings.windStrength = 0.2f;

        if (Application.isPlaying)
        {
            CreateURPFogMaterial();
            ApplyFogSettings();
        }

        Debug.Log("Staircase gradient fog setup complete!");
        Debug.Log("Bottom: Clear → 1/3 height: Fog starts → Top: Maximum fog");
    }

    [ContextMenu("Create Realistic Fog")]
    public void CreateRealisticFog()
    {
        Debug.Log("Creating realistic soft fog...");

        // Set realistic fog settings - much more subtle
        fogSettings.baseFogDensity = 80f;
        fogSettings.maxFogDensity = 300f;
        fogSettings.fogColor = new Color(0.95f, 0.95f, 1f, 0.08f); // Very transparent white-blue
        fogSettings.fogParticleSize = 0.8f; // Small particles
        fogSettings.fogParticleLifetime = 25f; // Long lifetime for stability

        // Gentle, realistic wind
        fogSettings.enableWind = true;
        fogSettings.windDirection = new Vector3(0.1f, 0.05f, 0.1f); // Very gentle
        fogSettings.windStrength = 0.1f; // Very subtle movement
        fogSettings.windTurbulence = 0.1f;

        if (Application.isPlaying)
        {
            CreateURPFogMaterial();
            ApplyFogSettings();
        }

        Debug.Log("Realistic fog created with soft circular particles and subtle movement.");
    }

    [ContextMenu("Fix Settings Persistence")]
    public void FixSettingsPersistence()
    {
        // Turn off auto-fit so manual settings persist
        autoFitToObject = false;

        Debug.Log("Settings persistence fixed. Your manual fog volume size will now be kept between play sessions.");
        Debug.Log($"Current fog volume locked at: {fogSettings.fogVolumeSize}");

#if UNITY_EDITOR
        // Mark dirty to save changes
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    [ContextMenu("Fix Fog Position (Don't Move Prefab)")]
    public void FixFogPositionOnly()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Fix Fog Position only works in Play Mode!");
            return;
        }

        Debug.Log("Repositioning fog volume only (not moving prefab)...");

        // Only adjust the fog volume offset, don't move the whole transform
        fogSettings.fogVolumeOffset = Vector3.zero; // Center fog on the controller

        // Make sure fog size is reasonable
        if (fogSettings.fogVolumeSize.magnitude < 10f)
        {
            fogSettings.fogVolumeSize = new Vector3(12f, 15f, 12f); // Good size for staircase
        }

        ApplyFogSettings();

        Debug.Log($"Fog repositioned. Volume size: {fogSettings.fogVolumeSize}, Offset: {fogSettings.fogVolumeOffset}");
        Debug.Log("The prefab itself was not moved - only the fog volume was adjusted.");
    }

    [ContextMenu("Create Visible White Fog")]
    public void CreateVisibleWhiteFog()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Create Visible White Fog only works in Play Mode!");
            return;
        }

        Debug.Log("Creating highly visible white fog...");

        // Extreme visibility settings
        fogSettings.baseFogDensity = 300f;
        fogSettings.maxFogDensity = 1000f;
        fogSettings.fogColor = Color.white; // Pure white
        fogSettings.fogParticleSize = 5f; // Very large particles
        fogSettings.fogParticleLifetime = 20f; // Very long lifetime

        // Strong animation
        fogSettings.enableWind = true;
        fogSettings.windDirection = new Vector3(0.5f, 0.2f, 0.3f);
        fogSettings.windStrength = 0.6f;

        ApplyFogSettings();

        Debug.Log("Extreme white fog created! This should be impossible to miss in Game view.");
    }

    [ContextMenu("Debug Particle System Info")]
    public void DebugParticleSystemInfo()
    {
        if (fogParticleSystem == null)
        {
            Debug.LogError("No particle system found!");
            return;
        }

        Debug.Log("=== PARTICLE SYSTEM DEBUG INFO ===");
        Debug.Log($"Particle System Active: {fogParticleSystem.gameObject.activeInHierarchy}");
        Debug.Log($"Particle System Playing: {fogParticleSystem.isPlaying}");
        Debug.Log($"Particle Count: {fogParticleSystem.particleCount}");
        Debug.Log($"Emission Rate: {fogParticleSystem.emission.rateOverTime.constant}");
        Debug.Log($"Max Particles: {fogParticleSystem.main.maxParticles}");

        var renderer = fogParticleSystem.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            Debug.Log($"Renderer Enabled: {renderer.enabled}");
            Debug.Log($"Material: {(renderer.material != null ? renderer.material.name : "NULL")}");
            Debug.Log($"Shader: {(renderer.material != null ? renderer.material.shader.name : "NULL")}");
        }

        Debug.Log("=== END DEBUG INFO ===");
    }

    [ContextMenu("Setup for VR Performance")]
    public void SetupForVRPerformance()
    {
        Debug.Log("Configuring fog for optimal VR performance...");

        // Enable all VR optimizations
        enableVROptimizations = true;
        maxParticlesPerFogVolume = 30; // Conservative per instance

        // Set ultra-light fog settings
        fogSettings.baseFogDensity = 15f; // Minimal particles
        fogSettings.maxFogDensity = 30f;
        fogSettings.fogParticleSize = 1.5f; // Larger to compensate
        fogSettings.fogParticleLifetime = 40f; // Long lifetime
        fogSettings.fogColor = new Color(0.9f, 0.9f, 1f, 0.12f); // Slightly more opaque to compensate

        // Minimal wind for performance
        fogSettings.enableWind = true;
        fogSettings.windStrength = 0.05f; // Very subtle
        fogSettings.windTurbulence = 0.02f; // Minimal

        // Check if prefab bounds are tiny and enable custom scale if needed
        if (Application.isPlaying || !Application.isEditor)
        {
            Bounds bounds = GetPrefabBounds();
            if (bounds.size.magnitude < 5f)
            {
                Debug.LogWarning("Tiny prefab detected. Enabling custom scale override.");
                useCustomScale = true;
                customFogVolumeSize = new Vector3(12f, 15f, 12f);
            }
        }

        // Ensure prefab setup
        setupOnAwake = true;
        autoFitToObject = true;

        if (Application.isPlaying)
        {
            ApplyVROptimizations();
            CreateURPFogMaterial();
            ApplyFogSettings();
        }

        Debug.Log($"VR setup complete! Each instance uses ~{maxParticlesPerFogVolume} particles max.");

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    [ContextMenu("Fix Tiny Prefab Scale")]
    public void FixTinyPrefabScale()
    {
        Debug.Log("Enabling custom scale for tiny prefab...");

        useCustomScale = true;

        // Automatically calculate correct size based on prefab scale
        Vector3 baseSize = new Vector3(12f, 15f, 12f); // Base size for 1,1,1 scale

        // Don't multiply by scale here - let AutoFitFogToPrefab handle it
        customFogVolumeSize = baseSize;

        // Position fog at prefab center
        fogSettings.fogVolumeOffset = Vector3.zero;

        Debug.Log($"Prefab scale detected: {transform.lossyScale}");
        Debug.Log($"Base fog volume: {customFogVolumeSize}");
        Debug.Log($"Actual scaled fog volume will be: {Vector3.Scale(customFogVolumeSize, transform.lossyScale)}");

        if (Application.isPlaying)
        {
            ApplyFogSettings();
        }

        Debug.Log("Custom scale enabled. Fog will now account for prefab scaling automatically.");

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    [ContextMenu("Fix Scale 100x100x100 Prefab")]
    public void FixScale100Prefab()
    {
        Debug.Log("Fixing fog for 100x100x100 scaled prefab...");

        useCustomScale = true;

        // For 100x scale, we need base values that will be multiplied by 100
        customFogVolumeSize = new Vector3(0.12f, 0.15f, 0.12f); // Will become (12, 15, 12) when scaled by 100

        // Also update the main fog volume size to show the correct final values
        Vector3 finalSize = Vector3.Scale(customFogVolumeSize, transform.lossyScale);
        fogSettings.fogVolumeSize = finalSize;

        fogSettings.fogVolumeOffset = Vector3.zero;

        Debug.Log($"Prefab scale: {transform.lossyScale}");
        Debug.Log($"Base fog volume (unscaled): {customFogVolumeSize}");
        Debug.Log($"Final fog volume (scaled): {finalSize}");
        Debug.Log($"Main Fog Volume Size updated to: {fogSettings.fogVolumeSize}");

        if (Application.isPlaying)
        {
            AutoFitFogToPrefab();
            ApplyFogSettings();
        }

        Debug.Log("Scale 100x prefab fixed! Check the 'Fog Volume Size' field - it should now show the correct large values.");

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    [ContextMenu("Manual Fix - Set Correct Size")]
    public void ManualFixCorrectSize()
    {
        Debug.Log("Manually setting correct fog volume size for 100x scaled prefab...");

        // Calculate the volume scaling factor
        Vector3 originalSize = new Vector3(100f, 100f, 100f);
        Vector3 newSize = new Vector3(1200f, 1500f, 1200f);
        float volumeScalingFactor = (newSize.x * newSize.y * newSize.z) / (originalSize.x * originalSize.y * originalSize.z);

        Debug.Log($"Volume scaling factor: {volumeScalingFactor:F1}x larger");

        // Set the main fog volume size directly
        fogSettings.fogVolumeSize = newSize;
        fogSettings.fogVolumeOffset = Vector3.zero;

        // Compensate for the larger volume by increasing particle density and size
        float densityCompensation = Mathf.Pow(volumeScalingFactor, 0.33f); // Cube root for more reasonable scaling

        if (enableVROptimizations)
        {
            // For VR, increase particles moderately and size significantly
            maxParticlesPerFogVolume = Mathf.RoundToInt(30 * Mathf.Min(densityCompensation, 10f)); // Cap at 10x increase
            fogSettings.maxFogDensity = maxParticlesPerFogVolume;
            fogSettings.baseFogDensity = maxParticlesPerFogVolume * 0.5f;

            // Increase particle size significantly to fill the volume
            fogSettings.fogParticleSize = 1.5f * densityCompensation; // Much larger particles

            Debug.Log($"VR compensation: Particles={maxParticlesPerFogVolume}, Size={fogSettings.fogParticleSize:F1}");
        }
        else
        {
            // For desktop, we can afford more particles
            fogSettings.maxFogDensity = Mathf.RoundToInt(200 * Mathf.Min(densityCompensation, 5f));
            fogSettings.baseFogDensity = fogSettings.maxFogDensity * 0.3f;
            fogSettings.fogParticleSize = 0.8f * densityCompensation;

            Debug.Log($"Desktop compensation: Particles={fogSettings.maxFogDensity}, Size={fogSettings.fogParticleSize:F1}");
        }

        // Increase particle lifetime to maintain density
        fogSettings.fogParticleLifetime = 40f;

        // Make fog slightly more opaque to compensate for spread
        Color currentColor = fogSettings.fogColor;
        currentColor.a = Mathf.Min(currentColor.a * 1.5f, 0.3f); // Increase opacity but cap it
        fogSettings.fogColor = currentColor;

        // Turn off auto-fit so our settings stick
        useCustomScale = false;
        autoFitToObject = false;

        Debug.Log($"Fog Volume Size set to: {fogSettings.fogVolumeSize}");
        Debug.Log($"Density compensation applied - fog should look properly thick!");

        if (Application.isPlaying)
        {
            ApplyFogSettings();
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    [ContextMenu("Setup For Large Scale Prefab (100x)")]
    public void SetupForLargeScalePrefab()
    {
        Debug.Log("Setting up fog optimized for 100x scale prefab...");

        // Set appropriate fog volume
        fogSettings.fogVolumeSize = new Vector3(1200f, 1500f, 1200f);
        fogSettings.fogVolumeOffset = Vector3.zero;

        if (enableVROptimizations)
        {
            // VR settings for large scale
            maxParticlesPerFogVolume = 150; // More particles needed for large volume
            fogSettings.maxFogDensity = 150;
            fogSettings.baseFogDensity = 75;
            fogSettings.fogParticleSize = 8f; // Much larger particles
            fogSettings.fogParticleLifetime = 50f; // Longer lifetime
            fogSettings.fogColor = new Color(0.9f, 0.9f, 1f, 0.2f); // More opaque

            Debug.Log("VR large-scale settings: 150 particles, size 8.0, longer lifetime");
        }
        else
        {
            // Desktop settings for large scale
            fogSettings.maxFogDensity = 400;
            fogSettings.baseFogDensity = 150;
            fogSettings.fogParticleSize = 6f;
            fogSettings.fogParticleLifetime = 45f;
            fogSettings.fogColor = new Color(0.9f, 0.9f, 1f, 0.15f);

            Debug.Log("Desktop large-scale settings: 400 particles, size 6.0");
        }

        // Gentle wind appropriate for large scale
        fogSettings.enableWind = true;
        fogSettings.windStrength = 0.3f; // More noticeable wind for large scale
        fogSettings.windTurbulence = 0.15f;

        // Disable auto-fit
        useCustomScale = false;
        autoFitToObject = false;
        setupOnAwake = true;

        if (Application.isPlaying)
        {
            ApplyFogSettings();
        }

        Debug.Log("Large-scale prefab setup complete! Fog density compensated for 100x scale.");

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    [ContextMenu("Calculate Max Instances for VR")]
    public void CalculateMaxInstancesForVR()
    {
        int particlesPerInstance = enableVROptimizations ? maxParticlesPerFogVolume : (int)fogSettings.maxFogDensity;
        bool isLargeScale = fogSettings.fogVolumeSize.magnitude > 500f; // Detect large-scale prefabs

        Debug.Log("=== VR PERFORMANCE CALCULATOR ===");
        Debug.Log($"Particles per fog instance: {particlesPerInstance}");
        Debug.Log($"Fog volume size: {fogSettings.fogVolumeSize}");

        if (isLargeScale)
        {
            Debug.Log("🔍 LARGE-SCALE PREFAB DETECTED (100x scale)");
            Debug.Log("Performance targets adjusted for large volume fog:");
        }

        // Mobile VR performance targets - adjusted for large scale
        int[] targetTotals = isLargeScale ?
            new int[] { 300, 600, 900, 1500 } :  // Large scale targets
            new int[] { 300, 500, 750, 1000 };   // Normal scale targets

        string[] qualityLevels = { "Excellent (60+ FPS)", "Good (45+ FPS)", "Acceptable (30+ FPS)", "Poor (< 30 FPS)" };

        for (int i = 0; i < targetTotals.Length; i++)
        {
            int maxInstances = targetTotals[i] / particlesPerInstance;
            Debug.Log($"{qualityLevels[i]}: Max {maxInstances} instances ({maxInstances * particlesPerInstance} total particles)");
        }

        Debug.Log("=== RECOMMENDATIONS ===");
        if (isLargeScale)
        {
            Debug.Log("• Large-scale (100x) prefabs: 2-6 instances max for good VR performance");
            Debug.Log("• Consider LOD system - only enable fog for nearby staircases");
            Debug.Log("• Each instance uses more particles due to volume compensation");
        }
        else
        {
            Debug.Log("• Google Cardboard: Stay under 300 total particles");
            Debug.Log("• Quest/higher-end mobile: Up to 500-750 particles OK");
        }

        Debug.Log($"• Current setting supports {300 / particlesPerInstance} instances optimally");

        if (isLargeScale && particlesPerInstance > 100)
        {
            Debug.LogWarning("⚠️  HIGH PARTICLE COUNT for VR! Consider reducing maxParticlesPerFogVolume or using fewer instances.");
        }
    }

    [ContextMenu("Setup Prefab (No Runtime Changes Needed)")]
    public void SetupPrefabForInstantiation()
    {
        Debug.Log("Setting up prefab for multiple instantiation...");

        // Configure for automatic setup on instantiation
        setupOnAwake = true;
        autoFitToObject = true;
        debugPositioning = false; // Turn off debug for production

        // Set reasonable default fog volume (will auto-fit anyway)
        fogSettings.fogVolumeSize = new Vector3(12f, 15f, 12f);
        fogSettings.fogVolumeOffset = Vector3.zero;

        // Set production-ready fog settings
        fogSettings.baseFogDensity = 60f;
        fogSettings.maxFogDensity = 200f;
        fogSettings.fogColor = new Color(0.92f, 0.92f, 1f, 0.08f);
        fogSettings.fogParticleSize = 0.8f;
        fogSettings.fogParticleLifetime = 25f;

        // Gentle wind
        fogSettings.enableWind = true;
        fogSettings.windDirection = new Vector3(0.1f, 0.05f, 0.1f);
        fogSettings.windStrength = 0.1f;
        fogSettings.windTurbulence = 0.08f;

        Debug.Log("Prefab setup complete! This prefab can now be instantiated multiple times.");
        Debug.Log("Each instance will automatically configure its fog on instantiation.");

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    [ContextMenu("Test Current Position (Debug)")]
    public void TestCurrentPosition()
    {
        debugPositioning = true;

        if (Application.isPlaying)
        {
            AutoFitFogToPrefab();
            ApplyFogSettings();
        }
        else
        {
            Bounds bounds = GetPrefabBounds();

            Debug.Log($"=== POSITION DEBUG (Edit Mode) ===");
            Debug.Log($"Prefab transform scale: {transform.lossyScale}");
            Debug.Log($"Prefab bounds: Min={bounds.min}, Max={bounds.max}, Size={bounds.size}");
            Debug.Log($"Bounds center (world): {bounds.center}");
            Debug.Log($"Bounds size magnitude: {bounds.size.magnitude}");

            if (useCustomScale)
            {
                Vector3 scaledSize = Vector3.Scale(customFogVolumeSize, transform.lossyScale);
                Debug.Log($"Using CUSTOM scale:");
                Debug.Log($"  Base fog size: {customFogVolumeSize}");
                Debug.Log($"  × Prefab scale: {transform.lossyScale}");
                Debug.Log($"  = Final fog size: {scaledSize}");
                Debug.Log("Fog offset: (0, 0, 0) - centered on prefab");
            }
            else if (bounds.size.magnitude < 5f)
            {
                Debug.LogWarning("TINY PREFAB DETECTED!");
                Debug.Log($"For scale {transform.lossyScale}, recommended actions:");

                if (transform.lossyScale.x >= 100f)
                {
                    Debug.Log("• Use 'Fix Scale 100x100x100 Prefab' for your 100x scaled prefab");
                }
                else
                {
                    Debug.Log("• Use 'Fix Tiny Prefab Scale' for automatic scaling");
                }
            }
            else
            {
                Vector3 recommendedSize = bounds.size * 2f + Vector3.one * 3f;
                Vector3 scaledRecommendedSize = Vector3.Scale(recommendedSize, transform.lossyScale);
                Debug.Log($"Recommended base fog volume size: {recommendedSize}");
                Debug.Log($"Scaled for your prefab: {scaledRecommendedSize}");
                Debug.Log("Fog offset: (0, 0, 0) - centered on prefab");
            }
        }
    }

    [ContextMenu("Keep Current Volume Size (Disable Auto-Fit)")]
    public void KeepCurrentVolumeSize()
    {
        autoFitToObject = false;
        Debug.Log($"Auto-fit disabled. Current volume size locked: {fogSettings.fogVolumeSize}");
        Debug.Log("Volume size will no longer change when entering Play Mode.");
    }

    Bounds GetObjectBounds(Transform obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(obj.position, Vector3.one * 10f);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    [ContextMenu("Create Height-Based Fog Layers")]
    public void CreateFogLayers()
    {
        if (!Application.isPlaying) return;

        // Create multiple particle systems for different height layers
        int layerCount = 5;
        float heightStep = fogSettings.fogVolumeSize.y / layerCount;

        for (int i = 0; i < layerCount; i++)
        {
            GameObject layerGO = new GameObject($"FogLayer_{i}");
            layerGO.transform.SetParent(transform);

            float heightRatio = (float)i / (layerCount - 1);
            float yPosition = fogSettings.fogVolumeOffset.y - fogSettings.fogVolumeSize.y / 2 + (i * heightStep);
            layerGO.transform.localPosition = new Vector3(0, yPosition, 0);

            ParticleSystem layerPS = layerGO.AddComponent<ParticleSystem>();

            // Configure layer-specific settings
            var layerMain = layerPS.main;
            var layerEmission = layerPS.emission;
            var layerShape = layerPS.shape;

            layerMain.startLifetime = fogSettings.fogParticleLifetime;
            layerMain.startSpeed = 0f;
            layerMain.startSize = fogSettings.fogParticleSize;

            // Increase density with height
            float densityMultiplier = fogSettings.densityByHeight.Evaluate(heightRatio);
            layerEmission.rateOverTime = fogSettings.baseFogDensity * densityMultiplier;

            // Make each layer cover a slice of the volume
            layerShape.enabled = true;
            layerShape.shapeType = ParticleSystemShapeType.Box;
            layerShape.scale = new Vector3(fogSettings.fogVolumeSize.x, heightStep, fogSettings.fogVolumeSize.z);

            // Increase opacity with height
            Color layerColor = fogSettings.fogColor;
            layerColor.a *= densityMultiplier;
            layerMain.startColor = layerColor;
        }

        // Disable main particle system since we're using layers
        fogParticleSystem.gameObject.SetActive(false);

        Debug.Log($"Created {layerCount} fog layers with height-based density");
    }

    void OnValidate()
    {
        // Only apply changes in play mode AND if we have valid materials
        if (Application.isPlaying && fogParticleSystem != null)
        {
            ApplyFogSettings();
        }

        // Don't reset settings in edit mode - this was causing the persistence issue
    }

    void OnDrawGizmosSelected()
    {
        // Draw fog volume bounds
        Gizmos.color = new Color(fogSettings.fogColor.r, fogSettings.fogColor.g, fogSettings.fogColor.b, 0.3f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(fogSettings.fogVolumeOffset, fogSettings.fogVolumeSize);

        // Draw wire frame
        Gizmos.color = fogSettings.fogColor;
        Gizmos.DrawWireCube(fogSettings.fogVolumeOffset, fogSettings.fogVolumeSize);

#if UNITY_EDITOR
        // Draw size labels for easier adjustment
        Vector3 worldPos = transform.TransformPoint(fogSettings.fogVolumeOffset);
        UnityEditor.Handles.Label(worldPos + Vector3.up * (fogSettings.fogVolumeSize.y * 0.5f + 1f),
                                 $"Fog Volume: {fogSettings.fogVolumeSize.x:F1} x {fogSettings.fogVolumeSize.y:F1} x {fogSettings.fogVolumeSize.z:F1}");
#endif
    }
}