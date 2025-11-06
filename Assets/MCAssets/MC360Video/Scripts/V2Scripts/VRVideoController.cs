using UnityEngine;
using UnityEngine.Video;
using UnityEngine.Events;
using TMPro;
using System;
using System.Collections;

/// <summary>
/// Controls video playback and notifies VRLoadingManager when ready.
/// Handles: play, pause, stop, speed control, time display.
/// Does NOT handle UI interaction (that's VRHUDButton's job).
/// Attach to VideoPlayer GameObject.
/// </summary>
public class VRVideoController : MonoBehaviour
{
    [Header("Video Players")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private VideoPlayer audioPlayer;

    [Header("Time Display")]
    [SerializeField] private TMP_Text elapsedTimeText;
    [SerializeField] private TMP_Text totalTimeText;

    [Header("Play/Pause Toggle Objects")]
    [SerializeField] private GameObject playIcon;
    [SerializeField] private GameObject pauseIcon;

    [Header("Events")]
    public UnityEvent OnVideoReady;
    public UnityEvent OnVideoStarted;
    public UnityEvent OnVideoFinished;

    // State
    private bool isReady = false;
    private bool isPlaying = false;

    void Awake()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        // Subscribe to video player events
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.loopPointReached += OnVideoEnd;
        videoPlayer.errorReceived += OnVideoError;

        // Additional event for frame ready (shows actual loading progress)
        videoPlayer.frameReady += OnFrameReady;
    }

    void Start()
    {
        Debug.Log("[VRVideoController] Loading video from URL...");

        // Show loading screen
        if (VRLoadingManager.Instance != null)
        {
            VRLoadingManager.Instance.ShowLoading("Initializing video player...", 0f);
        }

        // Load video URL from PlayerPrefs
        string videoUrl = PlayerPrefs.GetString("VideoUrl", "");

        if (string.IsNullOrEmpty(videoUrl))
        {
            Debug.LogError("[VRVideoController] No video URL found!");

            if (VRLoadingManager.Instance != null)
            {
                VRLoadingManager.Instance.UpdateStatus("Error: No video URL provided");
            }

            return;
        }

        videoPlayer.url = videoUrl;
        if (audioPlayer != null)
            audioPlayer.url = videoUrl;

        if (VRLoadingManager.Instance != null)
        {
            VRLoadingManager.Instance.UpdateStatus($"Connecting to video source...");
        }

        Debug.Log($"[VRVideoController] Video URL: {videoUrl}");

        // Prepare the video (streams/downloads)
        videoPlayer.Prepare();

        // Simulate progress (since we can't track actual download)
        StartCoroutine(SimulateLoadingProgress());
    }

    private void OnFrameReady(VideoPlayer vp, long frameIdx)
    {
        // First frame ready - video is actually loading
        if (frameIdx == 0 && !isReady)
        {
            Debug.Log("[VRVideoController] First frame ready");

            if (VRLoadingManager.Instance != null)
            {
                VRLoadingManager.Instance.UpdateStatus("Video loaded successfully");
                VRLoadingManager.Instance.UpdateProgress(0.95f);
            }
        }
    }

    void Update()
    {
        // Update elapsed time display
        if (isPlaying && elapsedTimeText != null)
        {
            TimeSpan elapsed = TimeSpan.FromSeconds(videoPlayer.time);
            elapsedTimeText.text = FormatTime(elapsed);
        }
    }

    /// <summary>
    /// Simulate loading progress (real progress not available for streaming)
    /// </summary>
    private IEnumerator SimulateLoadingProgress()
    {
        float progress = 0f;

        // Phase 1: Connecting (0-30%)
        while (!isReady && progress < 0.3f)
        {
            progress += Time.deltaTime * 0.15f;

            if (VRLoadingManager.Instance != null)
            {
                VRLoadingManager.Instance.UpdateStatus($"Downloading video... {Mathf.RoundToInt(progress * 100)}%");
                VRLoadingManager.Instance.UpdateProgress(progress);
            }

            yield return null;
        }

        // Phase 2: Buffering (30-70%)
        while (!isReady && progress < 0.7f)
        {
            progress += Time.deltaTime * 0.2f;

            // Try to get actual frame count for more accurate progress
            if (videoPlayer.frameCount > 0 && videoPlayer.frame > 0)
            {
                float actualProgress = (float)videoPlayer.frame / videoPlayer.frameCount;
                progress = Mathf.Max(progress, 0.3f + actualProgress * 0.4f); // Map to 30-70% range
            }

            if (VRLoadingManager.Instance != null)
            {
                VRLoadingManager.Instance.UpdateStatus($"Buffering video data... {Mathf.RoundToInt(progress * 100)}%");
                VRLoadingManager.Instance.UpdateProgress(progress);
            }

            yield return null;
        }

        // Phase 3: Preparing (70-90%)
        while (!isReady && progress < 0.9f)
        {
            progress += Time.deltaTime * 0.15f;

            if (VRLoadingManager.Instance != null)
            {
                VRLoadingManager.Instance.UpdateStatus($"Preparing playback... {Mathf.RoundToInt(progress * 100)}%");
                VRLoadingManager.Instance.UpdateProgress(progress);
            }

            yield return null;
        }
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        Debug.Log("[VRVideoController] Video prepared and ready");

        isReady = true;

        // Update total time display
        if (totalTimeText != null)
        {
            TimeSpan totalTime = TimeSpan.FromSeconds(videoPlayer.length);
            totalTimeText.text = FormatTime(totalTime);
        }

        // Complete loading progress
        if (VRLoadingManager.Instance != null)
        {
            VRLoadingManager.Instance.UpdateProgress(1f);
            VRLoadingManager.Instance.UpdateStatus("Starting video...");
        }

        // Fire ready event
        OnVideoReady?.Invoke();

        // Auto-play after brief delay
        StartCoroutine(AutoPlayAfterDelay());
    }

    private IEnumerator AutoPlayAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);

        // Hide loading screen
        if (VRLoadingManager.Instance != null)
        {
            VRLoadingManager.Instance.HideLoading();
        }

        // Start playback
        Play();
    }

    private int retryCount = 0;
    private const int maxRetries = 3;

    private void OnVideoError(VideoPlayer vp, string message)
    {
        Debug.LogError($"[VRVideoController] Video error: {message}");

        if (VRLoadingManager.Instance != null)
        {
            if (retryCount < maxRetries)
            {
                retryCount++;
                VRLoadingManager.Instance.UpdateStatus($"Connection failed. Retrying... ({retryCount}/{maxRetries})");
                VRLoadingManager.Instance.UpdateProgress(0.1f);

                Debug.Log($"[VRVideoController] Retry attempt {retryCount}/{maxRetries}");

                // Wait and retry
                StartCoroutine(RetryLoadVideo());
            }
            else
            {
                VRLoadingManager.Instance.UpdateStatus($"Error: Could not load video after {maxRetries} attempts");
                Debug.LogError($"[VRVideoController] Failed to load video after {maxRetries} retries");
            }
        }
    }

    private IEnumerator RetryLoadVideo()
    {
        yield return new WaitForSeconds(2f);

        if (VRLoadingManager.Instance != null)
        {
            VRLoadingManager.Instance.UpdateStatus("Reconnecting...");
        }

        // Try preparing again
        videoPlayer.Prepare();
    }

    private void OnVideoEnd(VideoPlayer vp)
    {
        Debug.Log("[VRVideoController] Video finished");
        isPlaying = false;
        OnVideoFinished?.Invoke();

        // Optionally reset or load next scene
        StopVideo();
    }

    // === PUBLIC CONTROL METHODS (Called by VRHUDButton events) ===

    public void Play()
    {
        if (!isReady)
        {
            Debug.LogWarning("[VRVideoController] Cannot play - video not ready");
            return;
        }

        videoPlayer.Play();
        if (audioPlayer != null)
            audioPlayer.Play();

        isPlaying = true;

        // Update icons
        if (playIcon != null) playIcon.SetActive(false);
        if (pauseIcon != null) pauseIcon.SetActive(true);

        OnVideoStarted?.Invoke();
        Debug.Log("[VRVideoController] Playing");
    }

    public void Pause()
    {
        videoPlayer.Pause();
        if (audioPlayer != null)
            audioPlayer.Pause();

        isPlaying = false;

        // Update icons
        if (playIcon != null) playIcon.SetActive(true);
        if (pauseIcon != null) pauseIcon.SetActive(false);

        Debug.Log("[VRVideoController] Paused");
    }

    public void StopVideo()
    {
        videoPlayer.Stop();
        if (audioPlayer != null)
            audioPlayer.Stop();

        isPlaying = false;

        // Update icons
        if (playIcon != null) playIcon.SetActive(true);
        if (pauseIcon != null) pauseIcon.SetActive(false);

        Debug.Log("[VRVideoController] Stopped");

        // Clear video URL and return to menu
        PlayerPrefs.DeleteKey("VideoUrl");
        // UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    public void SetPlaybackSpeed(float speed)
    {
        if (videoPlayer.canSetPlaybackSpeed)
        {
            videoPlayer.playbackSpeed = speed;
            if (audioPlayer != null)
                audioPlayer.playbackSpeed = speed;

            Debug.Log($"[VRVideoController] Playback speed: {speed}x");
        }
        else
        {
            Debug.LogWarning("[VRVideoController] Cannot set playback speed");
        }
    }

    public void FastForward()
    {
        SetPlaybackSpeed(2f);
    }

    public void VeryFastForward()
    {
        SetPlaybackSpeed(3f);
    }

    public void Rewind()
    {
        SetPlaybackSpeed(-2f);
    }

    public void NormalSpeed()
    {
        SetPlaybackSpeed(1f);
    }

    public void SkipForward(float seconds = 10f)
    {
        videoPlayer.time = Mathf.Min((float)videoPlayer.time + seconds, (float)videoPlayer.length);
        if (audioPlayer != null)
            audioPlayer.time = videoPlayer.time;

        Debug.Log($"[VRVideoController] Skipped forward {seconds}s");
    }

    public void SkipBackward(float seconds = 10f)
    {
        videoPlayer.time = Mathf.Max((float)videoPlayer.time - seconds, 0f);
        if (audioPlayer != null)
            audioPlayer.time = videoPlayer.time;

        Debug.Log($"[VRVideoController] Skipped backward {seconds}s");
    }

    public void RestartVideo()
    {
        videoPlayer.time = 0;
        if (audioPlayer != null)
            audioPlayer.time = 0;

        Play();
        Debug.Log("[VRVideoController] Restarted");
    }

    // === UTILITY ===

    private string FormatTime(TimeSpan time)
    {
        return string.Format("{0:D2}:{1:D2}:{2:D2}",
            time.Hours, time.Minutes, time.Seconds);
    }

    public bool IsReady() => isReady;
    public bool IsPlaying() => isPlaying;
    public float GetCurrentTime() => (float)videoPlayer.time;
    public float GetTotalTime() => (float)videoPlayer.length;

    void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.loopPointReached -= OnVideoEnd;
            videoPlayer.errorReceived -= OnVideoError;
            videoPlayer.frameReady -= OnFrameReady;
        }
    }
}