using UnityEngine;

public class FogDiagnostic : MonoBehaviour
{
    [ContextMenu("Diagnose Fog Setup")]
    public void DiagnoseFogSetup()
    {
        Debug.Log("=== FOG DIAGNOSTIC REPORT ===");

        // Check if HeightFogController exists
        HeightFogController fogController = GetComponent<HeightFogController>();
        if (fogController == null)
        {
            Debug.LogError("No HeightFogController found on this object!");
            return;
        }

        // Check renderers
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        Debug.Log($"Found {renderers.Length} renderers");

        if (renderers.Length == 0)
        {
            Debug.LogError("No renderers found! Make sure your object has MeshRenderer components.");
            return;
        }

        // Check materials and shaders
        int fogMaterials = 0;
        int totalMaterials = 0;

        foreach (var renderer in renderers)
        {
            // Use sharedMaterials in edit mode to avoid instantiation warnings
            Material[] materials = Application.isPlaying ? renderer.materials : renderer.sharedMaterials;

            foreach (var material in materials)
            {
                if (material == null) continue;

                totalMaterials++;
                Debug.Log($"Material: {material.name}, Shader: {material.shader.name}");

                if (material.shader.name == "Custom/HeightFog")
                {
                    fogMaterials++;

                    // Check fog settings
                    if (material.HasProperty("_FogStart"))
                    {
                        float fogStart = material.GetFloat("_FogStart");
                        float fogEnd = material.GetFloat("_FogEnd");
                        float fogDensity = material.GetFloat("_FogDensity");
                        Color fogColor = material.GetColor("_FogColor");

                        Debug.Log($"  Fog Start: {fogStart}, End: {fogEnd}, Density: {fogDensity}");
                        Debug.Log($"  Fog Color: {fogColor}");
                    }
                    else
                    {
                        Debug.LogWarning($"  Material {material.name} uses HeightFog shader but properties not found!");
                    }
                }
            }
        }

        Debug.Log($"Materials using HeightFog shader: {fogMaterials}/{totalMaterials}");

        // Check object bounds
        Bounds bounds = GetTotalBounds();
        Debug.Log($"Object bounds - Min Y: {bounds.min.y}, Max Y: {bounds.max.y}");
        Debug.Log($"Recommended fog range: Start={bounds.min.y:F2}, End={bounds.max.y:F2}");

        // Check fog settings
        Debug.Log($"Current fog settings - Start: {fogController.fogSettings.fogStartHeight}, End: {fogController.fogSettings.fogEndHeight}");
        Debug.Log($"Fog density: {fogController.fogSettings.maxFogDensity}, Smoothness: {fogController.fogSettings.fogSmoothness}");

        Debug.Log("=== END DIAGNOSTIC ===");
    }

    private Bounds GetTotalBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(transform.position, Vector3.one);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        // Convert to local coordinates
        Vector3 localMin = transform.InverseTransformPoint(bounds.min);
        Vector3 localMax = transform.InverseTransformPoint(bounds.max);

        return new Bounds((localMin + localMax) * 0.5f, localMax - localMin);
    }
}