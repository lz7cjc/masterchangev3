using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class SceneSettingsEditor
{
    static SceneSettingsEditor()
    {
        // Apply settings when the hierarchy changes (scene is loaded or updated)
        EditorApplication.hierarchyChanged += ApplyCanvasSettings;
    }

    private static void ApplyCanvasSettings()
    {
        // Only apply settings in the Editor, not during runtime
        if (Application.isPlaying) return;

        // Find all Canvas objects in the scene
        foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                // Set desired CanvasScaler properties
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920); // Example resolution
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                Debug.Log($"Applied CanvasScaler settings for Canvas: {canvas.name}");
            }
        }
    }
}
