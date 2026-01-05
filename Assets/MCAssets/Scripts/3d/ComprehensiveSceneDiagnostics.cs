using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;

[DefaultExecutionOrder(-1000)]
public class ComprehensiveSceneDiagnostics : MonoBehaviour
{
    private static Stopwatch stopwatch;
    private static Dictionary<string, float> timings = new Dictionary<string, float>();

    void Awake()
    {
        if (stopwatch == null)
        {
            stopwatch = Stopwatch.StartNew();
        }

        LogTiming("=== SCENE LOAD START ===");
        LogTiming($"Scene: {SceneManager.GetActiveScene().name}");
        LogTiming($"Frame: {Time.frameCount}");
        LogTiming($"Unity Time: {Time.realtimeSinceStartup}s");

        StartCoroutine(DiagnoseEverything());
    }

    IEnumerator DiagnoseEverything()
    {
        // Phase 1: Check DontDestroyOnLoad objects
        LogTiming("--- CHECKING DONTDESTROYONLOAD ---");
        yield return CheckDontDestroyOnLoad();

        // Phase 2: Check all active GameObjects
        LogTiming("--- CHECKING ACTIVE GAMEOBJECTS ---");
        yield return CheckAllGameObjects();

        // Phase 3: Check EventSystems
        LogTiming("--- CHECKING EVENTSYSTEMS ---");
        yield return CheckEventSystems();

        // Phase 4: Check Cameras
        LogTiming("--- CHECKING CAMERAS ---");
        yield return CheckCameras();

        // Phase 5: Check MonoBehaviours with heavy Start()
        LogTiming("--- CHECKING MONOBEHAVIOURS ---");
        yield return CheckMonoBehaviours();

        // Phase 6: Frame-by-frame tracking
        LogTiming("--- TRACKING FIRST 10 FRAMES ---");
        yield return TrackFrames();

        LogTiming("=== DIAGNOSTICS COMPLETE ===");
    }

    IEnumerator CheckDontDestroyOnLoad()
    {
        GameObject temp = new GameObject("TempForDDOL");
        DontDestroyOnLoad(temp);
        Scene ddolScene = temp.scene;
        Destroy(temp);

        GameObject[] rootObjects = ddolScene.GetRootGameObjects();
        LogTiming($"DontDestroyOnLoad objects: {rootObjects.Length}");

        foreach (GameObject obj in rootObjects)
        {
            LogTiming($"  DDOL: {obj.name}");

            // Check for loading managers
            VRLoadingManager vrLoader = obj.GetComponentInChildren<VRLoadingManager>();
            if (vrLoader != null)
            {
                LogTiming($"    ⚠️ VRLoadingManager found! Panel active: {vrLoader.gameObject.activeSelf}");
            }

            // Check for canvas/UI blocking view
            Canvas canvas = obj.GetComponentInChildren<Canvas>(true);
            if (canvas != null)
            {
                LogTiming($"    ⚠️ Canvas found! Active: {canvas.gameObject.activeSelf}, Enabled: {canvas.enabled}");
            }
        }

        yield return null;
    }

    IEnumerator CheckAllGameObjects()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        LogTiming($"Total GameObjects in scene: {allObjects.Length}");

        int activeCount = 0;
        int disabledCount = 0;

        foreach (GameObject obj in allObjects)
        {
            if (obj.activeInHierarchy) activeCount++;
            else disabledCount++;
        }

        LogTiming($"  Active: {activeCount}, Disabled: {disabledCount}");
        yield return null;
    }

    IEnumerator CheckEventSystems()
    {
        UnityEngine.EventSystems.EventSystem[] eventSystems = FindObjectsOfType<UnityEngine.EventSystems.EventSystem>();
        LogTiming($"EventSystems found: {eventSystems.Length}");

        if (eventSystems.Length > 1)
        {
            LogTiming("  ⚠️ MULTIPLE EVENT SYSTEMS DETECTED!");
            foreach (var es in eventSystems)
            {
                LogTiming($"    EventSystem on: {es.gameObject.name}, Scene: {es.gameObject.scene.name}");
            }
        }

        yield return null;
    }

    IEnumerator CheckCameras()
    {
        Camera[] cameras = FindObjectsOfType<Camera>();
        LogTiming($"Cameras found: {cameras.Length}");

        foreach (Camera cam in cameras)
        {
            LogTiming($"  Camera: {cam.gameObject.name}");
            LogTiming($"    Enabled: {cam.enabled}, Depth: {cam.depth}, ClearFlags: {cam.clearFlags}");
            LogTiming($"    Rendering: {cam.isActiveAndEnabled}, Scene: {cam.gameObject.scene.name}");
        }

        yield return null;
    }

    IEnumerator CheckMonoBehaviours()
    {
        MonoBehaviour[] allScripts = FindObjectsOfType<MonoBehaviour>();
        LogTiming($"Total MonoBehaviours: {allScripts.Length}");

        // Check for specific problematic types
        int videoPlayers = 0;
        int loadingManagers = 0;
        int disabledScripts = 0;

        foreach (MonoBehaviour script in allScripts)
        {
            if (!script.enabled) disabledScripts++;

            if (script.GetType().Name.Contains("Video")) videoPlayers++;
            if (script.GetType().Name.Contains("Loading")) loadingManagers++;
        }

        LogTiming($"  Video-related: {videoPlayers}");
        LogTiming($"  Loading-related: {loadingManagers}");
        LogTiming($"  Disabled scripts: {disabledScripts}");

        yield return null;
    }

    IEnumerator TrackFrames()
    {
        for (int i = 1; i <= 10; i++)
        {
            LogTiming($"Frame {i} START");
            yield return null;
            LogTiming($"Frame {i} END (delta: {Time.deltaTime * 1000f:F1}ms)");
        }
    }

    void LogTiming(string message)
    {
        float elapsed = stopwatch.ElapsedMilliseconds / 1000f;
        UnityEngine.Debug.Log($"[DIAG {elapsed:F3}s] {message}");
    }
}