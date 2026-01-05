using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Diagnostics;

/// <summary>
/// Comprehensive scene loading diagnostics
/// Place on dashboard scene to track mainVR loading performance
/// </summary>
public class SceneLoadDiagnostics : MonoBehaviour
{
    private Stopwatch stopwatch;
    private float unityTimeStart;

    public void LoadMainVRWithDiagnostics()
    {
        StartCoroutine(DiagnosticLoad());
    }

    IEnumerator DiagnosticLoad()
    {
        stopwatch = Stopwatch.StartNew();
        unityTimeStart = Time.realtimeSinceStartup;

        Log("=== SCENE LOAD DIAGNOSTICS START ===");
        Log($"Current Scene: {SceneManager.GetActiveScene().name}");
        Log($"Target Scene: mainVR");
        Log($"Unity Time: {Time.realtimeSinceStartup}s");
        Log($"Frame Count: {Time.frameCount}");

        // Check memory before load
        Log($"Memory Before: {System.GC.GetTotalMemory(false) / 1048576f:F2} MB");

        // Phase 1: Pre-load
        Log("--- Phase 1: Pre-load checks ---");
        yield return LogPhase("Checking scene exists in build...");

        bool sceneExists = false;
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            Log($"Build Scene {i}: {sceneName}");
            if (sceneName == "mainVR") sceneExists = true;
        }

        if (!sceneExists)
        {
            LogError("mainVR scene NOT found in build settings!");
            yield break;
        }

        Log("✓ mainVR found in build");

        // Phase 2: Start async load
        Log("--- Phase 2: Starting async load ---");
        LogPhaseTime("Before LoadSceneAsync call");

        AsyncOperation asyncLoad = null;
        try
        {
            asyncLoad = SceneManager.LoadSceneAsync("mainVR");
        }
        catch (System.Exception e)
        {
            LogError($"LoadSceneAsync failed: {e.Message}");
            yield break;
        }

        LogPhaseTime("After LoadSceneAsync call");

        if (asyncLoad == null)
        {
            LogError("AsyncOperation is null!");
            yield break;
        }

        Log($"✓ AsyncOperation created");
        Log($"Initial progress: {asyncLoad.progress}");
        Log($"Is done: {asyncLoad.isDone}");
        Log($"Allow scene activation: {asyncLoad.allowSceneActivation}");

        // Phase 3: Track loading progress frame-by-frame
        Log("--- Phase 3: Tracking load progress ---");

        int frameCount = 0;
        float lastProgress = -1f;

        while (!asyncLoad.isDone)
        {
            frameCount++;

            // Log every change in progress
            if (asyncLoad.progress != lastProgress)
            {
                LogPhaseTime($"Progress changed: {asyncLoad.progress:F3} (frame {frameCount})");
                lastProgress = asyncLoad.progress;
            }

            // Log every 30 frames (roughly every 0.5 seconds at 60fps)
            if (frameCount % 30 == 0)
            {
                LogPhaseTime($"Still loading... Progress: {asyncLoad.progress:F3}, Frame: {frameCount}");
            }

            // Safety timeout
            if (stopwatch.ElapsedMilliseconds > 60000) // 60 seconds
            {
                LogError($"TIMEOUT! Loading took over 60 seconds. Last progress: {asyncLoad.progress}");
                break;
            }

            yield return null;
        }

        // Phase 4: Load complete
        Log("--- Phase 4: Load complete ---");
        LogPhaseTime("Scene activated");
        Log($"Total frames during load: {frameCount}");
        Log($"Memory After: {System.GC.GetTotalMemory(false) / 1048576f:F2} MB");
        Log($"New Scene: {SceneManager.GetActiveScene().name}");

        Log("=== SCENE LOAD DIAGNOSTICS END ===");
        Log($"TOTAL TIME: {stopwatch.ElapsedMilliseconds}ms ({stopwatch.ElapsedMilliseconds / 1000f:F2}s)");
        Log($"UNITY TIME: {Time.realtimeSinceStartup - unityTimeStart:F2}s");
    }

    IEnumerator LogPhase(string message)
    {
        LogPhaseTime(message);
        yield return null;
    }

    void LogPhaseTime(string message)
    {
        float elapsed = stopwatch.ElapsedMilliseconds / 1000f;
        float unityElapsed = Time.realtimeSinceStartup - unityTimeStart;
        Log($"[{elapsed:F3}s / Unity:{unityElapsed:F3}s] {message}");
    }

    void Log(string message)
    {
        UnityEngine.Debug.Log($"[DIAGNOSTIC] {message}");
    }

    void LogError(string message)
    {
        UnityEngine.Debug.LogError($"[DIAGNOSTIC] {message}");
    }
}