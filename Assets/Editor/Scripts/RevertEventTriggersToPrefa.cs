using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;

/// <summary>
/// The REAL solution: Your prefabs already have correct EventTriggers!
/// We just need to revert all instances to use the prefab configuration.
/// </summary>
public class RevertEventTriggersToPrefa
{
    [MenuItem("Tools/Video System/REVERT All EventTriggers to Prefab")]
    public static void RevertAllEventTriggersToPrefab()
    {
        EnhancedVideoPlayer[] allVideos = Object.FindObjectsOfType<EnhancedVideoPlayer>();

        if (!EditorUtility.DisplayDialog(
            "Revert EventTriggers to Prefab",
            $"This will revert EventTrigger components on {allVideos.Length} prefab instances.\n\n" +
            "This will restore the EventTrigger configuration from the prefab.\n\n" +
            "Continue?",
            "Yes, Revert All",
            "Cancel"))
        {
            return;
        }

        int revertedCount = 0;
        int notPrefabCount = 0;
        int noEventTriggerCount = 0;

        foreach (var video in allVideos)
        {
            if (video == null || video.gameObject == null) continue;

            // Check if this is a prefab instance
            if (!PrefabUtility.IsPartOfPrefabInstance(video.gameObject))
            {
                notPrefabCount++;
                continue;
            }

            // Get EventTrigger component
            EventTrigger eventTrigger = video.GetComponent<EventTrigger>();
            if (eventTrigger == null)
            {
                noEventTriggerCount++;
                continue;
            }

            try
            {
                // Revert the EventTrigger component to prefab values
                PrefabUtility.RevertObjectOverride(eventTrigger, InteractionMode.UserAction);

                EditorUtility.SetDirty(video.gameObject);
                revertedCount++;

                Debug.Log($"✅ Reverted EventTrigger on: {video.gameObject.name}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to revert {video.gameObject.name}: {ex.Message}");
            }
        }

        // Save changes
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog(
            "Revert Complete!",
            $"Reverted {revertedCount} EventTriggers to prefab configuration!\n\n" +
            $"Not prefab instances: {notPrefabCount}\n" +
            $"No EventTrigger component: {noEventTriggerCount}\n\n" +
            "Check any video object - EventTrigger should now show the functions!",
            "OK"
        );

        Debug.Log($"✅ REVERT COMPLETE! Reverted: {revertedCount}, Not prefabs: {notPrefabCount}, No EventTrigger: {noEventTriggerCount}");
    }

    [MenuItem("Tools/Video System/REVERT Selected EventTrigger to Prefab")]
    public static void RevertSelectedEventTriggerToPrefab()
    {
        if (Selection.activeGameObject == null)
        {
            EditorUtility.DisplayDialog("No Selection", "Select a video GameObject first", "OK");
            return;
        }

        // Check if prefab instance
        if (!PrefabUtility.IsPartOfPrefabInstance(Selection.activeGameObject))
        {
            EditorUtility.DisplayDialog("Not a Prefab Instance",
                "Selected object is not a prefab instance.\n\nThis tool only works on prefab instances.",
                "OK");
            return;
        }

        // Get EventTrigger
        EventTrigger eventTrigger = Selection.activeGameObject.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            EditorUtility.DisplayDialog("No EventTrigger",
                "Selected object doesn't have an EventTrigger component.",
                "OK");
            return;
        }

        // Show what we found
        Debug.Log($"Before revert:");
        Debug.Log($"  EventTrigger.triggers count: {eventTrigger.triggers?.Count ?? 0}");
        if (eventTrigger.triggers != null)
        {
            foreach (var trigger in eventTrigger.triggers)
            {
                int callbackCount = trigger.callback?.GetPersistentEventCount() ?? 0;
                Debug.Log($"  {trigger.eventID}: {callbackCount} callbacks");
            }
        }

        // Revert to prefab
        try
        {
            PrefabUtility.RevertObjectOverride(eventTrigger, InteractionMode.UserAction);
            EditorUtility.SetDirty(Selection.activeGameObject);
            AssetDatabase.SaveAssets();

            Debug.Log($"After revert:");
            Debug.Log($"  EventTrigger.triggers count: {eventTrigger.triggers?.Count ?? 0}");
            if (eventTrigger.triggers != null)
            {
                foreach (var trigger in eventTrigger.triggers)
                {
                    int callbackCount = trigger.callback?.GetPersistentEventCount() ?? 0;
                    Debug.Log($"  {trigger.eventID}: {callbackCount} callbacks");
                }
            }

            // Force Inspector refresh
            var temp = Selection.activeGameObject;
            Selection.activeGameObject = null;
            EditorApplication.delayCall += () => {
                Selection.activeGameObject = temp;
            };

            EditorUtility.DisplayDialog("Success!",
                $"Reverted EventTrigger on {Selection.activeGameObject.name}\n\n" +
                "Check Inspector - should show the functions now!",
                "OK");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to revert: {ex.Message}");
            EditorUtility.DisplayDialog("Error", $"Failed to revert:\n{ex.Message}", "OK");
        }
    }

    [MenuItem("Tools/Video System/Check Prefab EventTrigger Status")]
    public static void CheckPrefabEventTriggerStatus()
    {
        if (Selection.activeGameObject == null)
        {
            EditorUtility.DisplayDialog("No Selection", "Select a video GameObject first", "OK");
            return;
        }

        GameObject prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(Selection.activeGameObject);

        if (prefabAsset == null)
        {
            EditorUtility.DisplayDialog("Not a Prefab Instance",
                "Selected object is not a prefab instance.",
                "OK");
            return;
        }

        EventTrigger instanceTrigger = Selection.activeGameObject.GetComponent<EventTrigger>();
        EventTrigger prefabTrigger = prefabAsset.GetComponent<EventTrigger>();

        string report = "=== EventTrigger Status Report ===\n\n";

        report += $"Instance: {Selection.activeGameObject.name}\n";
        report += $"  Has EventTrigger: {instanceTrigger != null}\n";
        if (instanceTrigger != null)
        {
            report += $"  Triggers count: {instanceTrigger.triggers?.Count ?? 0}\n";
            if (instanceTrigger.triggers != null)
            {
                foreach (var trigger in instanceTrigger.triggers)
                {
                    int count = trigger.callback?.GetPersistentEventCount() ?? 0;
                    report += $"    {trigger.eventID}: {count} callbacks\n";
                }
            }
        }

        report += $"\nPrefab: {prefabAsset.name}\n";
        report += $"  Has EventTrigger: {prefabTrigger != null}\n";
        if (prefabTrigger != null)
        {
            report += $"  Triggers count: {prefabTrigger.triggers?.Count ?? 0}\n";
            if (prefabTrigger.triggers != null)
            {
                foreach (var trigger in prefabTrigger.triggers)
                {
                    int count = trigger.callback?.GetPersistentEventCount() ?? 0;
                    report += $"    {trigger.eventID}: {count} callbacks\n";

                    if (count > 0)
                    {
                        var target = trigger.callback.GetPersistentTarget(0);
                        var method = trigger.callback.GetPersistentMethodName(0);
                        report += $"      -> {target?.GetType().Name ?? "null"}.{method}\n";
                    }
                }
            }
        }

        Debug.Log(report);
        EditorUtility.DisplayDialog("Status Report", report, "OK");
    }

    [MenuItem("Tools/Video System/Apply Instance EventTrigger to Prefab")]
    public static void ApplyInstanceEventTriggerToPrefab()
    {
        if (Selection.activeGameObject == null)
        {
            EditorUtility.DisplayDialog("No Selection", "Select a video GameObject first", "OK");
            return;
        }

        if (!PrefabUtility.IsPartOfPrefabInstance(Selection.activeGameObject))
        {
            EditorUtility.DisplayDialog("Not a Prefab Instance",
                "Selected object is not a prefab instance.",
                "OK");
            return;
        }

        EventTrigger eventTrigger = Selection.activeGameObject.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            EditorUtility.DisplayDialog("No EventTrigger",
                "Selected object doesn't have an EventTrigger component.",
                "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog(
            "Apply to Prefab?",
            $"This will apply the EventTrigger configuration from this instance to the prefab.\n\n" +
            $"This will affect ALL instances of this prefab.\n\n" +
            "Continue?",
            "Yes, Apply to Prefab",
            "Cancel"))
        {
            return;
        }

        try
        {
            string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(Selection.activeGameObject);
            PrefabUtility.ApplyObjectOverride(eventTrigger, prefabPath, InteractionMode.UserAction);

            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("Success!",
                "Applied EventTrigger to prefab!\n\n" +
                "All instances of this prefab will now have this EventTrigger configuration.",
                "OK");

            Debug.Log($"✅ Applied EventTrigger to prefab: {prefabPath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to apply to prefab: {ex.Message}");
            EditorUtility.DisplayDialog("Error", $"Failed:\n{ex.Message}", "OK");
        }
    }
}