using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

// Enhanced version of the video player
public class EnhancedVideoPlayer : MonoBehaviour
{
    [Header("Video Configuration")]
    public string VideoUrlLink;
    public string title;
    public string description;
    public string prefabType;

    [Header("Interaction Settings")]
    public float hoverTimeRequired = 3.0f;
    public bool mouseHover = false;
    private float hoverTimer = 0;

    [Header("Scene Management")]
    public string returntoscene;
    public string nextscene;
    public int returnstage;
    public string behaviour;
    public bool useAdditiveLoading = true; // New flag to toggle additive loading

    [Header("UI Elements")]
    public TMP_Text TMP_title;
    public TMP_Text TMP_description;
    public bool hasText = true;

    [Header("Visual Feedback")]
    public Color normalColor = Color.white;
    public Color hoverColor = Color.green;
    public bool useProgressIndicator = true;
    private RectTransform progressBar;
    private Image progressBarImage;

    [Header("References")]
    [SerializeField] private StartUp StartUp;
    [SerializeField] private RiroStopGoV2 riroStopGoV2;

    // Riro currency check
    private int riroAmount;
    private int minRiroRequired = 50;

    // Object rotation
    public bool rotateOnHover = true;
    private Quaternion originalRotation;
    private bool isRotating = false;

    // Debug flag
    [Header("Debug")]
    public bool debugMode = true;

    // New property to store the category (for zone placement)
    [HideInInspector]
    public string category;

    // New property to support zone-based placement
    [HideInInspector]
    public string zoneName;

    private void Awake()
    {
        // Store original rotation
        originalRotation = transform.rotation;

        // Create progress indicator if needed
        if (useProgressIndicator)
        {
            CreateProgressBar();
        }
    }

    void Start()
    {
        riroAmount = PlayerPrefs.GetInt("rirosBalance");

        // Initialize hasText based on whether TMP_title is assigned
        hasText = (TMP_title != null);

        // Set title and description if available
        if (hasText && TMP_title != null && !string.IsNullOrEmpty(title))
        {
            TMP_title.text = title;
            TMP_title.color = normalColor;
        }
        else if (TMP_title == null && debugMode)
        {
            Debug.LogWarning($"TMP_title is null on {gameObject.name}. Text won't display.");
        }

        if (TMP_description != null && !string.IsNullOrEmpty(description))
        {
            TMP_description.text = description;
        }

        // Find required components if not assigned
        if (riroStopGoV2 == null)
        {
            riroStopGoV2 = FindObjectOfType<RiroStopGoV2>();
            if (riroStopGoV2 == null && debugMode)
            {
                Debug.LogWarning("RiroStopGoV2 not found. Riro currency check will be skipped.");
            }
        }

        // Try to extract category from URL if not already set
        if (string.IsNullOrEmpty(category) && !string.IsNullOrEmpty(VideoUrlLink))
        {
            ExtractCategoryFromUrl();
        }

        // Log initialization for debugging
        if (debugMode)
        {
            Debug.Log($"Initialized video player for: {title} ({VideoUrlLink})");
            Debug.Log($"Zone: {zoneName}, Category: {category}");
        }
    }

    void Update()
    {
        // Handle hover timer logic
        if (mouseHover)
        {
            hoverTimer += Time.deltaTime;

            // Update progress bar if available
            if (progressBar != null && progressBar.gameObject.activeSelf)
            {
                float progress = Mathf.Clamp01(hoverTimer / hoverTimeRequired);
                progressBar.sizeDelta = new Vector2(100 * progress, progressBar.sizeDelta.y);

                // Change color based on progress
                if (progressBarImage != null)
                {
                    progressBarImage.color = Color.Lerp(Color.yellow, Color.green, progress);
                }
            }

            // Handle rotation animation
            if (rotateOnHover && isRotating)
            {
                transform.Rotate(Vector3.up, 30f * Time.deltaTime);
            }

            if (hoverTimer >= hoverTimeRequired)
            {
                mouseHover = false;
                hoverTimer = 0;
                HideProgressBar();
                SetVideoUrl();
            }
        }
    }

    // Try to extract category from the URL
    private void ExtractCategoryFromUrl()
    {
        if (string.IsNullOrEmpty(VideoUrlLink)) return;

        try
        {
            string[] parts = VideoUrlLink.Split('/');
            if (parts.Length >= 3)
            {
                int startIndex = 0;

                // Look for "masterchange" in the URL
                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i].ToLower() == "masterchange")
                    {
                        startIndex = i + 1;
                        break;
                    }
                }

                if (startIndex > 0 && startIndex < parts.Length - 1)
                {
                    category = parts[startIndex];
                    if (debugMode) Debug.Log($"Extracted category: {category} from URL: {VideoUrlLink}");
                }
                else
                {
                    // Fallback to original logic
                    category = parts[parts.Length - 3] + "/" + parts[parts.Length - 2];
                    if (debugMode) Debug.Log($"Used fallback category extraction: {category}");
                }
            }
        }
        catch (System.Exception ex)
        {
            if (debugMode) Debug.LogError($"Error extracting category: {ex.Message}");
        }
    }

    // Creates a progress bar UI element
    private void CreateProgressBar()
    {
        Canvas canvas = GetComponentInChildren<Canvas>();

        // Create canvas if it doesn't exist
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("VideoCanvas");
            canvasObj.transform.SetParent(transform);
            canvasObj.transform.localPosition = Vector3.zero;
            canvasObj.transform.localRotation = Quaternion.identity;

            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            // Set canvas scale and position
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(200, 100);
            canvasRect.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            canvasRect.localPosition = new Vector3(0, 1.5f, 0);

            // Add canvas scaler
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 100;

            // Add raycaster
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Create progress bar
        GameObject progressObj = new GameObject("ProgressBar");
        progressObj.transform.SetParent(canvas.transform);

        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(progressObj.transform);

        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 0);
        bgRect.anchorMax = new Vector2(0.5f, 0);
        bgRect.pivot = new Vector2(0.5f, 0);
        bgRect.sizeDelta = new Vector2(100, 10);
        bgRect.anchoredPosition = new Vector2(0, -20);

        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.5f);

        // Progress fill
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(bgObj.transform);

        progressBar = fillObj.AddComponent<RectTransform>();
        progressBar.anchorMin = new Vector2(0, 0);
        progressBar.anchorMax = new Vector2(0, 1);
        progressBar.pivot = new Vector2(0, 0.5f);
        progressBar.sizeDelta = new Vector2(0, 0);
        progressBar.anchoredPosition = Vector2.zero;

        progressBarImage = fillObj.AddComponent<Image>();
        progressBarImage.color = Color.yellow;

        // Hide initially
        progressObj.SetActive(false);
        progressBar.gameObject.SetActive(false);
    }

    // Shows the progress bar
    private void ShowProgressBar()
    {
        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(true);
            progressBar.parent.gameObject.SetActive(true);
            progressBar.sizeDelta = new Vector2(0, 0);
        }
    }

    // Hides the progress bar
    private void HideProgressBar()
    {
        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(false);
            progressBar.parent.gameObject.SetActive(false);
        }
    }

    // Mouse Enter event
    public void MouseHoverChangeScene()
    {
        if (hasText && TMP_title != null)
        {
            TMP_title.color = hoverColor;
        }

        mouseHover = true;
        hoverTimer = 0;
        ShowProgressBar();

        // Start rotation animation
        if (rotateOnHover)
        {
            isRotating = true;
        }

        if (debugMode)
        {
            Debug.Log($"Mouse hover started on {title}");
        }
    }

    // Mouse Exit Event
    public void MouseExit()
    {
        if (hasText && TMP_title != null)
        {
            TMP_title.color = normalColor;
        }

        mouseHover = false;
        hoverTimer = 0;
        HideProgressBar();

        // Stop rotation and reset
        if (rotateOnHover)
        {
            isRotating = false;
            transform.rotation = originalRotation;
        }

        if (debugMode)
        {
            Debug.Log($"Mouse exit on {title}");
        }
    }

    // Set video URL and redirect to video player scene
    public void SetVideoUrl()
    {
        if (debugMode)
        {
            Debug.Log($"Setting video URL: {VideoUrlLink}");
            Debug.Log($"Return scene: {returntoscene}, Next scene: {nextscene}");
        }

        // Validate URL
        if (string.IsNullOrEmpty(VideoUrlLink))
        {
            Debug.LogError("Video URL is empty! Cannot play video.");
            return;
        }

        // Store scene navigation info
        PlayerPrefs.SetString("returntoscene", returntoscene);
        PlayerPrefs.SetString("behaviour", behaviour);
        PlayerPrefs.SetInt("stage", returnstage);
        PlayerPrefs.SetString("nextscene", nextscene);

        // Store video metadata
        if (!string.IsNullOrEmpty(title))
        {
            PlayerPrefs.SetString("videoTitle", title);
        }

        if (!string.IsNullOrEmpty(description))
        {
            PlayerPrefs.SetString("videoDescription", description);
        }

        // Check if user has enough riro currency
        if (riroAmount >= minRiroRequired)
        {
            PlayerPrefs.DeleteKey("stopFilm");
            Debug.Log($"Loading video with {riroAmount} riros available (sufficient)");
            PlayerPrefs.SetString("VideoUrl", VideoUrlLink);

            // Check if we're in mainVR scene and should use additive loading
            string currentSceneName = SceneManager.GetActiveScene().name.ToLower();
            if (useAdditiveLoading && (currentSceneName == "mainvr" || currentSceneName == "mainvr"))
            {
                // Store that we're coming from mainVR
                PlayerPrefs.SetInt("comingFromMainVR", 1);

                // Store the current scene name with correct casing
                PlayerPrefs.SetString("mainVRSceneName", SceneManager.GetActiveScene().name);

                // Store the active GameObjects for later reactivation
                GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
                foreach (GameObject root in rootObjects)
                {
                    if (root.activeSelf)
                    {
                        // Add to the reactivation list in PlayerPrefs
                        AddToReactivationList(root.name);

                        // Deactivate to avoid conflicts
                        root.SetActive(false);

                        if (debugMode)
                        {
                            Debug.Log($"Deactivated mainVR GameObject: {root.name}");
                        }
                    }
                }

                // Load 360VideoApp additively
                SceneManager.LoadScene("360VideoApp", LoadSceneMode.Additive);
                StartCoroutine(SetNewSceneActive("360VideoApp"));

                if (debugMode)
                {
                    Debug.Log("Using additive scene loading to keep mainVR in memory");
                }
            }
            else
            {
                // For other scenes, use regular scene loading
                PlayerPrefs.SetInt("comingFromMainVR", 0);
                SceneManager.LoadScene("360VideoApp", LoadSceneMode.Single);

                if (debugMode)
                {
                    Debug.Log("Using standard scene loading method");
                }
            }
        }
        else
        {
            Debug.Log($"Not enough riros to play video: {riroAmount} < {minRiroRequired}");
            PlayerPrefs.SetInt("stopFilm", 0);

            if (riroStopGoV2 != null)
            {
                riroStopGoV2.doNotPass(0);
            }
            else
            {
                Debug.LogError("RiroStopGoV2 component not found!");

                // Fallback message
                if (TMP_title != null)
                {
                    TMP_title.text = "Not enough Riros!";
                    TMP_title.color = Color.red;

                    // Reset after delay
                    StartCoroutine(ResetTitleAfterDelay(2.0f));
                }
            }
        }
    }

    // Helper method to add a GameObject name to the reactivation list
    private void AddToReactivationList(string objectName)
    {
        // Get existing list
        string listStr = PlayerPrefs.GetString("mainvr_reactivate", "");
        List<string> list = new List<string>();

        // Parse existing items
        if (!string.IsNullOrEmpty(listStr))
        {
            string[] items = listStr.Split('|');
            list.AddRange(items);
        }

        // Add new value if it doesn't exist
        if (!list.Contains(objectName))
        {
            list.Add(objectName);
        }

        // Save back to PlayerPrefs
        PlayerPrefs.SetString("mainvr_reactivate", string.Join("|", list));
    }

    // Get the list of object names to reactivate
    private List<string> GetReactivationList()
    {
        List<string> result = new List<string>();
        string listStr = PlayerPrefs.GetString("mainvr_reactivate", "");

        if (!string.IsNullOrEmpty(listStr))
        {
            string[] items = listStr.Split('|');
            result.AddRange(items);
        }

        return result;
    }

    // Clear the reactivation list
    private void ClearReactivationList()
    {
        PlayerPrefs.DeleteKey("mainvr_reactivate");
    }

    // Coroutine to set the newly loaded scene as active
    private IEnumerator SetNewSceneActive(string sceneName)
    {
        // Wait until the scene is loaded
        yield return new WaitUntil(() => {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).name == sceneName)
                    return true;
            }
            return false;
        });

        // Find the scene and set it active
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.name == sceneName)
            {
                SceneManager.SetActiveScene(scene);

                if (debugMode)
                {
                    Debug.Log($"Set {sceneName} as active scene");
                }

                break;
            }
        }
    }

    // Reset title text after delay
    private IEnumerator ResetTitleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (TMP_title != null && !string.IsNullOrEmpty(title))
        {
            TMP_title.text = title;
            TMP_title.color = normalColor;
        }
    }
}

// Add this component to help with video link interactions
[RequireComponent(typeof(Collider))]
public class VideoLinkInteraction : MonoBehaviour
{
    private EnhancedVideoPlayer videoPlayer;
    private bool debugMode = false;

    private void Start()
    {
        // Get player component - either on this object or parent
        videoPlayer = GetComponent<EnhancedVideoPlayer>();
        if (videoPlayer == null)
        {
            videoPlayer = GetComponentInParent<EnhancedVideoPlayer>();
        }

        if (videoPlayer == null)
        {
            Debug.LogError("No EnhancedVideoPlayer component found!");
        }
        else
        {
            // Get debug mode from player
            debugMode = videoPlayer.debugMode;
        }
    }

    private void OnMouseEnter()
    {
        if (debugMode) Debug.Log($"Mouse enter on {gameObject.name}");

        if (videoPlayer != null)
        {
            videoPlayer.MouseHoverChangeScene();
        }
        else
        {
            // Try to find again in case it was added after Start
            videoPlayer = GetComponent<EnhancedVideoPlayer>() ?? GetComponentInParent<EnhancedVideoPlayer>();
            if (videoPlayer != null)
            {
                videoPlayer.MouseHoverChangeScene();
            }
        }
    }

    private void OnMouseExit()
    {
        if (debugMode) Debug.Log($"Mouse exit on {gameObject.name}");

        if (videoPlayer != null)
        {
            videoPlayer.MouseExit();
        }
        else
        {
            // Try to find again in case it was added after Start
            videoPlayer = GetComponent<EnhancedVideoPlayer>() ?? GetComponentInParent<EnhancedVideoPlayer>();
            if (videoPlayer != null)
            {
                videoPlayer.MouseExit();
            }
        }
    }
}

// Adapter for backward compatibility with ToggleShowHideVideo
// Use this instead of trying to inherit from ToggleShowHideVideo
public class VideoPlayerAdapter : MonoBehaviour
{
    // Reference to your existing component
    public Component originalToggleShowHideVideo;

    // The enhanced component we'll create
    private EnhancedVideoPlayer enhancedPlayer;

    // Properties to copy (in order of importance)
    private readonly string[] propertiesToCopy = new string[]
    {
        "VideoUrlLink",
        "returntoscene",
        "nextscene",
        "returnstage",
        "behaviour",
        "TMP_title",
        "hasText",
        "category",
        "zoneName"
    };

    private void Awake()
    {
        try
        {
            // Add the enhanced player component
            enhancedPlayer = gameObject.AddComponent<EnhancedVideoPlayer>();

            // If we have a reference to the original component, copy its values
            if (originalToggleShowHideVideo != null)
            {
                CopyPropertiesFromOriginal();
                DisableOriginalComponent();
            }
            else
            {
                Debug.LogWarning("VideoPlayerAdapter: No original component provided to adapt from.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"VideoPlayerAdapter initialization failed: {ex.Message}\n{ex.StackTrace}");
        }
    }

    // Copy properties from original component to enhanced player
    private void CopyPropertiesFromOriginal()
    {
        if (originalToggleShowHideVideo == null || enhancedPlayer == null)
            return;

        System.Type originalType = originalToggleShowHideVideo.GetType();
        Debug.Log($"Copying properties from {originalType.Name} to EnhancedVideoPlayer");

        foreach (string propertyName in propertiesToCopy)
        {
            try
            {
                // Try property copying first
                if (TryCopyProperty(originalType, propertyName))
                    continue;

                // If property copying fails, try field copying
                if (TryCopyField(originalType, propertyName))
                    continue;

                // If both fail, log a debug message (not an error, as some properties might be intentionally missing)
                Debug.Log($"Property or field '{propertyName}' not found or couldn't be copied");
            }
            catch (System.Exception ex)
            {
                // Log exception but continue with other properties
                Debug.LogWarning($"Error copying '{propertyName}': {ex.Message}");
            }
        }
    }

    // Try to copy a property using reflection
    private bool TryCopyProperty(System.Type originalType, string propertyName)
    {
        try
        {
            // Get property info from both types
            System.Reflection.PropertyInfo originalProp = originalType.GetProperty(propertyName);
            System.Reflection.PropertyInfo enhancedProp = typeof(EnhancedVideoPlayer).GetProperty(propertyName);

            // If either property doesn't exist, return false
            if (originalProp == null || enhancedProp == null)
                return false;

            // Check if property is readable/writable
            if (!originalProp.CanRead || !enhancedProp.CanWrite)
                return false;

            // Get the value from original and set it on enhanced
            object value = originalProp.GetValue(originalToggleShowHideVideo);

            // Skip null values for reference types
            if (value == null && !originalProp.PropertyType.IsValueType)
                return true;

            enhancedProp.SetValue(enhancedPlayer, value);
            return true;
        }
        catch
        {
            // Any exception means the property copy failed
            return false;
        }
    }

    // Try to copy a field using reflection
    private bool TryCopyField(System.Type originalType, string fieldName)
    {
        try
        {
            // Get field info from both types
            System.Reflection.FieldInfo originalField = originalType.GetField(fieldName);
            System.Reflection.FieldInfo enhancedField = typeof(EnhancedVideoPlayer).GetField(fieldName);

            // If either field doesn't exist, return false
            if (originalField == null || enhancedField == null)
                return false;

            // Get the value from original and set it on enhanced
            object value = originalField.GetValue(originalToggleShowHideVideo);

            // Skip null values for reference types
            if (value == null && !originalField.FieldType.IsValueType)
                return true;

            enhancedField.SetValue(enhancedPlayer, value);
            return true;
        }
        catch
        {
            // Any exception means the field copy failed
            return false;
        }
    }

    // Try to disable the original component
    private void DisableOriginalComponent()
    {
        try
        {
            // Try standard MonoBehaviour.enabled property first
            System.Reflection.PropertyInfo enabledProp = originalToggleShowHideVideo.GetType().GetProperty("enabled");
            if (enabledProp != null && enabledProp.PropertyType == typeof(bool) && enabledProp.CanWrite)
            {
                enabledProp.SetValue(originalToggleShowHideVideo, false);
                Debug.Log("Successfully disabled original component");
                return;
            }

            // If that doesn't work, try through SetActive on GameObject
            var originalBehavior = originalToggleShowHideVideo as Behaviour;
            if (originalBehavior != null)
            {
                originalBehavior.enabled = false;
                Debug.Log("Successfully disabled original behavior");
                return;
            }

            // If we can't disable it, log a message
            Debug.LogWarning("Could not disable original component. Consider doing this manually.");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Failed to disable original component: {ex.Message}");
        }
    }
}