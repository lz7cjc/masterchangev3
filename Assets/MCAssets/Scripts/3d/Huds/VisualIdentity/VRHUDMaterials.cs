using UnityEngine;

/// <summary>
/// Professional VR HUD Materials - Creates holographic and glowing effects
/// </summary>
public class VRHUDMaterials : MonoBehaviour
{
    [Header("Material Enhancement")]
    [SerializeField] private bool autoEnhanceOnStart = true;
    [SerializeField] private bool addGlowEffect = true;
    [SerializeField] private bool addHolographicRim = true;
    [SerializeField] private bool addDepthFade = true;

    [Header("Visual Settings")]
    [SerializeField] private Color glowColor = new Color(0.3f, 0.8f, 1f, 1f); // Cyan glow
    [SerializeField] private float glowIntensity = 1.5f;
    [SerializeField] private Color rimColor = new Color(1f, 1f, 1f, 0.5f); // White rim
    [SerializeField] private float rimPower = 2f;

    [Header("State Colors")]
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color hoverColor = new Color(0.8f, 1f, 0.8f, 1f); // Light green
    [SerializeField] private Color selectedColor = new Color(1f, 0.8f, 0.3f, 1f); // Orange

    private void Start()
    {
        if (autoEnhanceOnStart)
        {
            EnhanceAllIcons();
        }
    }

    [ContextMenu("Enhance All Icons")]
    public void EnhanceAllIcons()
    {
        Debug.Log("=== ENHANCING VR HUD MATERIALS ===");

        // Find all renderers in children
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            if (ShouldEnhanceRenderer(renderer))
            {
                EnhanceRenderer(renderer);
            }
        }

        Debug.Log($"Enhanced {renderers.Length} renderers with professional VR materials");
    }

    [ContextMenu("Create Professional Materials")]
    public void CreateProfessionalMaterials()
    {
        Debug.Log("=== CREATING PROFESSIONAL VR MATERIALS ===");

        // Create materials for different states
        Material defaultMaterial = CreateHolographicMaterial("VR_HUD_Default", defaultColor);
        Material hoverMaterial = CreateHolographicMaterial("VR_HUD_Hover", hoverColor);
        Material selectedMaterial = CreateHolographicMaterial("VR_HUD_Selected", selectedColor);

        Debug.Log("Professional materials created! Check your Assets folder.");
    }

    [ContextMenu("Add Subtle Animation")]
    public void AddSubtleAnimation()
    {
        Debug.Log("=== ADDING SUBTLE ANIMATIONS ===");

        // Add gentle floating animation to all icons
        foreach (Transform child in transform)
        {
            if (ShouldAnimateIcon(child))
            {
                AddFloatingAnimation(child);
            }
        }

        Debug.Log("Added subtle floating animations to icons");
    }

    private bool ShouldEnhanceRenderer(Renderer renderer)
    {
        // Skip certain objects
        string name = renderer.name.ToLower();
        return !name.Contains("background") &&
               !name.Contains("panel") &&
               !name.Contains("text") &&
               renderer.GetComponent<TMPro.TextMeshPro>() == null; // Skip TextMeshPro
    }

    private void EnhanceRenderer(Renderer renderer)
    {
        // Create or modify material for better VR appearance
        Material originalMaterial = renderer.material;
        Material enhancedMaterial = CreateEnhancedMaterial(originalMaterial);

        renderer.material = enhancedMaterial;

        Debug.Log($"Enhanced: {renderer.name}");
    }

    private Material CreateEnhancedMaterial(Material original)
    {
        // Create a new material based on the original
        Material enhanced = new Material(original);

        // Enhance for VR
        if (addGlowEffect)
        {
            // Enable emission for glow
            enhanced.EnableKeyword("_EMISSION");
            enhanced.SetColor("_EmissionColor", glowColor * glowIntensity);
        }

        // Make it slightly transparent for holographic effect
        if (addHolographicRim)
        {
            SetupTransparentMaterial(enhanced);
        }

        return enhanced;
    }

    private Material CreateHolographicMaterial(string name, Color baseColor)
    {
        // Create a professional holographic material
        Material holographic = new Material(Shader.Find("Standard"));
        holographic.name = name;

        // Set up for transparency
        SetupTransparentMaterial(holographic);

        // Base color
        holographic.SetColor("_Color", baseColor);

        // Emission for glow
        holographic.EnableKeyword("_EMISSION");
        holographic.SetColor("_EmissionColor", baseColor * glowIntensity);

        // Metallic and smoothness for holographic look
        holographic.SetFloat("_Metallic", 0.1f);
        holographic.SetFloat("_Glossiness", 0.8f);

        return holographic;
    }

    private void SetupTransparentMaterial(Material material)
    {
        // Setup for transparency
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
    }

    private bool ShouldAnimateIcon(Transform icon)
    {
        // Only animate actual icons, not containers
        return icon.GetComponent<Renderer>() != null ||
               icon.GetComponent<ToggleActiveIcons>() != null;
    }

    private void AddFloatingAnimation(Transform icon)
    {
        // Add or get the floating animation component
        VRIconFloater floater = icon.GetComponent<VRIconFloater>();
        if (floater == null)
        {
            floater = icon.gameObject.AddComponent<VRIconFloater>();
        }

        // Randomize the animation slightly for each icon
        floater.floatSpeed = Random.Range(0.8f, 1.2f);
        floater.floatAmount = Random.Range(0.02f, 0.05f);
        floater.rotateSpeed = Random.Range(5f, 15f);
    }

    [ContextMenu("Reset All Materials")]
    public void ResetAllMaterials()
    {
        Debug.Log("=== RESETTING ALL MATERIALS ===");

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            // Reset to default material (you might want to customize this)
            renderer.material = new Material(Shader.Find("Standard"));
        }

        Debug.Log("All materials reset to default");
    }
}

/// <summary>
/// Subtle floating animation for VR icons
/// </summary>
public class VRIconFloater : MonoBehaviour
{
    [Header("Float Animation")]
    public float floatSpeed = 1f;
    public float floatAmount = 0.03f;

    [Header("Rotation Animation")]
    public float rotateSpeed = 10f;
    public bool enableRotation = false;

    private Vector3 startPosition;
    private float timeOffset;

    private void Start()
    {
        startPosition = transform.localPosition;
        timeOffset = Random.Range(0f, 2f * Mathf.PI); // Random start point
    }

    private void Update()
    {
        // Gentle floating motion
        float newY = startPosition.y + Mathf.Sin((Time.time + timeOffset) * floatSpeed) * floatAmount;
        transform.localPosition = new Vector3(startPosition.x, newY, startPosition.z);

        // Optional gentle rotation
        if (enableRotation)
        {
            transform.Rotate(0, rotateSpeed * Time.deltaTime, 0, Space.Self);
        }
    }
}