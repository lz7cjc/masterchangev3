using UnityEngine;

/// <summary>
/// Quick test to check PlayerPrefs and manually trigger loading screen
/// Attach to any GameObject temporarily for testing
/// </summary>
public class LoadingScreenTester : MonoBehaviour
{
    [Header("Test Video URL")]
    [SerializeField] private string testVideoUrl = "https://storage.googleapis.com/YOUR-VIDEO-URL.mp4";

    void Start()
    {
        Debug.Log("========================================");
        Debug.Log("LOADING SCREEN TESTER");
        Debug.Log("========================================");

        // Check what's saved
        string savedUrl = PlayerPrefs.GetString("VideoUrl", "");
        Debug.Log($"Saved VideoUrl: '{savedUrl}'");

        if (string.IsNullOrEmpty(savedUrl))
        {
            Debug.LogWarning("NO VIDEO URL SAVED! This is why loading screen doesn't show.");
            Debug.LogWarning("Press '1' to save a test URL");
        }

        Debug.Log("Press '2' to manually show loading screen");
        Debug.Log("Press '3' to hide loading screen");
        Debug.Log("========================================");
    }

    void Update()
    {
        // Press 1 to save test URL
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            PlayerPrefs.SetString("VideoUrl", testVideoUrl);
            PlayerPrefs.Save();
            Debug.Log($"✓ Saved test URL: {testVideoUrl}");
            Debug.Log("Now restart the scene to test!");
        }

        // Press 2 to manually show loading screen
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Debug.Log("Manually showing loading screen...");
            if (VRLoadingManager.Instance != null)
            {
                VRLoadingManager.Instance.ShowLoading("Testing loading screen...", 0f);
                Debug.Log("✓ Loading screen should now be visible!");
            }
            else
            {
                Debug.LogError("✗ VRLoadingManager.Instance is NULL!");
            }
        }

        // Press 3 to hide loading screen
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Debug.Log("Hiding loading screen...");
            if (VRLoadingManager.Instance != null)
            {
                VRLoadingManager.Instance.UpdateProgress(1f);
                VRLoadingManager.Instance.HideLoading();
                Debug.Log("✓ Loading screen hidden");
            }
        }
    }
}