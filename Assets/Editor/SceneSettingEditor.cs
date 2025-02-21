using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class SceneSettingsEditor
{
    // Add this static bool to easily enable/disable the script
    private static bool enforceCanvasSettings = true;

    static SceneSettingsEditor()
    {
        // Apply settings when the hierarchy changes (scene is loaded or updated)
        EditorApplication.hierarchyChanged += ApplyCanvasSettings;
    }

    private static void ApplyCanvasSettings()
    {
        // Exit if settings enforcement is disabled or during runtime
        if (!enforceCanvasSettings || Application.isPlaying) return;

        // Find all Canvas objects in the scene
        foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                // Set desired CanvasScaler properties optimized for landscape on Android
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080); // Landscape orientation
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0f; // Width only scaling for landscape
                Debug.Log($"Applied landscape-optimized CanvasScaler settings for Canvas: {canvas.name}");

                // Create/Setup main panel if needed
                SetupMainPanel(canvas);
            }
        }
    }

    private static void SetupMainPanel(Canvas canvas)
    {
        // Find or create a panel under the canvas
        Transform existingPanel = canvas.transform.Find("Panel");
        RectTransform panel;

        if (existingPanel == null)
        {
            // Create new panel with proper default positioning
            GameObject panelObj = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel = panelObj.GetComponent<RectTransform>();
            panel.SetParent(canvas.transform, false);

            // Default image and script setup as before...
        }
        else
        {
            panel = existingPanel as RectTransform;
            GameObject panelObj = existingPanel.gameObject;
            // Get panel image component and set it to the background sprite
            Image panelImage = panelObj.GetComponent<Image>();
            if (panelImage != null)
            {
                // Load the background sprite from Resources or project
                Sprite bgSprite = Resources.Load<Sprite>("img_background_panel");
                if (bgSprite == null)
                {
                    // Try to find in the project assets if not in Resources
                    string[] guids = AssetDatabase.FindAssets("t:Sprite img_background_panel");
                    if (guids.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    }
                }

                if (bgSprite != null)
                {
                    panelImage.sprite = bgSprite;
                    panelImage.type = Image.Type.Sliced; // Use sliced for panels if it's a 9-slice sprite
                    panelImage.color = Color.white; // Full opacity
                }
                else
                {
                    Debug.LogWarning("Could not find sprite 'img_background_panel'");
                    panelImage.color = new Color(1, 1, 1, 0); // Transparent if sprite not found
                }
            }

            // Add the required ChangesceneInc.cs script
            if (panelObj.GetComponent<ChangesceneInc>() == null)
            {
                panelObj.AddComponent<ChangesceneInc>();
            }

            // =========================================================
            // PLACEHOLDER 1: ADD CUSTOM SCRIPT HERE
            // Example: panelObj.AddComponent<YourCustomScript1>();
            // =========================================================

            // =========================================================
            // PLACEHOLDER 2: ADD CUSTOM SCRIPT HERE
            // Example: panelObj.AddComponent<YourCustomScript2>();
            // =========================================================

            Debug.Log($"Updated Panel for Canvas: {canvas.name}");
        }

        // Always reset the panel to fill the canvas correctly
        if (panel != null)
        {
            // Set proper anchoring to fill canvas
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.one;

            // Reset offsets to zero - this ensures panel exactly matches canvas size
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;

            // Reset position and rotation
            panel.localPosition = Vector3.zero;
            panel.localRotation = Quaternion.identity;

            // Reset scale to 1
            panel.localScale = Vector3.one;
        }
    }

    // Helper menu item to toggle enforcement
    [MenuItem("Tools/Canvas Settings/Toggle Auto-Enforce")]
    private static void ToggleEnforceCanvasSettings()
    {
        enforceCanvasSettings = !enforceCanvasSettings;
        EditorUtility.DisplayDialog("Canvas Settings",
                                   $"Auto-enforce canvas settings is now {(enforceCanvasSettings ? "enabled" : "disabled")}",
                                   "OK");
    }
}
