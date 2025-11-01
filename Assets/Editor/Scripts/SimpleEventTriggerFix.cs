using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;
using UnityEditor.Events;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// VERSION 3: Uses UnityEventTools.AddPersistentListener - Unity's official API
/// This is the PROPER way to add persistent listeners that save correctly
/// </summary>
public class SimpleEventTriggerFix_v3
{
    [MenuItem("Tools/Video System/FIX v3 - Use UnityEventTools (PROPER)")]
    public static void FixAllWithUnityEventTools()
    {
        EnhancedVideoPlayer[] allVideos = Object.FindObjectsOfType<EnhancedVideoPlayer>();
        int fixedCount = 0;
        int skippedCount = 0;

        if (!EditorUtility.DisplayDialog(
            "Fix EventTriggers (Version 3 - PROPER)",
            $"This will add persistent EventTrigger callbacks to {allVideos.Length} video objects.\n\n" +
            "Uses UnityEventTools - Unity's official API for persistent listeners!\n\n" +
            "Continue?",
            "Yes, Fix Them",
            "Cancel"))
        {
            return;
        }

        foreach (var video in allVideos)
        {
            if (video == null || video.gameObject == null) continue;

            try
            {
                if (FixEventTriggerWithUnityEventTools(video.gameObject, video))
                {
                    fixedCount++;
                }
                else
                {
                    skippedCount++;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error fixing {video.gameObject.name}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // Force save
        AssetDatabase.SaveAssets();
        EditorApplication.RepaintHierarchyWindow();

        EditorUtility.DisplayDialog(
            "Fix Complete!",
            $"Successfully fixed {fixedCount} videos!\n\n" +
            $"Skipped: {skippedCount}\n\n" +
            "Select a video and check Inspector!",
            "OK"
        );

        Debug.Log($"✅ FIX v3 COMPLETE! Fixed: {fixedCount}, Skipped: {skippedCount}");
    }

    private static bool FixEventTriggerWithUnityEventTools(GameObject gameObject, EnhancedVideoPlayer videoPlayer)
    {
        // Get or add EventTrigger
        EventTrigger eventTrigger = gameObject.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = Undo.AddComponent<EventTrigger>(gameObject);
        }
        else
        {
            Undo.RecordObject(eventTrigger, "Fix EventTrigger");
        }

        // Initialize triggers
        if (eventTrigger.triggers == null)
        {
            eventTrigger.triggers = new List<EventTrigger.Entry>();
        }

        // Check if already configured properly
        if (HasValidCallbacks(eventTrigger, videoPlayer))
        {
            return false;
        }

        // Clear existing
        eventTrigger.triggers.Clear();

        // Get the methods we need to call
        MethodInfo mouseHoverMethod = typeof(EnhancedVideoPlayer).GetMethod("MouseHoverChangeScene",
            BindingFlags.Public | BindingFlags.Instance);
        MethodInfo mouseExitMethod = typeof(EnhancedVideoPlayer).GetMethod("MouseExit",
            BindingFlags.Public | BindingFlags.Instance);

        if (mouseHoverMethod == null || mouseExitMethod == null)
        {
            Debug.LogError($"Could not find methods on EnhancedVideoPlayer! Hover: {mouseHoverMethod != null}, Exit: {mouseExitMethod != null}");
            return false;
        }

        // Create entries
        EventTrigger.Entry pointerEnterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        EventTrigger.Entry pointerExitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        EventTrigger.Entry pointerClickEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };

        // Initialize callbacks
        pointerEnterEntry.callback = new EventTrigger.TriggerEvent();
        pointerExitEntry.callback = new EventTrigger.TriggerEvent();
        pointerClickEntry.callback = new EventTrigger.TriggerEvent();

        // Add entries to triggers list
        eventTrigger.triggers.Add(pointerEnterEntry);
        eventTrigger.triggers.Add(pointerExitEntry);
        eventTrigger.triggers.Add(pointerClickEntry);

        // NOW use UnityEventTools to add persistent listeners
        // This is Unity's official API and WILL save correctly!

        try
        {
            // Pointer Enter -> MouseHoverChangeScene
            UnityEventTools.AddPersistentListener(pointerEnterEntry.callback,
                new UnityEngine.Events.UnityAction<BaseEventData>((data) => videoPlayer.MouseHoverChangeScene()));

            // Pointer Exit -> MouseExit
            UnityEventTools.AddPersistentListener(pointerExitEntry.callback,
                new UnityEngine.Events.UnityAction<BaseEventData>((data) => videoPlayer.MouseExit()));

            // Pointer Click -> MouseHoverChangeScene
            UnityEventTools.AddPersistentListener(pointerClickEntry.callback,
                new UnityEngine.Events.UnityAction<BaseEventData>((data) => videoPlayer.MouseHoverChangeScene()));

            Debug.Log($"✅ Added UnityEventTools listeners to: {gameObject.name}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to add UnityEventTools listeners: {ex.Message}");
            return false;
        }

        // Mark dirty
        EditorUtility.SetDirty(eventTrigger);
        EditorUtility.SetDirty(gameObject);

        // For prefab instances
        if (PrefabUtility.IsPartOfPrefabInstance(gameObject))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(eventTrigger);
        }

        return true;
    }

    private static bool HasValidCallbacks(EventTrigger trigger, EnhancedVideoPlayer expectedTarget)
    {
        if (trigger == null || trigger.triggers == null || trigger.triggers.Count < 3)
            return false;

        foreach (var entry in trigger.triggers)
        {
            if (entry.callback == null || entry.callback.GetPersistentEventCount() == 0)
                return false;
        }

        return true;
    }

    [MenuItem("Tools/Video System/Test Single v3 (UnityEventTools)")]
    public static void TestSingleWithUnityEventTools()
    {
        if (Selection.activeGameObject == null)
        {
            EditorUtility.DisplayDialog("No Selection", "Select a video GameObject first", "OK");
            return;
        }

        EnhancedVideoPlayer video = Selection.activeGameObject.GetComponent<EnhancedVideoPlayer>();
        if (video == null)
        {
            EditorUtility.DisplayDialog("Invalid Selection", "Selected object needs EnhancedVideoPlayer component", "OK");
            return;
        }

        EventTrigger trigger = Selection.activeGameObject.GetComponent<EventTrigger>();
        if (trigger != null)
        {
            Debug.Log($"Before: EventTrigger has {trigger.triggers?.Count ?? 0} triggers");
        }

        if (FixEventTriggerWithUnityEventTools(Selection.activeGameObject, video))
        {
            trigger = Selection.activeGameObject.GetComponent<EventTrigger>();
            Debug.Log($"After: EventTrigger has {trigger.triggers?.Count ?? 0} triggers");

            if (trigger != null && trigger.triggers != null)
            {
                foreach (var entry in trigger.triggers)
                {
                    int count = entry.callback?.GetPersistentEventCount() ?? 0;
                    Debug.Log($"  {entry.eventID}: {count} persistent callbacks");
                }
            }

            // Force Inspector refresh
            EditorUtility.SetDirty(Selection.activeGameObject);
            EditorApplication.RepaintHierarchyWindow();
            Selection.activeGameObject = null;
            System.Threading.Thread.Sleep(100);
            Selection.activeGameObject = trigger.gameObject;

            EditorUtility.DisplayDialog("Success!",
                $"Fixed {Selection.activeGameObject.name}\n\n" +
                "Check Inspector now!",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Already Fixed",
                "EventTrigger already has valid callbacks.",
                "OK");
        }
    }
}