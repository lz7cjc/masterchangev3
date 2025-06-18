using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;

// Enhanced version of the video player with automatic Riro checking
public class EnhancedVideoPlayer : MonoBehaviour
{
    [Header("Video Configuration")]
    public string VideoUrlLink;
    public string title;
    public string description;
    public string prefabType;

    [Header("Zone Tracking")]
    [Tooltip("The zone the user was in when they selected this video")]
    public string LastKnownZone = "Home";

    [Header("Interaction Settings")]
    public float hoverTimeRequired = 3.0f;
    public bool mouseHover = false;
    private float hoverTimer = 0;

    [Header("Scene Management")]
    public string returntoscene = "mainVR";
    public string nextscene = "360VideoApp";
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

    // Riro currency check - AUTOMATIC (no editor configuration needed)
    private RiroStopGoV2 riroStopGoV2;
    private int riroAmount;
    private const int MIN_RIRO_REQUIRED = 50; // Fixed requirement, not configurable

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

    // Event Trigger support
    private EventTrigger eventTrigger;
    private BoxCollider boxCollider;

    private void Awake()
    {
        // Store original rotation
        originalRotation = transform.rotation;

        // Ensure we have required components for Event System
        SetupEventSystemComponents();

        // Create progress indicator if needed
        if (useProgressIndicator)
        {
            CreateProgressBar();
        }
    }

    // CRITICAL: Ensure Event System components are properly configured
    private void SetupEventSystemComponents()
    {
        // Ensure we have a BoxCollider configured as trigger
        boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider>();
            if (debugMode) Debug.Log($"Added BoxCollider to {gameObject.name}");
        }

        // Ensure it's a trigger for Event System
        if (!boxCollider.isTrigger)
        {
            boxCollider.isTrigger = true;
            if (debugMode) Debug.Log($"Set BoxCollider to trigger for {gameObject.name}");
        }

        // Get EventTrigger component (don't create here, let the editor/setup handle it)
        eventTrigger = GetComponent<EventTrigger>();
        if (eventTrigger != null && debugMode)
        {
            Debug.Log($"Found EventTrigger on {gameObject.name} with {eventTrigger.triggers?.Count ?? 0} triggers");
        }
    }

    void Start()
    {
        // AUTOMATIC: Find RiroStopGoV2 component in the scene
        FindRiroStopGoComponent();

        // Always check riro balance at start
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

        // Try to extract category from URL if not already set
        if (string.IsNullOrEmpty(category) && !string.IsNullOrEmpty(VideoUrlLink))
        {
            ExtractCategoryFromUrl();
        }

        // Determine current zone if LastKnownZone is not set
        if (string.IsNullOrEmpty(LastKnownZone) || LastKnownZone == "Home")
        {
            LastKnownZone = DetermineCurrentZone();
        }

        // Validate Event Trigger setup
        ValidateEventTriggerSetup();

        // Log initialization for debugging
        if (debugMode)
        {
            Debug.Log($"Initialized video player for: {title} ({VideoUrlLink})");
            Debug.Log($"Zone: {zoneName}, Category: {category}, LastKnownZone: {LastKnownZone}");
            Debug.Log($"Current Riro balance: {riroAmount}, Required: {MIN_RIRO_REQUIRED}");
            Debug.Log($"RiroStopGoV2 found: {(riroStopGoV2 != null ? "YES" : "NO")}");
            LogEventTriggerStatus();
        }
    }

    // AUTOMATIC: Find RiroStopGoV2 component in the scene
    private void FindRiroStopGoComponent()
    {
        // Try to find RiroStopGoV2 in the scene
        riroStopGoV2 = FindObjectOfType<RiroStopGoV2>();

        if (riroStopGoV2 == null)
        {
            // If not found, log warning but don't break functionality
            if (debugMode)
            {
                Debug.LogWarning("RiroStopGoV2 component not found in scene. Riro checking will be skipped.");
            }
        }
        else
        {
            if (debugMode)
            {
                Debug.Log($"✅ Found RiroStopGoV2 component on GameObject: {riroStopGoV2.gameObject.name}");
            }
        }
    }

    // CRITICAL: Validate that Event Triggers are properly set up
    private void ValidateEventTriggerSetup()
    {
        eventTrigger = GetComponent<EventTrigger>();

        if (eventTrigger == null)
        {
            if (debugMode) Debug.LogWarning($"No EventTrigger found on {gameObject.name}! Mouse/VR interactions won't work.");
            return;
        }

        if (eventTrigger.triggers == null || eventTrigger.triggers.Count == 0)
        {
            if (debugMode) Debug.LogWarning($"EventTrigger on {gameObject.name} has no triggers configured!");
            return;
        }

        // Check if triggers are properly configured
        bool hasPointerEnter = false;
        bool hasPointerExit = false;

        foreach (var trigger in eventTrigger.triggers)
        {
            if (trigger.eventID == EventTriggerType.PointerEnter)
            {
                hasPointerEnter = true;

                // Check if it calls the right method
                if (trigger.callback != null && trigger.callback.GetPersistentEventCount() > 0)
                {
                    for (int i = 0; i < trigger.callback.GetPersistentEventCount(); i++)
                    {
                        var target = trigger.callback.GetPersistentTarget(i);
                        var methodName = trigger.callback.GetPersistentMethodName(i);

                        if (target == this && methodName == "MouseHoverChangeScene")
                        {
                            if (debugMode) Debug.Log($"✅ PointerEnter properly configured on {gameObject.name}");
                        }
                    }
                }
            }
            else if (trigger.eventID == EventTriggerType.PointerExit)
            {
                hasPointerExit = true;

                // Check if it calls the right method
                if (trigger.callback != null && trigger.callback.GetPersistentEventCount() > 0)
                {
                    for (int i = 0; i < trigger.callback.GetPersistentEventCount(); i++)
                    {
                        var target = trigger.callback.GetPersistentTarget(i);
                        var methodName = trigger.callback.GetPersistentMethodName(i);

                        if (target == this && methodName == "MouseExit")
                        {
                            if (debugMode) Debug.Log($"✅ PointerExit properly configured on {gameObject.name}");
                        }
                    }
                }
            }
        }

        if (!hasPointerEnter && debugMode)
        {
            Debug.LogWarning($"EventTrigger on {gameObject.name} missing PointerEnter trigger!");
        }

        if (!hasPointerExit && debugMode)
        {
            Debug.LogWarning($"EventTrigger on {gameObject.name} missing PointerExit trigger!");
        }
    }

    private void LogEventTriggerStatus()
    {
        if (eventTrigger == null)
        {
            Debug.Log($"🔍 {gameObject.name}: No EventTrigger component");
            return;
        }

        Debug.Log($"🔍 {gameObject.name}: EventTrigger has {eventTrigger.triggers?.Count ?? 0} triggers");

        if (eventTrigger.triggers != null)
        {
            foreach (var trigger in eventTrigger.triggers)
            {
                string eventName = trigger.eventID.ToString();
                int callbackCount = trigger.callback?.GetPersistentEventCount() ?? 0;
                Debug.Log($"  - {eventName}: {callbackCount} callbacks");

                if (trigger.callback != null)
                {
                    for (int i = 0; i < trigger.callback.GetPersistentEventCount(); i++)
                    {
                        var target = trigger.callback.GetPersistentTarget(i);
                        var methodName = trigger.callback.GetPersistentMethodName(i);
                        string targetName = target != null ? target.GetType().Name : "null";
                        Debug.Log($"    [{i}] {targetName}.{methodName}");
                    }
                }
            }
        }
    }

    void Update()
    {
        // Safety check: Don't process if GameObject is inactive
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

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

    // Determine the current zone based on various methods
    private string DetermineCurrentZone()
    {
        // Method 1: Try to get from PlayerPrefs (last known zone)
        string lastKnown = PlayerPrefs.GetString("lastknownzone", "");
        if (!string.IsNullOrEmpty(lastKnown) && lastKnown != "Home")
        {
            if (debugMode) Debug.Log($"Using last known zone from PlayerPrefs: {lastKnown}");
            return lastKnown;
        }

        // Method 2: Try to determine from parent GameObject name or tag
        Transform parent = transform.parent;
        while (parent != null)
        {
            string parentName = parent.name.ToLower();

            // Check for zone-related keywords in parent names
            if (parentName.Contains("mindfulness")) return "Mindfulness";
            if (parentName.Contains("beach")) return "Beaches";
            if (parentName.Contains("travel")) return "Travel";
            if (parentName.Contains("sport")) return "Sport";
            if (parentName.Contains("height")) return "Heights";
            if (parentName.Contains("alcohol")) return "Alcohol";
            if (parentName.Contains("smoking")) return "Smoking";
            if (parentName.Contains("mfn")) return "MFN";

            parent = parent.parent;
        }

        // Method 3: Try to determine from the video's category/zone
        if (!string.IsNullOrEmpty(zoneName))
        {
            if (debugMode) Debug.Log($"Using video's zone name: {zoneName}");
            return zoneName;
        }

        // Method 4: Parse from video URL if it contains zone information
        if (!string.IsNullOrEmpty(VideoUrlLink))
        {
            string urlZone = ExtractZoneFromUrl();
            if (!string.IsNullOrEmpty(urlZone))
            {
                if (debugMode) Debug.Log($"Extracted zone from URL: {urlZone}");
                return urlZone;
            }
        }

        // Method 5: Default fallback
        if (debugMode) Debug.Log("Could not determine current zone, defaulting to Home");
        return "Home";
    }

    // Extract zone information from video URL
    private string ExtractZoneFromUrl()
    {
        if (string.IsNullOrEmpty(VideoUrlLink)) return "";

        string url = VideoUrlLink.ToLower();

        // Check for zone keywords in the URL
        if (url.Contains("/mfn/") || url.Contains("mindfulness")) return "Mindfulness";
        if (url.Contains("/heights/")) return "Heights";
        if (url.Contains("/beach") || url.Contains("brasil") || url.Contains("wight") || url.Contains("oman")) return "Beaches";
        if (url.Contains("/travel/")) return "Travel";
        if (url.Contains("football") || url.Contains("sport")) return "Sport";
        if (url.Contains("/alcohol/")) return "Alcohol";
        if (url.Contains("/smoking/")) return "Smoking";

        return "";
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

    // CRITICAL: Mouse Enter event - Called by EventTrigger
    public void MouseHoverChangeScene()
    {
        if (debugMode) Debug.Log($"🖱️ MouseHoverChangeScene called on {title} (EventTrigger method)");

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
            Debug.Log($"Mouse hover started on {title} in zone {LastKnownZone}");
        }
    }

    // CRITICAL: Mouse Exit Event - Called by EventTrigger
    public void MouseExit()
    {
        if (debugMode) Debug.Log($"🖱️ MouseExit called on {title} (EventTrigger method)");

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

    // Alternative method names for backward compatibility
    public void OnPointerEnter()
    {
        MouseHoverChangeScene();
    }

    public void OnPointerExit()
    {
        MouseExit();
    }

    // AUTOMATIC RIRO CHECKING: This method now automatically checks riros every time
    public void SetVideoUrl()
    {
        // Safety check: Don't proceed if GameObject is inactive
        if (!gameObject.activeInHierarchy)
        {
            if (debugMode) Debug.Log($"SetVideoUrl called on inactive GameObject {gameObject.name}, aborting");
            return;
        }

        if (debugMode)
        {
            Debug.Log($"🎬 SetVideoUrl called for: {title}");
            Debug.Log($"Setting video URL: {VideoUrlLink}");
            Debug.Log($"Return scene: {returntoscene}, Next scene: {nextscene}");
            Debug.Log($"Current LastKnownZone: {LastKnownZone}");
            Debug.Log($"Current zoneName: {zoneName}");
        }

        // Validate URL
        if (string.IsNullOrEmpty(VideoUrlLink))
        {
            Debug.LogError("Video URL is empty! Cannot play video.");
            return;
        }

        // AUTOMATIC RIRO CHECK: Always refresh riro balance and check it
        riroAmount = PlayerPrefs.GetInt("rirosBalance");

        if (debugMode)
        {
            Debug.Log($"💰 Current Riro balance: {riroAmount} (Required: {MIN_RIRO_REQUIRED})");
        }

        // CRITICAL: Check riro balance FIRST before doing anything else
        if (riroAmount < MIN_RIRO_REQUIRED)
        {
            Debug.Log($"❌ Insufficient riros to play video: {riroAmount} < {MIN_RIRO_REQUIRED}");

            // Set the stopFilm flag
            PlayerPrefs.SetInt("stopFilm", 0);

            // Try to find RiroStopGoV2 if we don't have it
            if (riroStopGoV2 == null)
            {
                FindRiroStopGoComponent();
            }

            if (riroStopGoV2 != null)
            {
                // Call RiroStopGoV2 to handle insufficient funds (0 = generic films)
                riroStopGoV2.doNotPass(0);

                if (debugMode)
                {
                    Debug.Log("✅ Called RiroStopGoV2.doNotPass(0) for insufficient riros");
                }
            }
            else
            {
                Debug.LogError("❌ RiroStopGoV2 component not found! Cannot show riro insufficient message.");

                // Fallback message if component is missing
                if (TMP_title != null)
                {
                    TMP_title.text = "Not enough Riros!";
                    TMP_title.color = Color.red;

                    // Reset after delay - but only if GameObject is active
                    if (gameObject.activeInHierarchy)
                    {
                        StartCoroutine(ResetTitleAfterDelay(2.0f));
                    }
                    else
                    {
                        // Can't start coroutine on inactive GameObject, use Invoke instead
                        if (debugMode) Debug.Log("GameObject inactive, skipping title reset coroutine");
                    }
                }
            }

            return; // Exit early if insufficient funds
        }

        // ✅ User has sufficient riros, proceed with video loading
        if (debugMode)
        {
            Debug.Log($"✅ Sufficient riros available ({riroAmount} >= {MIN_RIRO_REQUIRED}). Proceeding with video...");
        }

        // CRITICAL FIX: Ensure LastKnownZone is set to the current zone
        // Priority order: zoneName > LastKnownZone > DetermineCurrentZone()
        string zoneToUse = "";

        if (!string.IsNullOrEmpty(zoneName))
        {
            zoneToUse = zoneName;
            LastKnownZone = zoneName; // Update LastKnownZone to match
        }
        else if (!string.IsNullOrEmpty(LastKnownZone) && LastKnownZone != "Home")
        {
            zoneToUse = LastKnownZone;
        }
        else
        {
            zoneToUse = DetermineCurrentZone();
            LastKnownZone = zoneToUse; // Update LastKnownZone
        }

        if (debugMode)
        {
            Debug.Log($"Final zone to use: {zoneToUse}");
            Debug.Log($"Updated LastKnownZone to: {LastKnownZone}");
        }

        // Store scene navigation info
        PlayerPrefs.SetString("returntoscene", returntoscene);
        PlayerPrefs.SetString("nextscene", nextscene);

        // Store the zone information - THIS IS CRITICAL
        PlayerPrefs.SetString("lastknownzone", zoneToUse);

        // Store video metadata
        if (!string.IsNullOrEmpty(title))
        {
            PlayerPrefs.SetString("videoTitle", title);
        }

        if (!string.IsNullOrEmpty(description))
        {
            PlayerPrefs.SetString("videoDescription", description);
        }

        // Clear any previous stop film flag
        PlayerPrefs.DeleteKey("stopFilm");

        if (debugMode)
        {
            Debug.Log($"🎬 Loading video with {riroAmount} riros available");
            Debug.Log($"🏠 User will return to zone: {zoneToUse}");
        }

        PlayerPrefs.SetString("VideoUrl", VideoUrlLink);

        // Save PlayerPrefs immediately
        PlayerPrefs.Save();

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

            // Only start coroutine if GameObject is still active
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(SetNewSceneActive("360VideoApp"));
            }
            else
            {
                if (debugMode) Debug.Log("GameObject inactive, skipping SetNewSceneActive coroutine");
            }

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

    // Alternative method to reset title without coroutines (for when GameObject might become inactive)
    private void ResetTitleToNormal()
    {
        if (TMP_title != null && !string.IsNullOrEmpty(title))
        {
            TMP_title.text = title;
            TMP_title.color = normalColor;
        }
    }

    // CRITICAL: Public methods for manual triggering (useful for testing)
    [ContextMenu("Test Mouse Hover")]
    public void TestMouseHover()
    {
        MouseHoverChangeScene();
    }

    [ContextMenu("Test Mouse Exit")]
    public void TestMouseExit()
    {
        MouseExit();
    }

    [ContextMenu("Test Play Video")]
    public void TestPlayVideo()
    {
        SetVideoUrl();
    }

    [ContextMenu("Log Event Trigger Status")]
    public void LogEventTriggerStatusMenu()
    {
        LogEventTriggerStatus();
    }

    [ContextMenu("Find RiroStopGo Component")]
    public void FindRiroStopGoComponentMenu()
    {
        FindRiroStopGoComponent();
    }

    [ContextMenu("Test Insufficient Riros")]
    public void TestInsufficientRiros()
    {
        // Temporarily set low riro balance for testing
        int originalBalance = PlayerPrefs.GetInt("rirosBalance");
        PlayerPrefs.SetInt("rirosBalance", 10); // Set to insufficient amount

        if (debugMode)
        {
            Debug.Log($"Testing insufficient riros scenario (set balance to 10, was {originalBalance})");
        }

        SetVideoUrl();

        // Restore original balance after test
        PlayerPrefs.SetInt("rirosBalance", originalBalance);
    }

    // ENHANCED: Method to setup Event Triggers programmatically if needed
    public void SetupEventTriggersIfMissing()
    {
        eventTrigger = GetComponent<EventTrigger>();

        if (eventTrigger == null)
        {
            eventTrigger = gameObject.AddComponent<EventTrigger>();
            Debug.Log($"Added EventTrigger component to {gameObject.name}");
        }

        if (eventTrigger.triggers == null)
        {
            eventTrigger.triggers = new List<EventTrigger.Entry>();
        }

        // Check if we already have the required triggers
        bool hasPointerEnter = eventTrigger.triggers.Any(t => t.eventID == EventTriggerType.PointerEnter);
        bool hasPointerExit = eventTrigger.triggers.Any(t => t.eventID == EventTriggerType.PointerExit);

        if (!hasPointerEnter)
        {
            EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
            pointerEnter.eventID = EventTriggerType.PointerEnter;
            pointerEnter.callback.AddListener((data) => { MouseHoverChangeScene(); });
            eventTrigger.triggers.Add(pointerEnter);
            Debug.Log($"Added PointerEnter trigger to {gameObject.name}");
        }

        if (!hasPointerExit)
        {
            EventTrigger.Entry pointerExit = new EventTrigger.Entry();
            pointerExit.eventID = EventTriggerType.PointerExit;
            pointerExit.callback.AddListener((data) => { MouseExit(); });
            eventTrigger.triggers.Add(pointerExit);
            Debug.Log($"Added PointerExit trigger to {gameObject.name}");
        }

        if (debugMode)
        {
            Debug.Log($"✅ Event Triggers setup complete for {gameObject.name}");
            LogEventTriggerStatus();
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
        if (debugMode) Debug.Log($"OnMouseEnter on {gameObject.name}");

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
        if (debugMode) Debug.Log($"OnMouseExit on {gameObject.name}");

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
        "TMP_title",
        "hasText",
        "category",
        "zoneName",
        "LastKnownZone"
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