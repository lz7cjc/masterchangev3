using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// SceneLoader - Optimized scene loading with XR coordination
/// More efficient than ChangeScene3d - uses coroutines properly and has better state management
/// </summary>
public class SceneLoader : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Name of the scene to load")]
    [SerializeField] private string targetSceneName;

    [Header("XR Management")]
    [Tooltip("Will auto-find if not assigned")]
    [SerializeField] private togglingXR xrToggler;

    [Header("HUD Integration")]
    [SerializeField] private ToggleActiveIcons toggleActiveIcons;
    [SerializeField] private HUDSystemCoordinator hudCoordinator;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    private bool isLoading = false;

    void Awake()
    {
        // Auto-find references
        if (xrToggler == null)
        {
            xrToggler = FindFirstObjectByType<togglingXR>();
        }

        if (toggleActiveIcons == null)
        {
            toggleActiveIcons = GetComponent<ToggleActiveIcons>();
        }

        if (hudCoordinator == null)
        {
            hudCoordinator = FindFirstObjectByType<HUDSystemCoordinator>();
        }

        LogDebug("SceneLoader initialized");
    }

    /// <summary>
    /// Load the target scene - called by GazeHoverTrigger
    /// </summary>
    public void LoadScene()
    {
        LoadScene(targetSceneName);
    }

    /// <summary>
    /// Load a specific scene by name
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (isLoading)
        {
            LogDebug("Already loading a scene, ignoring duplicate call");
            return;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneLoader] Scene name is empty! Cannot load scene.");
            return;
        }

        LogDebug($"Loading scene: {sceneName}");

        // Visual feedback
        if (toggleActiveIcons != null)
        {
            toggleActiveIcons.SelectIcon();
        }

        // Close HUDs
        if (hudCoordinator != null)
        {
            hudCoordinator.CloseAllHUDs();
        }

        // Save current zone before switching
        string currentZone = PlayerPrefs.GetString("lastknownzone", "");
        if (!string.IsNullOrEmpty(currentZone))
        {
            PlayerPrefs.SetString("returnzone", currentZone);
        }
        PlayerPrefs.Save();

        isLoading = true;

        // Stop XR before loading (synchronous - faster than coroutine)
        if (xrToggler != null)
        {
            xrToggler.StopXR();
        }

        // Load scene
        try
        {
            SceneManager.LoadScene(sceneName);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SceneLoader] Failed to load scene '{sceneName}': {e.Message}");
            isLoading = false;

            // Reset icon on error
            if (toggleActiveIcons != null)
            {
                toggleActiveIcons.DefaultIcon();
            }
        }
    }

    /// <summary>
    /// Load scene asynchronously (better for large scenes)
    /// </summary>
    public void LoadSceneAsync()
    {
        LoadSceneAsync(targetSceneName);
    }

    /// <summary>
    /// Load a specific scene asynchronously
    /// </summary>
    public void LoadSceneAsync(string sceneName)
    {
        if (isLoading) return;

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneLoader] Scene name is empty!");
            return;
        }

        StartCoroutine(LoadSceneAsyncCoroutine(sceneName));
    }

    private System.Collections.IEnumerator LoadSceneAsyncCoroutine(string sceneName)
    {
        isLoading = true;

        // Visual feedback
        if (toggleActiveIcons != null)
        {
            toggleActiveIcons.SelectIcon();
        }

        // Close HUDs
        if (hudCoordinator != null)
        {
            hudCoordinator.CloseAllHUDs();
        }

        // Stop XR
        if (xrToggler != null)
        {
            xrToggler.StopXR();
            yield return null; // Wait one frame for XR to stop
        }

        // Save zone info
        string currentZone = PlayerPrefs.GetString("lastknownzone", "");
        if (!string.IsNullOrEmpty(currentZone))
        {
            PlayerPrefs.SetString("returnzone", currentZone);
        }
        PlayerPrefs.Save();

        // Start async load
        LogDebug($"Starting async load of scene: {sceneName}");
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        // Wait until scene is loaded
        while (!asyncLoad.isDone)
        {
            LogDebug($"Loading progress: {asyncLoad.progress * 100}%");
            yield return null;
        }

        LogDebug($"Scene loaded: {sceneName}");
    }

    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[SceneLoader] {message}");
        }
    }

    #region Inspector Helpers

    [ContextMenu("Validate Scene Exists")]
    private void ValidateSceneExists()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning("[SceneLoader] Target scene name is not set!");
            return;
        }

        bool exists = false;
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

            if (sceneName == targetSceneName)
            {
                exists = true;
                Debug.Log($"[SceneLoader] ✓ Scene '{targetSceneName}' found at index {i}");
                break;
            }
        }

        if (!exists)
        {
            Debug.LogWarning($"[SceneLoader] ✗ Scene '{targetSceneName}' NOT in build settings!");
        }
    }

    [ContextMenu("Test Load Scene")]
    private void TestLoadScene()
    {
        if (Application.isPlaying)
        {
            LoadScene();
        }
        else
        {
            Debug.LogWarning("[SceneLoader] Can only test in Play mode");
        }
    }

    #endregion

    #region Public Properties

    public string TargetSceneName => targetSceneName;
    public bool IsLoading => isLoading;

    #endregion
}
