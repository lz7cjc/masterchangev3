using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// CLEANED: Scene changing with XR coordination
/// Handles scene transitions and ensures XR stops before loading new scene
/// </summary>
public class changeScene3d : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Name of the scene to load")]
    public string sceneName;
    
    [Tooltip("Hover time before scene change (seconds)")]
    public float hoverTimeRequired = 3f;

    [Header("References")]
    [Tooltip("Optional: Assign togglingXR manually, or leave empty for auto-find")]
    [SerializeField] private togglingXR togglingXR;

    // Runtime state
    private float hoverTimer = 0f;
    private bool isHovering = false;

    private void Awake()
    {
        // Find togglingXR if not assigned
        if (togglingXR == null)
        {
            togglingXR = FindObjectOfType<togglingXR>();
            if (togglingXR == null)
            {
                Debug.LogWarning("[ChangeScene3d] togglingXR not found. Assign in Inspector or ensure it exists in scene.");
            }
        }
    }

    void Update()
    {
        if (isHovering)
        {
            hoverTimer += Time.deltaTime;
            
            if (hoverTimer >= hoverTimeRequired)
            {
                // Reset state
                isHovering = false;
                hoverTimer = 0;

                // Stop XR before changing scene
                if (togglingXR != null)
                {
                    togglingXR.StopXR();
                }

                // Load scene
                if (!string.IsNullOrEmpty(sceneName))
                {
                    Debug.Log($"[ChangeScene3d] Loading scene: {sceneName}");
                    SceneManager.LoadScene(sceneName);
                }
                else
                {
                    Debug.LogError("[ChangeScene3d] Scene name is empty! Cannot load scene.");
                }
            }
        }
    }

    /// <summary>
    /// Called by GazeHoverTrigger when gaze enters
    /// </summary>
    public void MouseHoverChangeScene(string sceneNameToLoad)
    {
        sceneName = sceneNameToLoad;
        isHovering = true;
        hoverTimer = 0f;
        Debug.Log($"[ChangeScene3d] Started hover for scene: {sceneName}");
    }

    /// <summary>
    /// Called by GazeHoverTrigger when gaze exits
    /// </summary>
    public void MouseExit()
    {
        isHovering = false;
        hoverTimer = 0f;
        Debug.Log("[ChangeScene3d] Hover cancelled");
    }

    /// <summary>
    /// Alternative method - load scene immediately
    /// </summary>
    public void LoadSceneImmediate(string sceneNameToLoad)
    {
        if (togglingXR != null)
        {
            togglingXR.StopXR();
        }

        if (!string.IsNullOrEmpty(sceneNameToLoad))
        {
            Debug.Log($"[ChangeScene3d] Loading scene immediately: {sceneNameToLoad}");
            SceneManager.LoadScene(sceneNameToLoad);
        }
    }
}
