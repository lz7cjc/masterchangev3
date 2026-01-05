using UnityEngine;
using System.Diagnostics;
using System.Collections.Generic;

/// <summary>
/// Profiles EVERY script's Awake/Start execution time
/// Attach to same GameObject as MainVREntryMonitor
/// </summary>
[DefaultExecutionOrder(-1000)] // Run before everything else
public class MonoBehaviourProfiler : MonoBehaviour
{
    private static Dictionary<string, float> awakeTimings = new Dictionary<string, float>();
    private static Dictionary<string, float> startTimings = new Dictionary<string, float>();
    private static Stopwatch stopwatch = Stopwatch.StartNew();

    void Awake()
    {
        Log("Profiler initialized");
        StartCoroutine(ProfileAllScripts());
    }

    System.Collections.IEnumerator ProfileAllScripts()
    {
        yield return new WaitForEndOfFrame();

        // After first frame, report all timings
        Log("=== SCRIPT EXECUTION PROFILING ===");

        if (awakeTimings.Count > 0)
        {
            Log("--- Awake() Timings ---");
            foreach (var kvp in awakeTimings)
            {
                Log($"{kvp.Key}: {kvp.Value:F3}s");
            }
        }

        yield return null;

        if (startTimings.Count > 0)
        {
            Log("--- Start() Timings ---");
            foreach (var kvp in startTimings)
            {
                Log($"{kvp.Key}: {kvp.Value:F3}s");
            }
        }

        Log("=== PROFILING COMPLETE ===");
    }

    public static void RecordAwake(string scriptName)
    {
        float time = stopwatch.ElapsedMilliseconds / 1000f;
        awakeTimings[scriptName] = time;
    }

    public static void RecordStart(string scriptName)
    {
        float time = stopwatch.ElapsedMilliseconds / 1000f;
        startTimings[scriptName] = time;
    }

    void Log(string message)
    {
        UnityEngine.Debug.Log($"[PROFILER] {message}");
    }
}