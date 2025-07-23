using UnityEngine;

/// <summary>
/// VR Visual Enhancer - Adds effects WITHOUT interfering with sprite swapping
/// </summary>
public class VRVisualEnhancer : MonoBehaviour
{
    [Header("Enhancement Effects")]
    [SerializeField] private bool addGlowLighting = true;
    [SerializeField] private bool addSubtleAnimation = true;
    [SerializeField] private bool addParticleEffects = false;
    [SerializeField] private bool addAudioFeedback = true;

    [Header("Lighting Settings")]
    [SerializeField] private Color glowColor = new Color(0.3f, 0.8f, 1f, 0.5f);
    [SerializeField] private float glowIntensity = 1f;
    [SerializeField] private float glowRange = 2f;

    [Header("Animation Settings")]
    [SerializeField] private float floatAmount = 0.02f;
    [SerializeField] private float floatSpeed = 1f;
    [SerializeField] private bool randomizeAnimation = true;

    private void Start()
    {
        EnhanceHUD();
    }

    [ContextMenu("Enhance HUD (Non-Destructive)")]
    public void EnhanceHUD()
    {
        Debug.Log("=== ENHANCING VR HUD (NON-DESTRUCTIVE) ===");

        if (addGlowLighting)
        {
            AddGlowLighting();
        }

        if (addSubtleAnimation)
        {
            AddSubtleAnimations();
        }

        if (addAudioFeedback)
        {
            AddAudioFeedback();
        }

        Debug.Log("VR HUD enhanced without affecting sprite swapping!");
    }

    [ContextMenu("Add Glow Lighting")]
    private void AddGlowLighting()
    {
        Debug.Log("Adding glow lighting to icons...");

        foreach (Transform child in transform)
        {
            if (ShouldAddGlow(child))
            {
                AddGlowLight(child);
            }
        }
    }

    [ContextMenu("Add Subtle Animations")]
    private void AddSubtleAnimations()
    {
        Debug.Log("Adding subtle animations...");

        foreach (Transform child in transform)
        {
            if (ShouldAnimate(child))
            {
                AddFloatingEffect(child);
            }
        }
    }

    [ContextMenu("Add Audio Feedback")]
    private void AddAudioFeedback()
    {
        Debug.Log("Adding audio feedback...");

        foreach (Transform child in transform)
        {
            if (HasInteraction(child))
            {
                AddAudioSource(child);
            }
        }
    }

    private bool ShouldAddGlow(Transform icon)
    {
        // Add glow to icons with ToggleActiveIcons or interactive components
        return icon.GetComponent<ToggleActiveIcons>() != null ||
               icon.GetComponent<Collider>() != null ||
               icon.name.ToLower().Contains("btn_") ||
               icon.name.ToLower().Contains("icon");
    }

    private void AddGlowLight(Transform icon)
    {
        // Check if already has a glow light
        Transform existingGlow = icon.Find("GlowLight");
        if (existingGlow != null) return;

        // Create a child object for the glow light
        GameObject glowObject = new GameObject("GlowLight");
        glowObject.transform.SetParent(icon);
        glowObject.transform.localPosition = Vector3.zero;
        glowObject.transform.localRotation = Quaternion.identity;
        glowObject.transform.localScale = Vector3.one;

        // Add a light component for glow effect
        Light glowLight = glowObject.AddComponent<Light>();
        glowLight.type = LightType.Point;
        glowLight.color = glowColor;
        glowLight.intensity = glowIntensity;
        glowLight.range = glowRange;
        glowLight.shadows = LightShadows.None; // No shadows for better performance

        // Add glow controller to respond to icon states
        VRGlowController glowController = glowObject.AddComponent<VRGlowController>();
        glowController.SetupGlow(icon, glowLight);

        Debug.Log($"Added glow light to: {icon.name}");
    }

    private bool ShouldAnimate(Transform icon)
    {
        return icon.GetComponent<ToggleActiveIcons>() != null ||
               icon.GetComponent<Collider>() != null;
    }

    private void AddFloatingEffect(Transform icon)
    {
        // Check if already has floating effect
        VRIconFloater existingFloater = icon.GetComponent<VRIconFloater>();
        if (existingFloater != null) return;

        VRIconFloater floater = icon.gameObject.AddComponent<VRIconFloater>();
        floater.floatAmount = floatAmount;
        floater.floatSpeed = floatSpeed;

        if (randomizeAnimation)
        {
            floater.floatSpeed *= Random.Range(0.8f, 1.2f);
            floater.floatAmount *= Random.Range(0.5f, 1.5f);
        }

        Debug.Log($"Added floating animation to: {icon.name}");
    }

    private bool HasInteraction(Transform icon)
    {
        return icon.GetComponent<Collider>() != null ||
               icon.GetComponent<ToggleActiveIcons>() != null;
    }

    private void AddAudioSource(Transform icon)
    {
        // Check if already has audio
        AudioSource existingAudio = icon.GetComponent<AudioSource>();
        if (existingAudio != null) return;

        AudioSource audioSource = icon.gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D audio for VR
        audioSource.volume = 0.3f;
        audioSource.pitch = Random.Range(0.9f, 1.1f); // Slight variation

        // Try to find audio clips (you can assign these manually)
        // audioSource.clip = Resources.Load<AudioClip>("VR_UI_Click"); // Optional

        Debug.Log($"Added audio source to: {icon.name}");
    }

    [ContextMenu("Remove All Enhancements")]
    public void RemoveAllEnhancements()
    {
        Debug.Log("=== REMOVING ALL ENHANCEMENTS ===");

        // Remove glow lights
        foreach (Transform child in transform)
        {
            Transform glowLight = child.Find("GlowLight");
            if (glowLight != null)
            {
                DestroyImmediate(glowLight.gameObject);
            }

            // Remove floater components
            VRIconFloater floater = child.GetComponent<VRIconFloater>();
            if (floater != null)
            {
                DestroyImmediate(floater);
            }
        }

        Debug.Log("All enhancements removed");
    }
}

/// <summary>
/// Controls glow light intensity based on icon state
/// </summary>
public class VRGlowController : MonoBehaviour
{
    private Light glowLight;
    private ToggleActiveIcons iconController;
    private float baseIntensity;

    [Header("Glow Response")]
    public float hoverMultiplier = 1.5f;
    public float selectedMultiplier = 2f;

    public void SetupGlow(Transform parentIcon, Light light)
    {
        glowLight = light;
        iconController = parentIcon.GetComponent<ToggleActiveIcons>();
        baseIntensity = light.intensity;
    }

    private void Update()
    {
        if (glowLight == null || iconController == null) return;

        // Respond to icon state
        if (iconController.IsInState(ToggleActiveIcons.IconState.Selected))
        {
            glowLight.intensity = baseIntensity * selectedMultiplier;
        }
        else if (iconController.IsInState(ToggleActiveIcons.IconState.Hover))
        {
            glowLight.intensity = baseIntensity * hoverMultiplier;
        }
        else
        {
            glowLight.intensity = baseIntensity;
        }
    }
}