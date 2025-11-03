using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;
using TMPro;

/// <summary>
/// Tracks video download progress from Google Cloud and displays on loading screen
/// - Shows download speed (MB/s)
/// - Shows total downloaded size
/// - Shows percentage complete
/// - Integrates with VRLoadingManager
/// </summary>
public class VideoStreamDownloadTracker : MonoBehaviour
{
    [Header("Video Settings")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private string videoUrl; // Your Google Cloud video URL

    [Header("Debug Display (Optional)")]
    [SerializeField] private TextMeshProUGUI debugText;
    [SerializeField] private bool showDebugInConsole = true;

    [Header("Loading Screen Integration")]
    [SerializeField] private bool useLoadingScreen = true;

    // Download tracking
    private float downloadStartTime;
    private float lastUpdateTime;
    private ulong lastDownloadedBytes;
    private float currentDownloadSpeed; // MB/s
    private ulong totalDownloadedBytes;
    private ulong totalContentLength;
    private bool isDownloading = false;

    private void Start()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        if (!string.IsNullOrEmpty(videoUrl))
        {
            StartCoroutine(LoadVideoWithProgress(videoUrl));
        }
    }

    /// <summary>
    /// Load video with detailed progress tracking
    /// </summary>
    public IEnumerator LoadVideoWithProgress(string url)
    {
        Debug.Log("[VIDEO-STREAM] ========================================");
        Debug.Log($"[VIDEO-STREAM] Starting video download from: {url}");
        Debug.Log("[VIDEO-STREAM] ========================================");

        // Show loading screen
        if (useLoadingScreen && VRLoadingManager.Instance != null)
        {
            VRLoadingManager.Instance.ShowLoading("Loading video...", 0f);
        }

        isDownloading = true;
        downloadStartTime = Time.time;
        lastUpdateTime = Time.time;
        lastDownloadedBytes = 0;
        totalDownloadedBytes = 0;
        currentDownloadSpeed = 0f;

        // Create UnityWebRequest to track download
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            // Send the request
            var operation = request.SendWebRequest();

            // Get total file size from headers
            while (!operation.isDone)
            {
                if (request.downloadHandler != null && totalContentLength == 0)
                {
                    string contentLength = request.GetResponseHeader("Content-Length");
                    if (!string.IsNullOrEmpty(contentLength))
                    {
                        totalContentLength = ulong.Parse(contentLength);
                        Debug.Log($"[VIDEO-STREAM] Total video size: {FormatBytes(totalContentLength)}");
                    }
                }

                // Update progress
                totalDownloadedBytes = request.downloadedBytes;
                UpdateDownloadStats();

                yield return null;
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[VIDEO-STREAM] ✓ Video downloaded successfully!");
                Debug.Log($"[VIDEO-STREAM] Total size: {FormatBytes(totalDownloadedBytes)}");
                Debug.Log($"[VIDEO-STREAM] Average speed: {CalculateAverageSpeed():F2} MB/s");
                Debug.Log($"[VIDEO-STREAM] Total time: {(Time.time - downloadStartTime):F2}s");

                // Save to temporary location and load in VideoPlayer
                string tempPath = System.IO.Path.Combine(Application.temporaryCachePath, "temp_video.mp4");
                System.IO.File.WriteAllBytes(tempPath, request.downloadHandler.data);

                videoPlayer.url = tempPath;
                videoPlayer.Prepare();

                // Wait for video to be prepared
                while (!videoPlayer.isPrepared)
                {
                    yield return null;
                }

                Debug.Log("[VIDEO-STREAM] ✓ Video prepared and ready to play");

                // Hide loading screen
                if (useLoadingScreen && VRLoadingManager.Instance != null)
                {
                    VRLoadingManager.Instance.HideLoading();
                }
            }
            else
            {
                Debug.LogError($"[VIDEO-STREAM] ✗ Download failed: {request.error}");
                Debug.LogError($"[VIDEO-STREAM] URL: {url}");

                if (useLoadingScreen && VRLoadingManager.Instance != null)
                {
                    VRLoadingManager.Instance.UpdateStatus("Video download failed!");
                    yield return new WaitForSeconds(2f);
                    VRLoadingManager.Instance.HideLoading();
                }
            }
        }

        isDownloading = false;
        Debug.Log("[VIDEO-STREAM] ========================================");
    }

    /// <summary>
    /// Update download statistics (speed, progress, etc.)
    /// </summary>
    private void UpdateDownloadStats()
    {
        float currentTime = Time.time;
        float deltaTime = currentTime - lastUpdateTime;

        // Calculate download speed every 0.5 seconds
        if (deltaTime >= 0.5f)
        {
            ulong bytesDownloaded = totalDownloadedBytes - lastDownloadedBytes;
            currentDownloadSpeed = (bytesDownloaded / deltaTime) / (1024f * 1024f); // Convert to MB/s

            lastDownloadedBytes = totalDownloadedBytes;
            lastUpdateTime = currentTime;

            // Calculate progress
            float progress = totalContentLength > 0 ? (float)totalDownloadedBytes / totalContentLength : 0f;

            // Log to console
            if (showDebugInConsole)
            {
                Debug.Log($"[VIDEO-STREAM] Progress: {progress * 100:F1}% | " +
                         $"Downloaded: {FormatBytes(totalDownloadedBytes)} / {FormatBytes(totalContentLength)} | " +
                         $"Speed: {currentDownloadSpeed:F2} MB/s");
            }

            // Update loading screen
            if (useLoadingScreen && VRLoadingManager.Instance != null)
            {
                VRLoadingManager.Instance.UpdateProgress(progress);
                VRLoadingManager.Instance.UpdateStatus(
                    $"Loading video... {FormatBytes(totalDownloadedBytes)} / {FormatBytes(totalContentLength)}\n" +
                    $"Speed: {currentDownloadSpeed:F2} MB/s"
                );
            }

            // Update debug UI text
            if (debugText != null)
            {
                debugText.text = $"VIDEO DOWNLOAD\n" +
                                $"Progress: {progress * 100:F1}%\n" +
                                $"Downloaded: {FormatBytes(totalDownloadedBytes)} / {FormatBytes(totalContentLength)}\n" +
                                $"Speed: {currentDownloadSpeed:F2} MB/s\n" +
                                $"Time: {(Time.time - downloadStartTime):F1}s";
            }
        }
    }

    /// <summary>
    /// Calculate average download speed over entire download
    /// </summary>
    private float CalculateAverageSpeed()
    {
        float totalTime = Time.time - downloadStartTime;
        if (totalTime > 0)
        {
            return (totalDownloadedBytes / totalTime) / (1024f * 1024f); // MB/s
        }
        return 0f;
    }

    /// <summary>
    /// Format bytes to human-readable string (KB, MB, GB)
    /// </summary>
    private string FormatBytes(ulong bytes)
    {
        if (bytes == 0) return "0 B";

        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
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
    /// Public method to start download from external scripts
    /// </summary>
    public void StartVideoDownload(string url)
    {
        videoUrl = url;
        StartCoroutine(LoadVideoWithProgress(url));
    }

    /// <summary>
    /// Get current download statistics as a formatted string
    /// </summary>
    public string GetDownloadStats()
    {
        if (!isDownloading) return "Not downloading";

        float progress = totalContentLength > 0 ? (float)totalDownloadedBytes / totalContentLength : 0f;
        return $"Progress: {progress * 100:F1}% | " +
               $"Size: {FormatBytes(totalDownloadedBytes)} / {FormatBytes(totalContentLength)} | " +
               $"Speed: {currentDownloadSpeed:F2} MB/s";
    }

    private void Update()
    {
        // Update stats every frame during download
        if (isDownloading)
        {
            UpdateDownloadStats();
        }
    }
}