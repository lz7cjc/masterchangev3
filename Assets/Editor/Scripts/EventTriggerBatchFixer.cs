using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Standalone utility to batch-fix EventTriggers on all EnhancedVideoPlayer instances
/// This resolves the issue where imported prefabs have empty EventTrigger lists
/// </summary>
public class EventTriggerBatchFixer : EditorWindow
{
    private bool fixEmptyTriggers = true;
    private bool revertToPrefab = true;
    private bool applyPrefabOverrides = false;
    private Vector2 scrollPosition;

    private List<GameObject> videosToFix = new List<GameObject>();
    private int totalVideos = 0;
    private int videosNeedingFix = 0;
    private int videosAlreadyFixed = 0;

    [MenuItem("Tools/Film Zone Video Manager/EventTrigger Batch Fixer")]
    public static void ShowWindow()
    {
        EventTriggerBatchFixer window = GetWindow<EventTriggerBatchFixer>("EventTrigger Batch Fixer");
        window.minSize = new Vector2(500, 600);
        window.ScanScene();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("EventTrigger Batch Fixer", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        EditorGUILayout.HelpBox(
            "This tool fixes EventTriggers on all EnhancedVideoPlayer instances in the scene.\n\n" +
            "Common issue: When importing prefabs, EventTrigger lists are empty even though the prefab has them configured.",
            MessageType.Info
        );

        EditorGUILayout.Space(10);

        // Scan results
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Scene Analysis", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Total Video Objects: {totalVideos}");
        EditorGUILayout.LabelField($"Need Fixing: {videosNeedingFix}", videosNeedingFix > 0 ? EditorStyles.boldLabel : EditorStyles.label);
        EditorGUILayout.LabelField($"Already Fixed: {videosAlreadyFixed}");
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // Fix options
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Fix Options", EditorStyles.boldLabel);

        fixEmptyTriggers = EditorGUILayout.Toggle("Fix Empty EventTriggers", fixEmptyTriggers);
        EditorGUILayout.HelpBox(
            "Adds Pointer Enter, Pointer Exit, and Pointer Click events to empty EventTriggers",
            MessageType.None
        );

        EditorGUILayout.Space(5);

        revertToPrefab = EditorGUILayout.Toggle("Revert to Prefab First", revertToPrefab);
        EditorGUILayout.HelpBox(
            "For prefab instances, tries to revert EventTrigger to prefab values first. " +
            "Only creates new triggers if prefab also has empty triggers.",
            MessageType.None
        );

        EditorGUILayout.Space(5);

        applyPrefabOverrides = EditorGUILayout.Toggle("Apply to Prefabs", applyPrefabOverrides);
        EditorGUILayout.HelpBox(
            "If checked, applies changes back to the prefab asset. " +
            "Use this if your prefabs themselves need fixing.",
            MessageType.Warning
        );

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // Action buttons
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Rescan Scene", GUILayout.Height(30)))
        {
            ScanScene();
        }

        GUI.enabled = videosNeedingFix > 0;
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button($"Fix All ({videosNeedingFix})", GUILayout.Height(30)))
        {
            FixAllEventTriggers();
        }
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // List of videos needing fix
        if (videosNeedingFix > 0)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Videos Needing Fix ({videosNeedingFix})", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));

            foreach (var video in videosToFix)
            {
                if (video == null) continue;

                EditorGUILayout.BeginHorizontal("box");

                EditorGUILayout.ObjectField(video, typeof(GameObject), true);

                EnhancedVideoPlayer player = video.GetComponent<EnhancedVideoPlayer>();
                if (player != null)
                {
                    EditorGUILayout.LabelField($"{player.title} ({player.LastKnownZone})", GUILayout.Width(200));
                }

                if (GUILayout.Button("Fix This", GUILayout.Width(80)))
                {
                    FixSingleVideo(video);
                    ScanScene(); // Rescan after fix
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
    }

    private void ScanScene()
    {
        videosToFix.Clear();
        totalVideos = 0;
        videosNeedingFix = 0;
        videosAlreadyFixed = 0;

        EnhancedVideoPlayer[] allVideos = FindObjectsOfType<EnhancedVideoPlayer>();
        totalVideos = allVideos.Length;

        foreach (var video in allVideos)
        {
            if (video == null || video.gameObject == null) continue;

            EventTrigger trigger = video.GetComponent<EventTrigger>();

            bool needsFix = false;
            if (trigger == null)
            {
                needsFix = true;
            }
            else if (trigger.triggers == null || trigger.triggers.Count == 0)
            {
                needsFix = true;
            }
            else
            {
                // Check if any of the required events are missing
                bool hasPointerEnter = trigger.triggers.Any(t => t.eventID == EventTriggerType.PointerEnter);
                bool hasPointerExit = trigger.triggers.Any(t => t.eventID == EventTriggerType.PointerExit);
                bool hasPointerClick = trigger.triggers.Any(t => t.eventID == EventTriggerType.PointerClick);

                needsFix = !hasPointerEnter || !hasPointerExit || !hasPointerClick;
            }

            if (needsFix)
            {
                videosNeedingFix++;
                videosToFix.Add(video.gameObject);
            }
            else
            {
                videosAlreadyFixed++;
            }
        }

        Debug.Log($"📊 Scene scan complete: {totalVideos} total, {videosNeedingFix} need fixing, {videosAlreadyFixed} already fixed");
        Repaint();
    }

    private void FixAllEventTriggers()
    {
        if (videosToFix.Count == 0)
        {
            EditorUtility.DisplayDialog("No Videos to Fix", "All videos already have EventTriggers configured!", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog(
            "Confirm Batch Fix",
            $"This will fix EventTriggers on {videosToFix.Count} video objects.\n\n" +
            "This operation can be undone with Edit > Undo.\n\nContinue?",
            "Yes, Fix Them",
            "Cancel"))
        {
            return;
        }

        int fixedCount = 0;
        int errorCount = 0;

        EditorUtility.DisplayProgressBar("Fixing EventTriggers", "Processing videos...", 0f);

        for (int i = 0; i < videosToFix.Count; i++)
        {
            GameObject video = videosToFix[i];
            if (video == null) continue;

            float progress = (float)i / videosToFix.Count;
            EditorUtility.DisplayProgressBar("Fixing EventTriggers", $"Processing {video.name}...", progress);

            try
            {
                if (FixSingleVideo(video))
                {
                    fixedCount++;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error fixing {video.name}: {ex.Message}");
                errorCount++;
            }
        }

        EditorUtility.ClearProgressBar();

        // Rescan to update the list
        ScanScene();

        EditorUtility.DisplayDialog(
            "Batch Fix Complete",
            $"Successfully fixed {fixedCount} video objects!\n\n" +
            $"Errors: {errorCount}\n" +
            $"Remaining issues: {videosNeedingFix}",
            "OK"
        );

        Debug.Log($"✅ Batch fix complete! Fixed: {fixedCount}, Errors: {errorCount}, Remaining: {videosNeedingFix}");
    }

    private bool FixSingleVideo(GameObject videoObject)
    {
        if (videoObject == null) return false;

        EnhancedVideoPlayer videoPlayer = videoObject.GetComponent<EnhancedVideoPlayer>();
        if (videoPlayer == null)
        {
            Debug.LogWarning($"No EnhancedVideoPlayer found on {videoObject.name}");
            return false;
        }

        // Step 1: Try to revert to prefab if it's a prefab instance
        if (revertToPrefab)
        {
            GameObject prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(videoObject);
            if (prefabAsset != null)
            {
                EventTrigger instanceTrigger = videoObject.GetComponent<EventTrigger>();
                EventTrigger prefabTrigger = prefabAsset.GetComponent<EventTrigger>();

                // Check if prefab has valid triggers
                if (prefabTrigger != null && prefabTrigger.triggers != null && prefabTrigger.triggers.Count > 0)
                {
                    if (instanceTrigger != null)
                    {
                        // Revert to prefab
                        PrefabUtility.RevertObjectOverride(instanceTrigger, InteractionMode.AutomatedAction);
                        Debug.Log($"✅ Reverted EventTrigger to prefab on: {videoObject.name}");
                        EditorUtility.SetDirty(videoObject);
                        return true;
                    }
                }
            }
        }

        // Step 2: If revert didn't work or wasn't applicable, create new triggers
        if (fixEmptyTriggers)
        {
            EventTrigger eventTrigger = videoObject.GetComponent<EventTrigger>();
            if (eventTrigger == null)
            {
                eventTrigger = videoObject.AddComponent<EventTrigger>();
            }

            if (eventTrigger.triggers == null)
            {
                eventTrigger.triggers = new List<EventTrigger.Entry>();
            }
            else
            {
                eventTrigger.triggers.Clear();
            }

            // Add Pointer Enter
            EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
            pointerEnter.eventID = EventTriggerType.PointerEnter;
            UnityEngine.Events.UnityAction<BaseEventData> enterAction = (data) => {
                videoPlayer.SendMessage("MouseHoverChangeScene", SendMessageOptions.DontRequireReceiver);
            };
            pointerEnter.callback.AddListener(enterAction);
            eventTrigger.triggers.Add(pointerEnter);

            // Add Pointer Exit
            EventTrigger.Entry pointerExit = new EventTrigger.Entry();
            pointerExit.eventID = EventTriggerType.PointerExit;
            UnityEngine.Events.UnityAction<BaseEventData> exitAction = (data) => {
                videoPlayer.SendMessage("MouseExit", SendMessageOptions.DontRequireReceiver);
            };
            pointerExit.callback.AddListener(exitAction);
            eventTrigger.triggers.Add(pointerExit);

            // Add Pointer Click
            EventTrigger.Entry pointerClick = new EventTrigger.Entry();
            pointerClick.eventID = EventTriggerType.PointerClick;
            UnityEngine.Events.UnityAction<BaseEventData> clickAction = (data) => {
                videoPlayer.SendMessage("MouseHoverChangeScene", SendMessageOptions.DontRequireReceiver);
            };
            pointerClick.callback.AddListener(clickAction);
            eventTrigger.triggers.Add(pointerClick);

            EditorUtility.SetDirty(eventTrigger);
            EditorUtility.SetDirty(videoObject);

            Debug.Log($"✅ Created new EventTriggers on: {videoObject.name}");

            // Step 3: Apply to prefab if requested
            if (applyPrefabOverrides && PrefabUtility.IsPartOfPrefabInstance(videoObject))
            {
                try
                {
                    PrefabUtility.ApplyObjectOverride(eventTrigger, PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(videoObject), InteractionMode.AutomatedAction);
                    Debug.Log($"✅ Applied EventTrigger changes to prefab for: {videoObject.name}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Could not apply to prefab for {videoObject.name}: {ex.Message}");
                }
            }

            return true;
        }

        return false;
    }

    [MenuItem("Tools/Film Zone Video Manager/Quick Fix All Empty EventTriggers")]
    public static void QuickFixAll()
    {
        EnhancedVideoPlayer[] allVideos = FindObjectsOfType<EnhancedVideoPlayer>();
        int fixedCount = 0;

        foreach (var video in allVideos)
        {
            if (video == null || video.gameObject == null) continue;

            EventTrigger trigger = video.GetComponent<EventTrigger>();
            bool needsFix = (trigger == null || trigger.triggers == null || trigger.triggers.Count == 0);

            if (needsFix)
            {
                if (trigger == null)
                {
                    trigger = video.gameObject.AddComponent<EventTrigger>();
                }

                if (trigger.triggers == null)
                {
                    trigger.triggers = new List<EventTrigger.Entry>();
                }
                else
                {
                    trigger.triggers.Clear();
                }

                // Add all three events
                AddEvent(trigger, EventTriggerType.PointerEnter, video, "MouseHoverChangeScene");
                AddEvent(trigger, EventTriggerType.PointerExit, video, "MouseExit");
                AddEvent(trigger, EventTriggerType.PointerClick, video, "MouseHoverChangeScene");

                EditorUtility.SetDirty(trigger);
                EditorUtility.SetDirty(video.gameObject);
                fixedCount++;
            }
        }

        Debug.Log($"✅ Quick fix complete! Fixed {fixedCount} out of {allVideos.Length} videos");

        EditorUtility.DisplayDialog(
            "Quick Fix Complete",
            $"Fixed {fixedCount} videos with empty EventTriggers out of {allVideos.Length} total videos.",
            "OK"
        );
    }

    private static void AddEvent(EventTrigger trigger, EventTriggerType type, EnhancedVideoPlayer player, string methodName)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = type;
        UnityEngine.Events.UnityAction<BaseEventData> action = (data) => {
            player.SendMessage(methodName, SendMessageOptions.DontRequireReceiver);
        };
        entry.callback.AddListener(action);
        trigger.triggers.Add(entry);
    }
}