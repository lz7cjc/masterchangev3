using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// AUTO-SETUP: Creates a complete loading screen UI with one click
/// Attach this to an empty GameObject and use the context menu
/// </summary>
public class LoadingScreenAutoSetup : MonoBehaviour
{
    [Header("Colors")]
    [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.8f);
    [SerializeField] private Color progressBarColor = new Color(0, 0.8f, 0.4f, 1f);
    [SerializeField] private Color textColor = Color.white;

    [Header("Settings")]
    [SerializeField] private int statusFontSize = 36;
    [SerializeField] private int percentageFontSize = 48;
    [SerializeField] private int progressBarWidth = 600;
    [SerializeField] private int progressBarHeight = 40;

    /// <summary>
    /// Create complete loading screen UI automatically
    /// </summary>
    [ContextMenu("Create Loading Screen UI")]
    public void CreateLoadingScreenUI()
    {
        Debug.Log("[AUTO-SETUP] Creating loading screen UI...");

        // 1. Create or find Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("LoadingCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();

            Debug.Log("[AUTO-SETUP] ✓ Created Canvas");
        }
        else
        {
            Debug.Log("[AUTO-SETUP] ✓ Using existing Canvas");
        }

        // 2. Create Loading Panel
        GameObject panel = new GameObject("LoadingPanel");
        panel.transform.SetParent(canvas.transform, false);

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = backgroundColor;

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;

        Debug.Log("[AUTO-SETUP] ✓ Created LoadingPanel");

        // 3. Create Status Text
        GameObject statusTextObj = new GameObject("StatusText");
        statusTextObj.transform.SetParent(panel.transform, false);

        Text statusText = statusTextObj.AddComponent<Text>();
        statusText.text = "Loading...";
        statusText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        statusText.fontSize = statusFontSize;
        statusText.color = textColor;
        statusText.alignment = TextAnchor.MiddleCenter;

        RectTransform statusRect = statusTextObj.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.5f, 0.5f);
        statusRect.anchorMax = new Vector2(0.5f, 0.5f);
        statusRect.sizeDelta = new Vector2(800, 100);
        statusRect.anchoredPosition = new Vector2(0, 50);

        Debug.Log("[AUTO-SETUP] ✓ Created StatusText");

        // 4. Create Percentage Text
        GameObject percentTextObj = new GameObject("PercentageText");
        percentTextObj.transform.SetParent(panel.transform, false);

        Text percentText = percentTextObj.AddComponent<Text>();
        percentText.text = "0%";
        percentText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        percentText.fontSize = percentageFontSize;
        percentText.color = textColor;
        percentText.alignment = TextAnchor.MiddleCenter;

        RectTransform percentRect = percentTextObj.GetComponent<RectTransform>();
        percentRect.anchorMin = new Vector2(0.5f, 0.5f);
        percentRect.anchorMax = new Vector2(0.5f, 0.5f);
        percentRect.sizeDelta = new Vector2(400, 100);
        percentRect.anchoredPosition = new Vector2(0, -50);

        Debug.Log("[AUTO-SETUP] ✓ Created PercentageText");

        // 5. Create Progress Bar Background
        GameObject bgObj = new GameObject("ProgressBarBackground");
        bgObj.transform.SetParent(panel.transform, false);

        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 0.5f);
        bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        bgRect.sizeDelta = new Vector2(progressBarWidth, progressBarHeight);
        bgRect.anchoredPosition = new Vector2(0, -120);

        Debug.Log("[AUTO-SETUP] ✓ Created Progress Bar Background");

        // 6. Create Progress Bar Fill
        GameObject fillObj = new GameObject("ProgressBarFill");
        fillObj.transform.SetParent(bgObj.transform, false);

        Image fillImage = fillObj.AddComponent<Image>();
        fillImage.color = progressBarColor;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = 0f;

        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;

        Debug.Log("[AUTO-SETUP] ✓ Created Progress Bar Fill");

        // 7. Create or find VRLoadingManager
        VRLoadingManager manager = FindObjectOfType<VRLoadingManager>();
        if (manager == null)
        {
            GameObject managerObj = new GameObject("VRLoadingManager");
            manager = managerObj.AddComponent<VRLoadingManager>();
            Debug.Log("[AUTO-SETUP] ✓ Created VRLoadingManager");
        }
        else
        {
            Debug.Log("[AUTO-SETUP] ✓ Using existing VRLoadingManager");
        }

        // 8. Assign references using reflection
#if UNITY_EDITOR
        SerializedObject serializedManager = new SerializedObject(manager);

        serializedManager.FindProperty("loadingPanel").objectReferenceValue = panel;
        serializedManager.FindProperty("statusText").objectReferenceValue = statusText;
        serializedManager.FindProperty("percentageText").objectReferenceValue = percentText;
        serializedManager.FindProperty("progressBar").objectReferenceValue = fillImage;
        serializedManager.FindProperty("animateProgress").boolValue = true;
        serializedManager.FindProperty("animationSpeed").floatValue = 2f;
        serializedManager.FindProperty("showDebugLogs").boolValue = true;

        serializedManager.ApplyModifiedProperties();
        Debug.Log("[AUTO-SETUP] ✓ Assigned all references to VRLoadingManager");
#endif

        // 9. Hide panel initially
        panel.SetActive(false);

        Debug.Log("[AUTO-SETUP] ========================================");
        Debug.Log("[AUTO-SETUP] ✅ Loading screen UI created successfully!");
        Debug.Log("[AUTO-SETUP] ========================================");
        Debug.Log("[AUTO-SETUP] Created objects:");
        Debug.Log("[AUTO-SETUP]   • LoadingCanvas");
        Debug.Log("[AUTO-SETUP]   • LoadingPanel");
        Debug.Log("[AUTO-SETUP]   • StatusText");
        Debug.Log("[AUTO-SETUP]   • PercentageText");
        Debug.Log("[AUTO-SETUP]   • ProgressBarBackground");
        Debug.Log("[AUTO-SETUP]   • ProgressBarFill");
        Debug.Log("[AUTO-SETUP]   • VRLoadingManager (with references assigned)");
        Debug.Log("[AUTO-SETUP] ========================================");
        Debug.Log("[AUTO-SETUP] To test:");
        Debug.Log("[AUTO-SETUP]   1. Select VRLoadingManager in Hierarchy");
        Debug.Log("[AUTO-SETUP]   2. Right-click on component");
        Debug.Log("[AUTO-SETUP]   3. Select 'Test Loading Screen'");
        Debug.Log("[AUTO-SETUP] ========================================");

        // 10. Select the manager for easy testing
#if UNITY_EDITOR
        Selection.activeGameObject = manager.gameObject;
#endif
    }

    /// <summary>
    /// Clean up - removes all loading screen UI
    /// </summary>
    [ContextMenu("Remove Loading Screen UI")]
    public void RemoveLoadingScreenUI()
    {
        GameObject panel = GameObject.Find("LoadingPanel");
        if (panel != null)
        {
            DestroyImmediate(panel);
            Debug.Log("[AUTO-SETUP] Removed LoadingPanel");
        }

        VRLoadingManager manager = FindObjectOfType<VRLoadingManager>();
        if (manager != null)
        {
            DestroyImmediate(manager.gameObject);
            Debug.Log("[AUTO-SETUP] Removed VRLoadingManager");
        }

        Debug.Log("[AUTO-SETUP] Loading screen UI removed");
    }
}