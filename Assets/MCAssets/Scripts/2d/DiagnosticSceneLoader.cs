using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// DIAGNOSTIC: Profile scene loading to find bottlenecks
/// Shows detailed timing and progress during load
/// </summary>
public class DiagnosticSceneLoader : MonoBehaviour
{
    [Header("Target Scene")]
    [SerializeField] private string targetScene = "mainVR";

    [Header("UI References (Optional)")]
    [SerializeField] private TextMeshProUGUI debugText;
    [SerializeField] private Image progressBar;
    [SerializeField] private TextMeshProUGUI statusText;

    private float startTime;
    private string debugLog = "";

    void Awake()
    {
        startTime = Time.realtimeSinceStartup;
        Log($"=== SCENE LOAD DIAGNOSTICS START ===");
        Log($"Target: {targetScene}");
        Log($"Time: {Time.realtimeSinceStartup:F2}s");

        StartCoroutine(DiagnosticLoad());
    }

    IEnumerator DiagnosticLoad()
    {
        // Checkpoint 1: Before load starts
        float checkpoint = Time.realtimeSinceStartup;
        Log($"\n[0.0s] Starting async load...");
        UpdateUI(0f, "Starting load...");

        // Start async load
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetScene);
        if (asyncLoad == null)
        {
            Log($"ERROR: Failed to start async load!");
            yield break;
        }

        Log($"[{ElapsedTime():F2}s] Async operation created");

        // Don't activate yet - we want to measure
        asyncLoad.allowSceneActivation = false;

        // Track load progress
        float lastProgress = 0f;
        float lastLogTime = Time.realtimeSinceStartup;

        while (!asyncLoad.isDone)
        {
            float currentProgress = asyncLoad.progress;

            // Log every 10% progress or every 2 seconds
            if (currentProgress - lastProgress >= 0.1f ||
                Time.realtimeSinceStartup - lastLogTime >= 2f)
            {
                Log($"[{ElapsedTime():F2}s] Progress: {(currentProgress * 100):F0}%");
                lastProgress = currentProgress;
                lastLogTime = Time.realtimeSinceStartup;
            }

            UpdateUI(currentProgress, $"Loading... {(currentProgress * 100):F0}%");

            // Check if reached 90% (Unity's "done loading" point)
            if (asyncLoad.progress >= 0.9f)
            {
                Log($"[{ElapsedTime():F2}s] Scene loaded to 90%");
                Log($"Scene is ready but not activated yet");
                Log($"Total load time: {ElapsedTime():F2}s");

                UpdateUI(0.9f, "Scene loaded, activating...");

                // Brief pause to show final progress
                yield return new WaitForSeconds(0.5f);

                // Activate scene
                Log($"[{ElapsedTime():F2}s] Activating scene...");
                asyncLoad.allowSceneActivation = true;

                break;
            }

            yield return null;
        }

        // Wait for scene to fully activate
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        Log($"[{ElapsedTime():F2}s] Scene activated!");
        Log($"\n=== SCENE LOAD COMPLETE ===");
        Log($"TOTAL TIME: {ElapsedTime():F2} seconds");

        // Print full log to console
        Debug.Log(debugLog);
    }

    float ElapsedTime()
    {
        return Time.realtimeSinceStartup - startTime;
    }

    void Log(string message)
    {
        debugLog += message + "\n";

        if (debugText != null)
        {
            debugText.text = debugLog;
        }
    }

    void UpdateUI(float progress, string status)
    {
        if (progressBar != null)
        {
            progressBar.fillAmount = progress;
        }

        if (statusText != null)
        {
            statusText.text = $"{status}\nElapsed: {ElapsedTime():F1}s";
        }
    }
}