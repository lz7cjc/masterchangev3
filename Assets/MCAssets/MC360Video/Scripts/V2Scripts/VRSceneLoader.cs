using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Simple scene loader for HUD buttons.
/// Attach to a button and connect to OnButtonTriggered event.
/// </summary>
public class VRSceneLoader : MonoBehaviour
{
    [Header("Scene to Load")]
    [SerializeField] private string sceneName = "MainMenu";
    [SerializeField] private int sceneIndex = -1; // -1 = use sceneName instead

    [Header("Options")]
    [SerializeField] private bool useSceneIndex = false; // Use index or name?
    [SerializeField] private float delayBeforeLoad = 0f; // Optional delay

    /// <summary>
    /// Load the configured scene
    /// </summary>
    public void LoadScene()
    {
        if (delayBeforeLoad > 0)
        {
            Invoke(nameof(LoadSceneNow), delayBeforeLoad);
            Debug.Log($"[VRSceneLoader] Loading scene in {delayBeforeLoad} seconds...");
        }
        else
        {
            LoadSceneNow();
        }
    }

    private void LoadSceneNow()
    {
        if (useSceneIndex)
        {
            if (sceneIndex >= 0 && sceneIndex < SceneManager.sceneCountInBuildSettings)
            {
                Debug.Log($"[VRSceneLoader] Loading scene index: {sceneIndex}");
                SceneManager.LoadScene(sceneIndex);
            }
            else
            {
                Debug.LogError($"[VRSceneLoader] Invalid scene index: {sceneIndex}");
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(sceneName))
            {
                Debug.Log($"[VRSceneLoader] Loading scene: {sceneName}");
                SceneManager.LoadScene(sceneName);
            }
            else
            {
                Debug.LogError("[VRSceneLoader] Scene name is empty!");
            }
        }
    }

    /// <summary>
    /// Load a specific scene by name
    /// </summary>
    public void LoadSceneByName(string name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            Debug.Log($"[VRSceneLoader] Loading scene: {name}");
            SceneManager.LoadScene(name);
        }
    }

    /// <summary>
    /// Load a specific scene by index
    /// </summary>
    public void LoadSceneByIndex(int index)
    {
        if (index >= 0 && index < SceneManager.sceneCountInBuildSettings)
        {
            Debug.Log($"[VRSceneLoader] Loading scene index: {index}");
            SceneManager.LoadScene(index);
        }
    }

    /// <summary>
    /// Reload the current scene
    /// </summary>
    public void ReloadCurrentScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        Debug.Log($"[VRSceneLoader] Reloading scene: {currentScene.name}");
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    /// <summary>
    /// Load the next scene in build settings
    /// </summary>
    public void LoadNextScene()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = currentIndex + 1;

        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            Debug.Log($"[VRSceneLoader] Loading next scene index: {nextIndex}");
            SceneManager.LoadScene(nextIndex);
        }
        else
        {
            Debug.LogWarning("[VRSceneLoader] No next scene - already at last scene");
        }
    }

    /// <summary>
    /// Load the previous scene in build settings
    /// </summary>
    public void LoadPreviousScene()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int prevIndex = currentIndex - 1;

        if (prevIndex >= 0)
        {
            Debug.Log($"[VRSceneLoader] Loading previous scene index: {prevIndex}");
            SceneManager.LoadScene(prevIndex);
        }
        else
        {
            Debug.LogWarning("[VRSceneLoader] No previous scene - already at first scene");
        }
    }

    /// <summary>
    /// Quit the application
    /// </summary>
    public void QuitApplication()
    {
        Debug.Log("[VRSceneLoader] Quitting application");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}