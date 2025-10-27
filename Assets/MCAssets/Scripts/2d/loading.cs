using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// OPTIMIZED: Loading screen for "everything" → "mainVR" scene transition
/// - Unified debug keyword: [VRLOAD]
/// - Better progress tracking
/// - Faster loading
/// - Smoother transitions
/// </summary>
public class loading : MonoBehaviour
{
    [Header("Progress UI")]
    [SerializeField] private Image progressBar;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI percentageText;

    [Header("Settings")]
    [SerializeField] private float minimumLoadTime = 1.5f;
    [SerializeField] private string targetScene = "mainVR";

    private float loadStartTime;

    void Start()
    {
        Debug.Log("[VRLOAD] ========================================");
        Debug.Log("[VRLOAD] EVERYTHING SCENE - LOADING START");
        Debug.Log("[VRLOAD] Target scene: " + targetScene);
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
    /// OPTIMIZED: Load mainVR scene with progress tracking
    /// </summary>
    IEnumerator LoadMainVRScene()
    {
        Debug.Log("[VRLOAD] === SCENE LOAD SEQUENCE START ===");

        // Phase 1: Start loading (0-10%)
        UpdateProgress(0.05f, "Initializing...");
        yield return new WaitForSeconds(0.2f);

        // Phase 2: Begin async load (10-30%)
        UpdateProgress(0.1f, "Loading VR environment...");

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetScene);
        if (asyncLoad == null)
        {
            Debug.LogError($"[VRLOAD] Failed to start loading scene: {targetScene}");
            yield break;
        }

        // Prevent scene from activating until we're ready
        asyncLoad.allowSceneActivation = false;

        Debug.Log("[VRLOAD] Async load started");

        // Phase 3: Track loading progress (30-90%)
        while (asyncLoad.progress < 0.9f)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            float displayProgress = 0.3f + (progress * 0.6f); // Map to 30-90%

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
    /// Update progress bar and status text
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
}