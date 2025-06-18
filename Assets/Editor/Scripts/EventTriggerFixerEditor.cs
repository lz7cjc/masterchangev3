using UnityEngine;
using UnityEditor;

// Editor script to add menu items for fixing EventTriggers
// PUT THIS FILE IN: Assets/Editor/Scripts/EventTriggerFixerEditor.cs
public class EventTriggerFixerEditor : Editor
{
    [MenuItem("Tools/Video System/Fix All Event Triggers in Scene")]
    public static void FixAllEventTriggersInScene()
    {
        EventTriggerFixer fixer = FindObjectOfType<EventTriggerFixer>();
        if (fixer == null)
        {
            GameObject tempGO = new GameObject("TempEventTriggerFixer");
            fixer = tempGO.AddComponent<EventTriggerFixer>();
        }

        fixer.FixAllEventTriggersInScene();

        if (fixer.gameObject.name == "TempEventTriggerFixer")
        {
            DestroyImmediate(fixer.gameObject);
        }
    }

    [MenuItem("Tools/Video System/Validate All Event Triggers")]
    public static void ValidateAllEventTriggers()
    {
        EventTriggerFixer fixer = FindObjectOfType<EventTriggerFixer>();
        if (fixer == null)
        {
            GameObject tempGO = new GameObject("TempEventTriggerFixer");
            fixer = tempGO.AddComponent<EventTriggerFixer>();
        }

        fixer.ValidateAllEventTriggers();

        if (fixer.gameObject.name == "TempEventTriggerFixer")
        {
            DestroyImmediate(fixer.gameObject);
        }
    }

    [MenuItem("Tools/Video System/Fix Selected Event Triggers")]
    public static void FixSelectedEventTriggers()
    {
        if (Selection.gameObjects.Length == 0)
        {
            Debug.LogWarning("No GameObjects selected. Select video prefabs to fix their EventTriggers.");
            return;
        }

        EventTriggerFixer fixer = FindObjectOfType<EventTriggerFixer>();
        if (fixer == null)
        {
            GameObject tempGO = new GameObject("TempEventTriggerFixer");
            fixer = tempGO.AddComponent<EventTriggerFixer>();
        }

        int fixedCount = 0;
        foreach (GameObject selected in Selection.gameObjects)
        {
            if (fixer.FixEventTriggersOnGameObject(selected))
            {
                fixedCount++;
            }
        }

        Debug.Log($"✅ Fixed EventTriggers on {fixedCount} out of {Selection.gameObjects.Length} selected objects");

        if (fixer.gameObject.name == "TempEventTriggerFixer")
        {
            DestroyImmediate(fixer.gameObject);
        }
    }

    [MenuItem("Tools/Video System/Create EventTriggerFixer GameObject")]
    public static void CreateEventTriggerFixerGameObject()
    {
        GameObject fixerGO = new GameObject("EventTriggerFixer");
        fixerGO.AddComponent<EventTriggerFixer>();
        Selection.activeGameObject = fixerGO;

        Debug.Log("✅ Created EventTriggerFixer GameObject. You can now use its context menu options.");
    }

    [MenuItem("Tools/Video System/Debug: Log All EnhancedVideoPlayer Status")]
    public static void LogAllVideoPlayerStatus()
    {
        EnhancedVideoPlayer[] allVideos = FindObjectOfType<EventTriggerFixer>()?.GetComponents<EnhancedVideoPlayer>() ?? FindObjectsOfType<EnhancedVideoPlayer>();

        Debug.Log($"🔍 Found {allVideos.Length} EnhancedVideoPlayer components in scene:");

        foreach (var video in allVideos)
        {
            Debug.Log($"📹 {video.gameObject.name}:");
            Debug.Log($"   Title: {video.title}");
            Debug.Log($"   Zone: {video.LastKnownZone}");
            Debug.Log($"   URL: {video.VideoUrlLink}");

            var eventTrigger = video.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            var boxCollider = video.GetComponent<BoxCollider>();

            Debug.Log($"   BoxCollider: {(boxCollider != null ? $"exists (isTrigger: {boxCollider.isTrigger})" : "missing")}");
            Debug.Log($"   EventTrigger: {(eventTrigger != null ? $"exists ({eventTrigger.triggers?.Count ?? 0} triggers)" : "missing")}");
        }
    }
}