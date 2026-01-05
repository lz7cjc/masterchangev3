using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Dashboard button handler - loads "everything" loading scene
/// Everything scene then handles the async load to mainVR
/// OPTIMIZED: Simple, fast transition to loading scene
/// </summary>
public class toMasterChange : MonoBehaviour
{
    [Header("Settings")]
    public bool launchmainvrToggle = true;

    [Header("Scene Names")]
    [SerializeField] private string loadingSceneName = "everything";
    [SerializeField] private string switchToVRScene = "switchtoVR";
    [SerializeField] private string switchTo360Scene = "switchto360";

    /// <summary>
    /// PUBLIC: Called by Dashboard button
    /// </summary>
    public void MasterChange()
    {
        Debug.Log("[toMasterChange] ==========================================");
        Debug.Log("[toMasterChange] BUTTON CLICKED");
        Debug.Log("[toMasterChange] ==========================================");

        int skipSwitchScreen = PlayerPrefs.GetInt("SwitchtoVR", 0);
        int toggleToVR = PlayerPrefs.GetInt("toggleToVR", 0);

        Debug.Log($"[toMasterChange] launchmainvrToggle: {launchmainvrToggle}");
        Debug.Log($"[toMasterChange] SkipSwitchScreen: {skipSwitchScreen}");
        Debug.Log($"[toMasterChange] toggleToVR: {toggleToVR}");

        // Determine target scene
        string targetScene = DetermineTargetScene(skipSwitchScreen, toggleToVR);
        
        Debug.Log($"[toMasterChange] Loading: {targetScene}");
        Debug.Log("[toMasterChange] ==========================================");

        // Load the scene (synchronous is fine for lightweight loading scene)
        SceneManager.LoadScene(targetScene);
    }

    /// <summary>
    /// Determine which scene to load based on settings
    /// </summary>
    string DetermineTargetScene(int skipSwitch, int vrMode)
    {
        // If launch toggle enabled, always go to loading scene
        if (launchmainvrToggle)
        {
            return loadingSceneName;
        }

        // Otherwise check if we should show switch screen
        if (skipSwitch == 0)
        {
            return vrMode == 1 ? switchToVRScene : switchTo360Scene;
        }

        // Skip switch screen, go to loading scene
        return loadingSceneName;
    }

    /// <summary>
    /// OPTIONAL: Direct load to loading scene (for other buttons)
    /// </summary>
    public void LoadMainVR()
    {
        Debug.Log($"[toMasterChange] Direct load to {loadingSceneName}");
        SceneManager.LoadScene(loadingSceneName);
    }
}
