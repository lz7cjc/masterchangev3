using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles Stop button functionality: Stops video playback and loads "tips" scene.
/// Attach to Stop button GameObject alongside VRHUDButtonUI.
/// 
/// SETUP:
/// 1. Attach this script to the Stop button
/// 2. Set Scene Name to "tips"
/// 3. Assign Video Controller (or enable Auto Find)
/// 4. This script automatically subscribes to VRHUDButtonUI.OnButtonTriggered
/// </summary>
public class StopButtonSceneLoader : MonoBehaviour
{
    [Header("Scene Loading")]
    [SerializeField] private string sceneName = "tips";
    [Tooltip("Delay before loading scene (gives time for stop animation)")]
    [SerializeField] private float delayBeforeSceneLoad = 0.5f;

    [Header("Video Controller")]
    [SerializeField] private bool stopVideoFirst = true;
    [SerializeField] private MonoBehaviour videoController; // VRVideoController reference
    [SerializeField] private bool autoFindVideoController = true;

    [Header("References")]
    [SerializeField] private VRHUDButtonUI buttonUI;
    [SerializeField] private bool autoFindButtonUI = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private void Start()
    {
        // Auto-find VRHUDButtonUI
        if (autoFindButtonUI && buttonUI == null)
        {
            buttonUI = GetComponent<VRHUDButtonUI>();
        }

        if (buttonUI == null)
        {
            Debug.LogError("[StopButtonSceneLoader] No VRHUDButtonUI found! This script requires VRHUDButtonUI on the same GameObject.");
            enabled = false;
            return;
        }

        // Subscribe to button trigger event
        buttonUI.OnButtonTriggered.AddListener(OnStopButtonTriggered);

        // Auto-find video controller
        if (autoFindVideoController && videoController == null)
        {
            // Try to find by type name
            GameObject videoSphere = GameObject.Find("VideoSphere");
            if (videoSphere != null)
            {
                videoController = videoSphere.GetComponent<MonoBehaviour>();
                if (showDebugLogs)
                    Debug.Log($"[StopButtonSceneLoader] Auto-found VideoController: {videoController.GetType().Name}");
            }
        }

        if (showDebugLogs)
            Debug.Log($"[StopButtonSceneLoader] Initialized. Will load scene: {sceneName}");
    }

    /// <summary>
    /// Called when Stop button is triggered via VRHUDButtonUI
    /// </summary>
    private void OnStopButtonTriggered()
    {
        if (showDebugLogs)
            Debug.Log($"[StopButtonSceneLoader] Stop button triggered!");

        // Stop video first if enabled
        if (stopVideoFirst && videoController != null)
        {
            StopVideo();
        }

        // Load scene after delay
        if (!string.IsNullOrEmpty(sceneName))
        {
            if (delayBeforeSceneLoad > 0)
            {
                Invoke(nameof(LoadScene), delayBeforeSceneLoad);
                if (showDebugLogs)
                    Debug.Log($"[StopButtonSceneLoader] Will load scene '{sceneName}' in {delayBeforeSceneLoad}s");
            }
            else
            {
                LoadScene();
            }
        }
        else
        {
            Debug.LogWarning("[StopButtonSceneLoader] Scene name is empty! Cannot load scene.");
        }
    }

    /// <summary>
    /// Stops video playback
    /// </summary>
    private void StopVideo()
    {
        if (videoController == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("[StopButtonSceneLoader] No video controller assigned!");
            return;
        }

        // Try to call StopVideo method via reflection (works with any video controller)
        var stopMethod = videoController.GetType().GetMethod("StopVideo");
        if (stopMethod != null)
        {
            stopMethod.Invoke(videoController, null);
            if (showDebugLogs)
                Debug.Log("[StopButtonSceneLoader] Video stopped");
        }
        else
        {
            if (showDebugLogs)
                Debug.LogWarning("[StopButtonSceneLoader] VideoController doesn't have StopVideo method!");
        }
    }

    /// <summary>
    /// Loads the specified scene
    /// </summary>
    private void LoadScene()
    {
        if (showDebugLogs)
            Debug.Log($"<color=green>[StopButtonSceneLoader] Loading scene: {sceneName}</color>");

        // Check if scene exists in build settings
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError($"[StopButtonSceneLoader] Scene '{sceneName}' not found in Build Settings! " +
                          $"Add it via File → Build Settings → Add Open Scenes");
        }
    }

    /// <summary>
    /// Manual trigger for testing
    /// </summary>
    [ContextMenu("Test Stop and Load Scene")]
    public void TestStopAndLoadScene()
    {
        OnStopButtonTriggered();
    }

    /// <summary>
    /// Load scene immediately without delay (for testing)
    /// </summary>
    [ContextMenu("Load Scene Now")]
    public void LoadSceneNow()
    {
        LoadScene();
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (buttonUI != null)
        {
            buttonUI.OnButtonTriggered.RemoveListener(OnStopButtonTriggered);
        }
    }

    private void OnValidate()
    {
        // Validate scene name in editor
        if (!string.IsNullOrEmpty(sceneName))
        {
            // Check if scene exists in build settings
            bool sceneExists = false;
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                string sceneNameInBuild = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                if (sceneNameInBuild == sceneName)
                {
                    sceneExists = true;
                    break;
                }
            }

            if (!sceneExists)
            {
                Debug.LogWarning($"[StopButtonSceneLoader] Scene '{sceneName}' not found in Build Settings! " +
                                $"Add it via File → Build Settings");
            }
        }
    }
}