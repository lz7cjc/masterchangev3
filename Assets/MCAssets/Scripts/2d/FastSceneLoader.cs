using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// OPTIMIZED: Fast async scene loading with progress display
/// Should load in 2-5 seconds instead of 30+
/// </summary>
public class FastSceneLoader : MonoBehaviour
{
    [Header("Target Scene")]
    public string targetScene = "mainVR";

    [Header("Optional UI")]
    public Image progressBar;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI percentageText;

    [Header("Settings")]
    public bool showProgress = true;
    public float minimumDisplayTime = 0.5f;

    void Awake()
    {
        Debug.Log($"[FastLoader] Starting load to {targetScene} at {Time.realtimeSinceStartup:F2}s");

        if (showProgress)
        {
            StartCoroutine(LoadWithProgress());
        }
        else
        {
            StartCoroutine(LoadQuick());
        }
    }

    /// <summary>
    /// FAST async load with progress display
    /// </summary>
    IEnumerator LoadWithProgress()
    {
        float startTime = Time.realtimeSinceStartup;

        // Initialize UI
        UpdateUI(0f, "Loading...");

        // Brief moment for UI to render
        yield return null;

        Debug.Log($"[FastLoader] Starting async operation at {Time.realtimeSinceStartup:F2}s");

        // Start ASYNC load - this is key for speed!
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetScene);

        if (asyncLoad == null)
        {
            Debug.LogError($"[FastLoader] Failed to start async load!");
            yield break;
        }

        // Allow scene to activate as soon as ready (don't hold it back)
        asyncLoad.allowSceneActivation = true;

        // Track progress
        float lastProgress = 0f;

        while (!asyncLoad.isDone)
        {
            // Unity's progress goes to 0.9, then jumps to 1.0
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

            // Only update UI if progress changed significantly
            if (progress - lastProgress >= 0.05f)
            {
                UpdateUI(progress, "Loading...");
                lastProgress = progress;
                Debug.Log($"[FastLoader] Progress: {(progress * 100):F0}% at {Time.realtimeSinceStartup:F2}s");
            }

            yield return null;
        }

        float elapsed = Time.realtimeSinceStartup - startTime;
        Debug.Log($"[FastLoader] Scene loaded in {elapsed:F2} seconds");

        // Scene is loaded and activated!
        UpdateUI(1f, "Ready!");
    }

    /// <summary>
    /// FASTEST possible load - no UI updates
    /// </summary>
    IEnumerator LoadQuick()
    {
        float startTime = Time.realtimeSinceStartup;
        Debug.Log($"[FastLoader] Quick load starting...");

        // Just load, don't track progress
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetScene);
        asyncLoad.allowSceneActivation = true;

        // Wait for completion
        yield return asyncLoad;

        float elapsed = Time.realtimeSinceStartup - startTime;
        Debug.Log($"[FastLoader] Quick load completed in {elapsed:F2} seconds");
    }

    void UpdateUI(float progress, string status)
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
    }

    /// <summary>
    /// For button/event trigger compatibility
    /// </summary>
    public void LoadScene()
    {
        Debug.Log($"[FastLoader] Manual load triggered");
        StartCoroutine(LoadWithProgress());
    }

    /// <summary>
    /// For event triggers that pass scene name
    /// </summary>
    public void SetNewScene(string sceneName)
    {
        Debug.Log($"[FastLoader] SetNewScene: {sceneName}");
        targetScene = sceneName;
        StartCoroutine(LoadWithProgress());
    }
}