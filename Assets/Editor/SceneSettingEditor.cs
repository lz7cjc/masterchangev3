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
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);

        foreach (Canvas canvas in canvases)
        {
            if (canvas == null) continue; // Safety check

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                // Set desired CanvasScaler properties optimized for landscape on Android
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080); // Landscape orientation
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0f; // Width only scaling for landscape

                // Mark scene as dirty to ensure changes are saved
                if (!Application.isPlaying)
                {
                    EditorUtility.SetDirty(scaler);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
                }

                Debug.Log($"Applied landscape-optimized CanvasScaler settings for Canvas: {canvas.name}");

                // Create/Setup main panel if needed
                SetupMainPanel(canvas);
            }
        }
    }

    private static void SetupMainPanel(Canvas canvas)
    {
        if (canvas == null) return; // Safety check

        // Find or create a panel under the canvas
        Transform existingPanel = canvas.transform.Find("Panel");
        RectTransform panel;

        if (existingPanel == null)
        {
            // Create new panel with proper default positioning
            GameObject panelObj = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel = panelObj.GetComponent<RectTransform>();
            panel.SetParent(canvas.transform, false);

            // Set up the image component for the new panel
            Image panelImage = panelObj.GetComponent<Image>();
            if (panelImage != null)
            {
                SetupPanelImage(panelImage);
            }

            Debug.Log($"Created new Panel for Canvas: {canvas.name}");
        }
        else
        {
            panel = existingPanel as RectTransform;
            GameObject panelObj = existingPanel.gameObject;

            // Get panel image component and set it to the background sprite
            Image panelImage = panelObj.GetComponent<Image>();
            if (panelImage != null)
            {
                SetupPanelImage(panelImage);
            }

            Debug.Log($"Updated Panel for Canvas: {canvas.name}");
        }

        // Always reset the panel to fill the canvas correctly
        if (panel != null)
        {
            SetupPanelTransform(panel);

            // Mark scene as dirty to ensure changes are saved
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(panel);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
            }
        }
    }

    private static void SetupPanelImage(Image panelImage)
    {
        if (panelImage == null) return;

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
            panelImage.type = Image.Type.Simple; // Use simple type for panels if it doesn't have a border
            panelImage.color = Color.white; // Full opacity
        }
        else
        {
            Debug.LogWarning("Could not find sprite 'img_background_panel' - Panel will be transparent");
            panelImage.color = new Color(1, 1, 1, 0); // Transparent if sprite not found
        }
    }

    private static void SetupPanelTransform(RectTransform panel)
    {
        if (panel == null) return;

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

    // Helper menu item to toggle enforcement
    [MenuItem("Tools/Canvas Settings/Toggle Auto-Enforce")]
    private static void ToggleEnforceCanvasSettings()
    {
        enforceCanvasSettings = !enforceCanvasSettings;
        EditorUtility.DisplayDialog("Canvas Settings",
                                   $"Auto-enforce canvas settings is now {(enforceCanvasSettings ? "enabled" : "disabled")}",
                                   "OK");
    }

    // Menu item to manually apply settings to current scene
    [MenuItem("Tools/Canvas Settings/Apply Canvas Settings Now")]
    private static void ManuallyApplyCanvasSettings()
    {
        ApplyCanvasSettings();
        EditorUtility.DisplayDialog("Canvas Settings", "Canvas settings applied to current scene!", "OK");
    }

    // Menu item to disable the script temporarily
    [MenuItem("Tools/Canvas Settings/Disable Auto-Enforce")]
    private static void DisableAutoEnforce()
    {
        enforceCanvasSettings = false;
        EditorUtility.DisplayDialog("Canvas Settings", "Auto-enforce canvas settings disabled", "OK");
    }

    // Menu item to enable the script
    [MenuItem("Tools/Canvas Settings/Enable Auto-Enforce")]
    private static void EnableAutoEnforce()
    {
        enforceCanvasSettings = true;
        EditorUtility.DisplayDialog("Canvas Settings", "Auto-enforce canvas settings enabled", "OK");
    }
}