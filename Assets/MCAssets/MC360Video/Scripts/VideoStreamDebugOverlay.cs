using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using TMPro;

/// <summary>
/// Simple debug overlay for video streaming stats
/// Attach to any GameObject and it will display download info
/// </summary>
public class VideoStreamDebugOverlay : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool showOverlay = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.F1;

    [Header("UI (Optional - will create if null)")]
    [SerializeField] private TextMeshProUGUI debugText;

    private bool isTracking = false;
    private UnityWebRequest currentRequest;

    // Stats
    private float startTime;
    private float lastCheckTime;
    private ulong lastBytes;
    private float currentSpeed; // MB/s
    private ulong totalBytes;
    private ulong contentLength;

    private void Start()
    {
        // Create debug UI if not assigned
        if (debugText == null && showOverlay)
        {
            CreateDebugUI();
        }
    }

    private void Update()
    {
        // Toggle overlay
        if (Input.GetKeyDown(toggleKey))
        {
            showOverlay = !showOverlay;
            if (debugText != null)
            {
                debugText.gameObject.SetActive(showOverlay);
            }
        }

        // Update display
        if (showOverlay && isTracking && debugText != null)
        {
            UpdateDebugDisplay();
        }
    }

    /// <summary>
    /// Start tracking a video download
    /// Call this before starting your video download
    /// </summary>
    public void StartTracking(UnityWebRequest request)
    {
        currentRequest = request;
        isTracking = true;
        startTime = Time.time;
        lastCheckTime = Time.time;
        lastBytes = 0;
        totalBytes = 0;
        currentSpeed = 0;
        contentLength = 0;

        Debug.Log("[VIDEO-DEBUG] Started tracking video download");
    }

    /// <summary>
    /// Update and display current stats
    /// </summary>
    private void UpdateDebugDisplay()
    {
        if (currentRequest == null) return;

        // Get current download progress
        totalBytes = currentRequest.downloadedBytes;

        // Get content length from headers
        if (contentLength == 0)
        {
            string length = currentRequest.GetResponseHeader("Content-Length");
            if (!string.IsNullOrEmpty(length))
            {
                ulong.TryParse(length, out contentLength);
            }
        }

        // Calculate speed
        float deltaTime = Time.time - lastCheckTime;
        if (deltaTime >= 0.5f) // Update every 0.5s
        {
            ulong deltaBytes = totalBytes - lastBytes;
            currentSpeed = (deltaBytes / deltaTime) / (1024f * 1024f); // MB/s

            lastBytes = totalBytes;
            lastCheckTime = Time.time;
        }

        // Format display
        float progress = contentLength > 0 ? (float)totalBytes / contentLength * 100f : 0f;
        float elapsedTime = Time.time - startTime;
        float avgSpeed = elapsedTime > 0 ? (totalBytes / elapsedTime) / (1024f * 1024f) : 0f;

        string display = $"<b><color=#00FF00>VIDEO STREAM DEBUG</color></b>\n" +
                        $"<color=#FFFF00>━━━━━━━━━━━━━━━━━━━━━━━</color>\n" +
                        $"<b>Progress:</b> {progress:F1}%\n" +
                        $"<b>Downloaded:</b> {FormatBytes(totalBytes)} / {FormatBytes(contentLength)}\n" +
                        $"<b>Current Speed:</b> <color=#00FFFF>{currentSpeed:F2} MB/s</color>\n" +
                        $"<b>Average Speed:</b> {avgSpeed:F2} MB/s\n" +
                        $"<b>Elapsed Time:</b> {elapsedTime:F1}s\n" +
                        $"<b>Est. Remaining:</b> {EstimateTimeRemaining()}\n" +
                        $"<color=#FFFF00>━━━━━━━━━━━━━━━━━━━━━━━</color>\n" +
                        $"<size=10>Press {toggleKey} to toggle</size>";

        debugText.text = display;

        // Also log to console every 2 seconds
        if (Time.frameCount % 120 == 0) // ~2 seconds at 60fps
        {
            Debug.Log($"[VIDEO-DEBUG] {progress:F1}% | {FormatBytes(totalBytes)}/{FormatBytes(contentLength)} | {currentSpeed:F2} MB/s");
        }
    }

    /// <summary>
    /// Estimate remaining download time
    /// </summary>
    private string EstimateTimeRemaining()
    {
        if (currentSpeed <= 0 || contentLength == 0) return "Calculating...";

        ulong remainingBytes = contentLength - totalBytes;
        float remainingSeconds = (remainingBytes / (1024f * 1024f)) / currentSpeed;

        if (remainingSeconds < 60)
            return $"{remainingSeconds:F0}s";
        else if (remainingSeconds < 3600)
            return $"{(remainingSeconds / 60):F1}m";
        else
            return $"{(remainingSeconds / 3600):F1}h";
    }

    /// <summary>
    /// Stop tracking
    /// </summary>
    public void StopTracking()
    {
        isTracking = false;
        currentRequest = null;
        Debug.Log("[VIDEO-DEBUG] Stopped tracking");
    }

    /// <summary>
    /// Format bytes to readable string
    /// </summary>
    private string FormatBytes(ulong bytes)
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
    /// Create debug UI automatically
    /// </summary>
    private void CreateDebugUI()
    {
        // Create Canvas
        GameObject canvasObj = new GameObject("VideoStreamDebugCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // On top of everything

        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Create text
        GameObject textObj = new GameObject("DebugText");
        textObj.transform.SetParent(canvasObj.transform, false);

        debugText = textObj.AddComponent<TextMeshProUGUI>();
        debugText.fontSize = 14;
        debugText.color = Color.white;
        debugText.alignment = TextAlignmentOptions.TopLeft;

        // Position in top-left corner
        RectTransform rectTransform = textObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.pivot = new Vector2(0, 1);
        rectTransform.anchoredPosition = new Vector2(10, -10);
        rectTransform.sizeDelta = new Vector2(400, 300);

        Debug.Log("[VIDEO-DEBUG] Created debug UI overlay");
    }

    /// <summary>
    /// Example usage: Call this from your video loading code
    /// </summary>
    public static IEnumerator ExampleUsage(string videoUrl)
    {
        // Get or create debug tracker
        VideoStreamDebugOverlay tracker = FindObjectOfType<VideoStreamDebugOverlay>();
        if (tracker == null)
        {
            GameObject go = new GameObject("VideoStreamDebugger");
            tracker = go.AddComponent<VideoStreamDebugOverlay>();
        }

        Debug.Log($"[VIDEO-DEBUG] Starting download: {videoUrl}");

        // Create request
        UnityWebRequest request = UnityWebRequest.Get(videoUrl);

        // Start tracking
        tracker.StartTracking(request);

        // Send request
        yield return request.SendWebRequest();

        // Stop tracking
        tracker.StopTracking();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("[VIDEO-DEBUG] ✓ Download complete!");
            // Do something with request.downloadHandler.data
        }
        else
        {
            Debug.LogError($"[VIDEO-DEBUG] ✗ Download failed: {request.error}");
        }

        request.Dispose();
    }
}