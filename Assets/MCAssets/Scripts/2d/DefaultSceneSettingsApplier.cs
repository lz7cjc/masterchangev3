using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class DefaultSceneSettingsApplier
{
    static DefaultSceneSettingsApplier()
    {
        EditorApplication.hierarchyChanged += ApplySettings;
    }

    private static void ApplySettings()
    {
        if (Application.isPlaying) return;

        DefaultSceneSettings settings = AssetDatabase.LoadAssetAtPath<DefaultSceneSettings>("Assets/MobilePortrait.asset");

        if (settings == null) return;

        foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = settings.referenceResolution;
                scaler.screenMatchMode = settings.screenMatchMode;
                scaler.matchWidthOrHeight = settings.matchWidthOrHeight;
            }
        }
    }
}
