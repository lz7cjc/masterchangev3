using UnityEngine;
using UnityEngine.Video;
using System.Collections;

/// <summary>
/// FINAL VERSION: Video loader with guaranteed visible loading screen
/// - Fixed case sensitivity bug ✓
/// - Shows loading screen for minimum time (always visible!) ✓
/// - Proper video preparation ✓
/// - Better debug output ✓
/// </summary>
public class VideoLoader : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private bool autoPlayOnLoad = true;

    [Header("Loading Screen")]
    [Tooltip("Minimum time loading screen stays visible (seconds)")]
    [SerializeField] private float minimumLoadingTime = 1.5f;

    // FIXED: Consistent key name
    private const string VIDEO_URL_KEY = "VideoUrl";

    void Start()
    {
        Debug.Log("[VIDEO-LOADER] ========================================");
        Debug.Log("[VIDEO-LOADER] Initializing video loader...");

        string url = PlayerPrefs.GetString(VIDEO_URL_KEY, "");
        Debug.Log($"[VIDEO-LOADER] Retrieved URL from PlayerPrefs: '{url}'");

        if (!string.IsNullOrEmpty(url))
        {
            Debug.Log($"[VIDEO-LOADER] Loading video from: {url}");
            StartCoroutine(LoadVideoWithLoadingScreen(url));
        }
        else
        {
            Debug.LogWarning("[VIDEO-LOADER] No video URL found in PlayerPrefs!");
            Debug.LogWarning($"[VIDEO-LOADER] Looking for key: '{VIDEO_URL_KEY}'");
        }

        Debug.Log("[VIDEO-LOADER] ========================================");
    }

    /// <summary>
    /// Load video with loading screen (guaranteed visible time)
    /// </summary>
    IEnumerator LoadVideoWithLoadingScreen(string url)
    {
        float loadStartTime = Time.time; // Track when we started

        // SHOW LOADING SCREEN
        if (VRLoadingManager.Instance != null)
        {
            VRLoadingManager.Instance.ShowLoading("Loading video...", 0f);
            Debug.Log("[VIDEO-LOADER] ✓ Loading screen shown");
        }
        else
        {
            Debug.LogWarning("[VIDEO-LOADER] VRLoadingManager not found - no loading screen will show");
        }

        // Validate
        if (videoPlayer == null)
        {
            Debug.LogError("[VIDEO-LOADER] VideoPlayer is not assigned!");
            yield break;
        }

        if (string.IsNullOrEmpty(url))
        {
            Debug.LogError("[VIDEO-LOADER] Video URL is empty!");
            yield break;
        }

        // Set URL and prepare
        Debug.Log($"[VIDEO-LOADER] Setting video URL: {url}");
        videoPlayer.url = url;

        Debug.Log("[VIDEO-LOADER] Preparing video...");
        videoPlayer.Prepare();

        // Wait for video to prepare with progress updates
        float prepareStartTime = Time.time;
        while (!videoPlayer.isPrepared)
        {
            // Update loading progress (simulated - 50% while preparing)
            if (VRLoadingManager.Instance != null)
            {
                VRLoadingManager.Instance.UpdateProgress(0.5f);
                VRLoadingManager.Instance.UpdateStatus("Loading video...");
            }

            // Timeout after 30 seconds
            if (Time.time - prepareStartTime > 30f)
            {
                Debug.LogError("[VIDEO-LOADER] Video preparation timeout!");
                if (VRLoadingManager.Instance != null)
                {
                    VRLoadingManager.Instance.UpdateStatus("Load failed!");
                    yield return new WaitForSeconds(1f);
                    VRLoadingManager.Instance.HideLoading();
                }
                yield break;
            }

            yield return null;
        }

        Debug.Log("[VIDEO-LOADER] ✓ Video prepared successfully!");
        Debug.Log($"[VIDEO-LOADER] Video length: {videoPlayer.length:F2}s");
        Debug.Log($"[VIDEO-LOADER] Video size: {videoPlayer.width}x{videoPlayer.height}");

        // ENSURE MINIMUM LOADING TIME (so user can see the loading screen!)
        float elapsedTime = Time.time - loadStartTime;
        float remainingTime = minimumLoadingTime - elapsedTime;

        if (remainingTime > 0)
        {
            Debug.Log($"[VIDEO-LOADER] Video loaded quickly! Waiting {remainingTime:F2}s more so loading screen is visible...");

            // Update progress smoothly during wait
            if (VRLoadingManager.Instance != null)
            {
                VRLoadingManager.Instance.UpdateStatus("Ready!");

                // Animate progress from 50% to 100% during wait
                float waitStart = Time.time;
                while (Time.time - waitStart < remainingTime)
                {
                    float waitProgress = (Time.time - waitStart) / remainingTime;
                    float displayProgress = 0.5f + (waitProgress * 0.5f); // 50% to 100%
                    VRLoadingManager.Instance.UpdateProgress(displayProgress);
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSeconds(remainingTime);
            }
        }

        // HIDE LOADING SCREEN
        if (VRLoadingManager.Instance != null)
        {
            VRLoadingManager.Instance.UpdateProgress(1f);
            VRLoadingManager.Instance.UpdateStatus("Ready!");
            yield return new WaitForSeconds(0.3f);
            VRLoadingManager.Instance.HideLoading();
            Debug.Log("[VIDEO-LOADER] ✓ Loading screen hidden");
        }

        // Play video
        if (autoPlayOnLoad)
        {
            Debug.Log("[VIDEO-LOADER] Starting playback...");
            videoPlayer.Play();
        }
    }

    /// <summary>
    /// Load video without coroutine (legacy support)
    /// </summary>
    public void LoadVideo(string url)
    {
        StartCoroutine(LoadVideoWithLoadingScreen(url));
    }

    /// <summary>
    /// Save video URL to PlayerPrefs
    /// </summary>
    public void SaveVideoURL(string url)
    {
        Debug.Log("[VIDEO-LOADER] ========================================");
        Debug.Log($"[VIDEO-LOADER] Saving video URL: {url}");
        Debug.Log($"[VIDEO-LOADER] Using key: '{VIDEO_URL_KEY}'");

        PlayerPrefs.SetString(VIDEO_URL_KEY, url);
        PlayerPrefs.Save();

        Debug.Log("[VIDEO-LOADER] ✓ Video URL saved successfully!");
        Debug.Log("[VIDEO-LOADER] ========================================");
    }

    /// <summary>
    /// Get currently saved video URL
    /// </summary>
    public string GetSavedVideoURL()
    {
        return PlayerPrefs.GetString(VIDEO_URL_KEY, "");
    }

    /// <summary>
    /// Clear saved video URL
    /// </summary>
    public void ClearSavedVideoURL()
    {
        Debug.Log("[VIDEO-LOADER] Clearing saved video URL...");
        PlayerPrefs.DeleteKey(VIDEO_URL_KEY);
        PlayerPrefs.Save();
        Debug.Log("[VIDEO-LOADER] ✓ Video URL cleared");
    }

    /// <summary>
    /// Check if a video URL is saved
    /// </summary>
    public bool HasSavedVideoURL()
    {
        return PlayerPrefs.HasKey(VIDEO_URL_KEY);
    }
}