using UnityEngine;
using System.Diagnostics;

/// <summary>
/// Monitors what happens when mainVR scene loads
/// Attach to a GameObject in mainVR scene (create empty GameObject called "DiagnosticsMonitor")
/// </summary>
public class MainVREntryMonitor : MonoBehaviour
{
    private static Stopwatch sceneStopwatch;

    // Called BEFORE any Start() methods
    void Awake()
    {
        if (sceneStopwatch == null)
        {
            sceneStopwatch = Stopwatch.StartNew();
        }

        Log("=== MAINVR SCENE ENTRY ===");
        Log($"Awake() called at {Time.realtimeSinceStartup:F3}s");
        Log($"Frame: {Time.frameCount}");

        // Check all MonoBehaviours in scene
        MonoBehaviour[] allScripts = FindObjectsOfType<MonoBehaviour>();
        Log($"Total MonoBehaviours in scene: {allScripts.Length}");

        // List all scripts that might have heavy Start() methods
        foreach (MonoBehaviour script in allScripts)
        {
            if (script.enabled)
            {
                Log($"  Active: {script.GetType().Name} on {script.gameObject.name}");
            }
        }
    }

    void Start()
    {
        LogTime("Start() called");

        // Check camera
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            LogError("NO MAIN CAMERA FOUND!");
        }
        else
        {
            Log($"✓ Main Camera: {mainCam.gameObject.name}");
            Log($"  Enabled: {mainCam.enabled}");
            Log($"  Clear Flags: {mainCam.clearFlags}");
            Log($"  Culling Mask: {mainCam.cullingMask}");
        }

        // Check lighting
        Light[] lights = FindObjectsOfType<Light>();
        Log($"Lights in scene: {lights.Length}");
    }

    void OnEnable()
    {
        LogTime("OnEnable() called");
    }

    // Track first few frames
    private int frameTracker = 0;
    void Update()
    {
        frameTracker++;
        if (frameTracker <= 5)
        {
            LogTime($"Update() frame {frameTracker}");
        }
        else if (frameTracker == 6)
        {
            LogTime("First 5 frames complete - monitoring stopped");
            Log("=== MAINVR INITIALIZATION COMPLETE ===");
        }
    }

    void LogTime(string message)
    {
        float elapsed = sceneStopwatch.ElapsedMilliseconds / 1000f;
        Log($"[{elapsed:F3}s] {message}");
    }

    void Log(string message)
    {
        UnityEngine.Debug.Log($"[MAINVR] {message}");
    }

    void LogError(string message)
    {
        UnityEngine.Debug.LogError($"[MAINVR] {message}");
    }
}