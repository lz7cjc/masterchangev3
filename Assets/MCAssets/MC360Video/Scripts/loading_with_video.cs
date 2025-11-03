using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.Video;
using TMPro;

/// <summary>
/// ENHANCED VERSION: Your loading.cs with integrated video streaming support
/// - All your original features preserved
/// - Added video download tracking
/// - Shows download speed and size on loading screen
/// - Compatible with Google Cloud Storage
/// - Uses [VRLOAD] debug prefix to match your style
/// </summary>
public class loading_with_video : MonoBehaviour
{
    [Header("Progress UI")]
    [SerializeField] private Image progressBar;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI percentageText;

    [Header("Settings")]
    [SerializeField] private float minimumLoadTime = 1.5f;
    [SerializeField] private string targetScene = "mainVR";

    [Header("Video Streaming (NEW!)")]
    [SerializeField] private bool loadVideoOnStart = false;
    [SerializeField] private string videoUrl = ""; // Your Google Cloud video URL
    [SerializeField] private VideoPlayer videoPlayer; // Optional: auto-prepare video
    [SerializeField] private bool showVideoDownloadSpeed = true;

    private float loadStartTime;

    // Video tracking
    private bool isDownloadingVideo = false;
    private UnityWebRequest videoRequest;
    private float lastVideoCheckTime;
    private ulong lastVideoBytes;
    private float currentVideoSpeed; // MB/s
    private ulong totalVideoBytes;
    private ulong videoContentLength;

    void Start()
    {
        Debug.Log("[VRLOAD] ========================================");
        Debug.Log("[VRLOAD] EVERYTHING SCENE - LOADING START");
        Debug.Log("[VRLOAD] Target scene: " + targetScene);
        if (loadVideoOnStart && !string.IsNullOrEmpty(videoUrl))
        {
            Debug.Log("[VRLOAD] Video loading enabled: " + videoUrl);
        }
        Debug.Log("[VRLOAD] ========================================");

        loadStartTime = Time.time;

        // Initialize UI
        if (progressBar != null)
        {
            progressBar.fillAmount = 0f;
        }

        // Start loading
        StartCoroutine(LoadMainVRScene());
    }

    /// <summary>
    /// ENHANCED: Load mainVR scene with optional video download
    /// </summary>
    IEnumerator LoadMainVRScene()
    {
        Debug.Log("[VRLOAD] === SCENE LOAD SEQUENCE START ===");

        // Phase 1: Start loading (0-10%)
        UpdateProgress(0.05f, "Initializing...");
        yield return new WaitForSeconds(0.2f);

        // NEW: Phase 1.5: Download video if enabled (10-40%)
        if (loadVideoOnStart && !string.IsNullOrEmpty(videoUrl))
        {
            Debug.Log("[VRLOAD] === VIDEO DOWNLOAD START ===");
            yield return StartCoroutine(DownloadVideo(0.1f, 0.4f)); // Progress range: 10% to 40%
            Debug.Log("[VRLOAD] === VIDEO DOWNLOAD COMPLETE ===");
        }

        // Phase 2: Begin async scene load (40-70% or 10-30% if no video)
        float sceneLoadStartProgress = loadVideoOnStart ? 0.4f : 0.1f;
        UpdateProgress(sceneLoadStartProgress, "Loading VR environment...");

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetScene);
        if (asyncLoad == null)
        {
            Debug.LogError($"[VRLOAD] Failed to start loading scene: {targetScene}");
            yield break;
        }

        // Prevent scene from activating until we're ready
        asyncLoad.allowSceneActivation = false;
        Debug.Log("[VRLOAD] Async load started");

        // Phase 3: Track loading progress
        float sceneProgressRange = loadVideoOnStart ? 0.3f : 0.6f; // 30% or 60% of total
        while (asyncLoad.progress < 0.9f)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            float displayProgress = sceneLoadStartProgress + (progress * sceneProgressRange);

            UpdateProgress(displayProgress, "Loading VR environment...");
            yield return null;
        }

        Debug.Log("[VRLOAD] ✓ Scene loaded to 90%");
        UpdateProgress(0.9f, "Preparing VR...");

        // Phase 4: Ensure minimum load time
        float elapsedTime = Time.time - loadStartTime;
        float remainingTime = minimumLoadTime - elapsedTime;

        if (remainingTime > 0)
        {
            Debug.Log($"[VRLOAD] Waiting {remainingTime:F2}s to meet minimum load time");
            float waitStart = Time.time;

            while (Time.time - waitStart < remainingTime)
            {
                float waitProgress = (Time.time - waitStart) / remainingTime;
                float displayProgress = 0.9f + (waitProgress * 0.05f); // 90-95%

                UpdateProgress(displayProgress, "Preparing VR...");
                yield return null;
            }
        }

        // Phase 5: Final preparation (95-100%)
        UpdateProgress(0.95f, "Almost ready...");
        yield return new WaitForSeconds(0.3f);

        UpdateProgress(1f, "Ready!");
        yield return new WaitForSeconds(0.2f);

        // Phase 6: Activate scene
        Debug.Log("[VRLOAD] Activating mainVR scene...");
        asyncLoad.allowSceneActivation = true;

        Debug.Log("[VRLOAD] ========================================");
        Debug.Log("[VRLOAD] EVERYTHING SCENE - LOADING COMPLETE ✓");
        Debug.Log($"[VRLOAD] Total load time: {Time.time - loadStartTime:F2}s");
        Debug.Log("[VRLOAD] ========================================");
    }

    /// <summary>
    /// NEW: Download video from Google Cloud with progress tracking
    /// </summary>
    IEnumerator DownloadVideo(float progressStart, float progressEnd)
    {
        Debug.Log($"[VRLOAD] Starting video download from: {videoUrl}");

        isDownloadingVideo = true;
        lastVideoCheckTime = Time.time;
        lastVideoBytes = 0;
        totalVideoBytes = 0;
        currentVideoSpeed = 0f;
        videoContentLength = 0;

        videoRequest = UnityWebRequest.Get(videoUrl);
        var operation = videoRequest.SendWebRequest();

        float progressRange = progressEnd - progressStart;

        while (!operation.isDone)
        {
            // Get content length from headers
            if (videoContentLength == 0)
            {
                string contentLength = videoRequest.GetResponseHeader("Content-Length");
                if (!string.IsNullOrEmpty(contentLength))
                {
                    ulong.TryParse(contentLength, out videoContentLength);
                    Debug.Log($"[VRLOAD] Video size: {FormatBytes(videoContentLength)}");
                }
            }

            // Update download stats
            totalVideoBytes = videoRequest.downloadedBytes;
            UpdateVideoStats();

            // Calculate progress within the allocated range
            float videoProgress = videoContentLength > 0
                ? (float)totalVideoBytes / videoContentLength
                : operation.progress;

            float displayProgress = progressStart + (videoProgress * progressRange);

            // Create status message with download info
            string status = "Downloading video...";
            if (showVideoDownloadSpeed && videoContentLength > 0)
            {
                status = $"Downloading video...\n{FormatBytes(totalVideoBytes)} / {FormatBytes(videoContentLength)}\nSpeed: {currentVideoSpeed:F2} MB/s";
            }

            UpdateProgress(displayProgress, status);
            yield return null;
        }

        isDownloadingVideo = false;

        if (videoRequest.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"[VRLOAD] ✓ Video downloaded successfully!");
            Debug.Log($"[VRLOAD] Total size: {FormatBytes(totalVideoBytes)}");
            Debug.Log($"[VRLOAD] Average speed: {CalculateAverageVideoSpeed():F2} MB/s");

            // Save video to temporary location
            string tempPath = System.IO.Path.Combine(Application.temporaryCachePath, "downloaded_video.mp4");
            System.IO.File.WriteAllBytes(tempPath, videoRequest.downloadHandler.data);
            Debug.Log($"[VRLOAD] Video saved to: {tempPath}");

            // Prepare video player if provided
            if (videoPlayer != null)
            {
                videoPlayer.url = tempPath;
                videoPlayer.Prepare();

                UpdateProgress(progressEnd, "Preparing video...");
                while (!videoPlayer.isPrepared)
                {
                    yield return null;
                }

                Debug.Log("[VRLOAD] ✓ Video prepared and ready to play");
            }
        }
        else
        {
            Debug.LogError($"[VRLOAD] ✗ Video download failed: {videoRequest.error}");
            Debug.LogError($"[VRLOAD] URL: {videoUrl}");
        }

        videoRequest.Dispose();
        videoRequest = null;
    }

    /// <summary>
    /// NEW: Update video download statistics
    /// </summary>
    void UpdateVideoStats()
    {
        if (!isDownloadingVideo || videoRequest == null) return;

        float currentTime = Time.time;
        float deltaTime = currentTime - lastVideoCheckTime;

        // Calculate speed every 0.5 seconds
        if (deltaTime >= 0.5f)
        {
            ulong bytesDownloaded = totalVideoBytes - lastVideoBytes;
            currentVideoSpeed = (bytesDownloaded / deltaTime) / (1024f * 1024f); // MB/s

            lastVideoBytes = totalVideoBytes;
            lastVideoCheckTime = currentTime;

            // Log progress every 2 seconds (every 4th update)
            if (Time.frameCount % 240 == 0) // ~4 seconds at 60fps
            {
                float progress = videoContentLength > 0 ? (float)totalVideoBytes / videoContentLength * 100f : 0f;
                Debug.Log($"[VRLOAD] Video: {progress:F1}% | {FormatBytes(totalVideoBytes)}/{FormatBytes(videoContentLength)} | {currentVideoSpeed:F2} MB/s");
            }
        }
    }

    /// <summary>
    /// NEW: Calculate average download speed
    /// </summary>
    float CalculateAverageVideoSpeed()
    {
        if (videoRequest == null) return 0f;

        float totalTime = Time.time - loadStartTime;
        if (totalTime > 0)
        {
            return (totalVideoBytes / totalTime) / (1024f * 1024f); // MB/s
        }
        return 0f;
    }

    /// <summary>
    /// NEW: Format bytes to human-readable string
    /// </summary>
    string FormatBytes(ulong bytes)
    {
        if (bytes == 0) return "0 B";

        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:F2} {sizes[order]}";
    }

    /// <summary>
    /// Update progress bar and status text (unchanged from original)
    /// </summary>
    void UpdateProgress(float progress, string status)
    {
        if (progressBar != null)
        {
            progressBar.fillAmount = progress;
        }

        if (statusText != null)
        {
            statusText.text = status;
        }

        if (percentageText != null)
        {
            percentageText.text = $"{(progress * 100):F0}%";
        }

        Debug.Log($"[VRLOAD] Progress: {(progress * 100):F0}% - {status}");
    }

    /// <summary>
    /// NEW: Public method to start video download from other scripts
    /// </summary>
    public void StartVideoDownload(string url)
    {
        videoUrl = url;
        loadVideoOnStart = true;
        StartCoroutine(DownloadVideo(0f, 1f));
    }

    private void Update()
    {
        // Update video stats every frame during download
        if (isDownloadingVideo)
        {
            UpdateVideoStats();
        }
    }
}