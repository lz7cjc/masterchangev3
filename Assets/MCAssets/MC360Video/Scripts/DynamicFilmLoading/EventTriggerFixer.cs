using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// FIXED EventTriggerFixer with improved position persistence and consistent property handling
/// Key fixes:
/// - Consistent use of VideoUrlLink property
/// - Better validation and error handling
/// - Improved zone assignment logic
/// - Enhanced debugging and logging
/// - Removed all VideoZonePrefab references (deprecated component)
/// </summary>
public class EventTriggerFixer : MonoBehaviour
{
    [Header("Fix Options")]
    public bool fixOnStart = false;
    public bool debugMode = true;

    [Header("What to Fix")]
    public bool ensureBoxColliderExists = true;
    public bool ensureBoxColliderIsTrigger = true;
    public bool ensureEventTriggerExists = true;
    public bool validateEventTriggerTargets = true;
    public bool addMissingEventTriggers = true;

    [Header("FIXED: Position Preservation")]
    public bool autoSaveAfterFix = true;
    public bool preserveTransformDuringFix = true;

    private FilmZoneManager zoneManager;

    private void Awake()
    {
        zoneManager = FindObjectOfType<FilmZoneManager>();
        if (zoneManager == null && debugMode)
        {
            Debug.LogWarning("No FilmZoneManager found in scene for EventTriggerFixer");
        }
    }

    private void Start()
    {
        if (fixOnStart)
        {
            FixEventTriggersOnThisObject();
        }
    }

    [ContextMenu("Fix Event Triggers on This Object")]
    public void FixEventTriggersOnThisObject()
    {
        FixEventTriggersOnGameObject(gameObject);
    }

    [ContextMenu("Fix All Event Triggers in Scene")]
    public void FixAllEventTriggersInScene()
    {
        EnhancedVideoPlayer[] allVideos = FindObjectsOfType<EnhancedVideoPlayer>();
        int fixedCount = 0;
        int errorCount = 0;

        // Store original positions if preserving transforms
        Dictionary<GameObject, TransformSnapshot> originalTransforms = new Dictionary<GameObject, TransformSnapshot>();

        if (preserveTransformDuringFix)
        {
            foreach (var video in allVideos)
            {
                if (video != null && video.gameObject != null)
                {
                    originalTransforms[video.gameObject] = new TransformSnapshot(video.transform);
                }
            }
        }

        foreach (var video in allVideos)
        {
            if (video == null || video.gameObject == null)
            {
                errorCount++;
                continue;
            }

            try
            {
                if (FixEventTriggersOnGameObject(video.gameObject))
                {
                    fixedCount++;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to fix EventTriggers on {video.gameObject.name}: {ex.Message}");
                errorCount++;
            }
        }

        // Restore original positions if needed
        if (preserveTransformDuringFix)
        {
            foreach (var kvp in originalTransforms)
            {
                if (kvp.Key != null && kvp.Value != null)
                {
                    kvp.Value.RestoreToTransform(kvp.Key.transform);
                }
            }
        }

        // Auto-save positions after fixing
        if (autoSaveAfterFix && zoneManager != null)
        {
            try
            {
                zoneManager.SaveCurrentPositions();
                if (debugMode)
                {
                    Debug.Log("💾 Auto-saved positions after EventTrigger fixes");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to auto-save positions: {ex.Message}");
            }
        }

        Debug.Log($"✅ Fixed EventTriggers on {fixedCount} out of {allVideos.Length} video objects (Errors: {errorCount})");
    }

    public bool FixEventTriggersOnGameObject(GameObject target)
    {
        if (target == null)
        {
            if (debugMode) Debug.LogWarning("Cannot fix EventTriggers: target is null");
            return false;
        }

        bool madeChanges = false;
        EnhancedVideoPlayer videoPlayer = target.GetComponent<EnhancedVideoPlayer>();

        if (videoPlayer == null)
        {
            if (debugMode) Debug.LogWarning($"No EnhancedVideoPlayer found on {target.name}");
            return false;
        }

        if (debugMode) Debug.Log($"🔧 Fixing EventTriggers on: {target.name}");

        // Store original transform
        TransformSnapshot originalTransform = null;
        if (preserveTransformDuringFix)
        {
            originalTransform = new TransformSnapshot(target.transform);
        }

        try
        {
            // 1. Ensure BoxCollider exists and is configured properly
            if (ensureBoxColliderExists || ensureBoxColliderIsTrigger)
            {
                BoxCollider boxCollider = target.GetComponent<BoxCollider>();

                if (boxCollider == null && ensureBoxColliderExists)
                {
                    boxCollider = target.AddComponent<BoxCollider>();
                    madeChanges = true;
                    if (debugMode) Debug.Log($"  ✅ Added BoxCollider to {target.name}");
                }

                if (boxCollider != null && ensureBoxColliderIsTrigger && !boxCollider.isTrigger)
                {
                    boxCollider.isTrigger = true;
                    madeChanges = true;
                    if (debugMode) Debug.Log($"  ✅ Set BoxCollider.isTrigger = true on {target.name}");
                }
            }

            // 2. Ensure EventTrigger exists
            EventTrigger eventTrigger = target.GetComponent<EventTrigger>();

            if (eventTrigger == null && ensureEventTriggerExists)
            {
                eventTrigger = target.AddComponent<EventTrigger>();
                madeChanges = true;
                if (debugMode) Debug.Log($"  ✅ Added EventTrigger to {target.name}");
            }

            if (eventTrigger == null)
            {
                // Restore transform before returning
                if (preserveTransformDuringFix && originalTransform != null)
                {
                    originalTransform.RestoreToTransform(target.transform);
                }
                return madeChanges;
            }

            // 3. Initialize triggers list if null
            if (eventTrigger.triggers == null)
            {
                eventTrigger.triggers = new List<EventTrigger.Entry>();
                madeChanges = true;
                if (debugMode) Debug.Log($"  ✅ Initialized EventTrigger.triggers list on {target.name}");
            }

            // 4. Validate and fix EventTrigger targets
            if (validateEventTriggerTargets)
            {
                bool fixedTargets = ValidateAndFixEventTriggerTargets(eventTrigger, videoPlayer);
                if (fixedTargets)
                {
                    madeChanges = true;
                }
            }

            // 5. Add missing EventTriggers if needed
            if (addMissingEventTriggers)
            {
                bool addedTriggers = AddMissingEventTriggers(eventTrigger, videoPlayer);
                if (addedTriggers)
                {
                    madeChanges = true;
                }
            }

            // 6. FIXED: Validate zone assignment
            if (ValidateAndFixZoneAssignment(videoPlayer))
            {
                madeChanges = true;
            }

            // 7. Final validation
            if (debugMode)
            {
                LogEventTriggerStatus(target, eventTrigger);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error fixing EventTriggers on {target.name}: {ex.Message}");
        }
        finally
        {
            // Always restore original transform if preserving
            if (preserveTransformDuringFix && originalTransform != null)
            {
                originalTransform.RestoreToTransform(target.transform);
            }
        }

        // Auto-save position if changes were made and auto-save is enabled
        if (madeChanges && autoSaveAfterFix && zoneManager != null)
        {
            // Delay save to next frame to ensure all changes are applied
            StartCoroutine(DelayedSavePosition(videoPlayer));
        }

#if UNITY_EDITOR
        if (madeChanges)
        {
            UnityEditor.EditorUtility.SetDirty(target);
        }
#endif

        return madeChanges;
    }

    // FIXED: Enhanced zone assignment validation
    private bool ValidateAndFixZoneAssignment(EnhancedVideoPlayer videoPlayer)
    {
        if (videoPlayer == null) return false;

        bool madeChanges = false;

        // Check if zone assignment is missing or invalid
        if (string.IsNullOrEmpty(videoPlayer.LastKnownZone) || videoPlayer.LastKnownZone == "Home")
        {
            // Try to get zone from zoneName property
            if (!string.IsNullOrEmpty(videoPlayer.zoneName) && videoPlayer.zoneName != "Home")
            {
                videoPlayer.LastKnownZone = videoPlayer.zoneName;
                madeChanges = true;
                if (debugMode)
                {
                    Debug.Log($"  ✅ Fixed LastKnownZone for {videoPlayer.gameObject.name}: set to {videoPlayer.zoneName}");
                }
            }
            else if (zoneManager != null)
            {
                // Try to determine zone from position
                string detectedZone = DetectZoneFromPosition(videoPlayer.transform.position);
                if (!string.IsNullOrEmpty(detectedZone))
                {
                    videoPlayer.LastKnownZone = detectedZone;
                    videoPlayer.zoneName = detectedZone;
                    madeChanges = true;
                    if (debugMode)
                    {
                        Debug.Log($"  ✅ Auto-detected zone for {videoPlayer.gameObject.name}: {detectedZone}");
                    }
                }
            }
        }
        // Ensure zoneName and LastKnownZone are synchronized
        else if (videoPlayer.zoneName != videoPlayer.LastKnownZone)
        {
            videoPlayer.zoneName = videoPlayer.LastKnownZone;
            madeChanges = true;
            if (debugMode)
            {
                Debug.Log($"  ✅ Synchronized zoneName with LastKnownZone for {videoPlayer.gameObject.name}");
            }
        }

        return madeChanges;
    }

    // Helper method to detect zone from position
    private string DetectZoneFromPosition(Vector3 position)
    {
        if (zoneManager?.zones == null) return null;

        foreach (var zone in zoneManager.zones)
        {
            if (zone != null && zone.IsPointInZone(position))
            {
                return zone.zoneName;
            }
        }

        return null;
    }

    private System.Collections.IEnumerator DelayedSavePosition(EnhancedVideoPlayer videoPlayer)
    {
        yield return null; // Wait one frame

        if (videoPlayer != null && !string.IsNullOrEmpty(videoPlayer.VideoUrlLink) &&
            !string.IsNullOrEmpty(videoPlayer.LastKnownZone) && zoneManager != null)
        {
            try
            {
                zoneManager.SaveCurrentPositions();
                if (debugMode)
                {
                    Debug.Log($"💾 Auto-saved position for {videoPlayer.title} after EventTrigger fix");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to save position for {videoPlayer.title}: {ex.Message}");
            }
        }
    }

    private bool ValidateAndFixEventTriggerTargets(EventTrigger eventTrigger, EnhancedVideoPlayer videoPlayer)
    {
        bool madeChanges = false;

        if (eventTrigger?.triggers == null) return false;

        foreach (var trigger in eventTrigger.triggers)
        {
            if (trigger?.callback == null) continue;

            for (int i = 0; i < trigger.callback.GetPersistentEventCount(); i++)
            {
                var target = trigger.callback.GetPersistentTarget(i);
                var methodName = trigger.callback.GetPersistentMethodName(i);

                // If target is null or wrong type, log warning
                if (target == null || !(target is EnhancedVideoPlayer))
                {
                    if (debugMode)
                    {
                        Debug.LogWarning($"  ⚠️ EventTrigger on {videoPlayer.gameObject.name} has invalid target for {trigger.eventID} -> {methodName}");
                        Debug.LogWarning($"    Target: {target}, Expected: EnhancedVideoPlayer");
                    }
                    // Note: Fixing persistent event targets programmatically is complex and requires SerializedObject manipulation
                }
                else if (target == videoPlayer)
                {
                    // Validate method names
                    bool isValidMethod = IsValidEventMethod(trigger.eventID, methodName);

                    if (!isValidMethod && debugMode)
                    {
                        Debug.LogWarning($"  ⚠️ Unexpected method '{methodName}' for {trigger.eventID} on {videoPlayer.gameObject.name}");
                    }
                    else if (isValidMethod && debugMode)
                    {
                        Debug.Log($"  ✅ Valid EventTrigger: {trigger.eventID} -> {methodName} on {videoPlayer.gameObject.name}");
                    }
                }
            }
        }

        return madeChanges;
    }

    private bool IsValidEventMethod(EventTriggerType eventType, string methodName)
    {
        switch (eventType)
        {
            case EventTriggerType.PointerEnter:
                return methodName == "MouseHoverChangeScene" || methodName == "OnPointerEnter";
            case EventTriggerType.PointerExit:
                return methodName == "MouseExit" || methodName == "OnPointerExit";
            case EventTriggerType.PointerClick:
                return methodName == "SetVideoUrl" || methodName == "MouseHoverChangeScene";
            default:
                return false;
        }
    }

    private bool AddMissingEventTriggers(EventTrigger eventTrigger, EnhancedVideoPlayer videoPlayer)
    {
        if (eventTrigger?.triggers == null || videoPlayer == null) return false;

        bool madeChanges = false;

        // Check what triggers exist
        bool hasPointerEnter = eventTrigger.triggers.Any(t => t.eventID == EventTriggerType.PointerEnter);
        bool hasPointerExit = eventTrigger.triggers.Any(t => t.eventID == EventTriggerType.PointerExit);
        bool hasPointerClick = eventTrigger.triggers.Any(t => t.eventID == EventTriggerType.PointerClick);

        // Add missing PointerEnter
        if (!hasPointerEnter)
        {
            EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
            pointerEnter.eventID = EventTriggerType.PointerEnter;
            pointerEnter.callback.AddListener((data) => {
                if (videoPlayer != null) videoPlayer.MouseHoverChangeScene();
            });
            eventTrigger.triggers.Add(pointerEnter);
            madeChanges = true;
            if (debugMode) Debug.Log($"  ✅ Added PointerEnter trigger to {videoPlayer.gameObject.name}");
        }

        // Add missing PointerExit
        if (!hasPointerExit)
        {
            EventTrigger.Entry pointerExit = new EventTrigger.Entry();
            pointerExit.eventID = EventTriggerType.PointerExit;
            pointerExit.callback.AddListener((data) => {
                if (videoPlayer != null) videoPlayer.MouseExit();
            });
            eventTrigger.triggers.Add(pointerExit);
            madeChanges = true;
            if (debugMode) Debug.Log($"  ✅ Added PointerExit trigger to {videoPlayer.gameObject.name}");
        }

        // Add missing PointerClick (optional)
        if (!hasPointerClick)
        {
            EventTrigger.Entry pointerClick = new EventTrigger.Entry();
            pointerClick.eventID = EventTriggerType.PointerClick;
            pointerClick.callback.AddListener((data) => {
                if (videoPlayer == null) return;

                // FIXED: Save zone before triggering using consistent property names
                if (!string.IsNullOrEmpty(videoPlayer.LastKnownZone))
                {
                    PlayerPrefs.SetString("lastknownzone", videoPlayer.LastKnownZone);
                    PlayerPrefs.Save();
                }
                videoPlayer.SetVideoUrl();
            });
            eventTrigger.triggers.Add(pointerClick);
            madeChanges = true;
            if (debugMode) Debug.Log($"  ✅ Added PointerClick trigger to {videoPlayer.gameObject.name}");
        }

        return madeChanges;
    }

    private void LogEventTriggerStatus(GameObject target, EventTrigger eventTrigger)
    {
        if (!debugMode || eventTrigger == null) return;

        Debug.Log($"🔍 EventTrigger Status for {target.name}:");
        Debug.Log($"  - Triggers count: {eventTrigger.triggers?.Count ?? 0}");

        if (eventTrigger.triggers != null)
        {
            foreach (var trigger in eventTrigger.triggers)
            {
                if (trigger == null) continue;

                string eventName = trigger.eventID.ToString();
                int callbackCount = trigger.callback?.GetPersistentEventCount() ?? 0;
                Debug.Log($"  - {eventName}: {callbackCount} persistent callbacks");

                // Log details of persistent callbacks
                if (trigger.callback != null && callbackCount > 0)
                {
                    for (int i = 0; i < callbackCount; i++)
                    {
                        var callTarget = trigger.callback.GetPersistentTarget(i);
                        var methodName = trigger.callback.GetPersistentMethodName(i);
                        string targetName = callTarget != null ? callTarget.GetType().Name : "null";
                        Debug.Log($"    [{i}] {targetName}.{methodName}");
                    }
                }
            }
        }
    }

    [ContextMenu("Validate All Event Triggers")]
    public void ValidateAllEventTriggers()
    {
        EnhancedVideoPlayer[] allVideos = FindObjectsOfType<EnhancedVideoPlayer>();
        int validCount = 0;
        int invalidCount = 0;
        int totalIssues = 0;

        Debug.Log("🔍 Validating all EventTriggers in scene...");

        foreach (var video in allVideos)
        {
            if (video == null || video.gameObject == null)
            {
                invalidCount++;
                continue;
            }

            var issues = ValidateEventTriggerSetup(video.gameObject);
            if (issues.Count == 0)
            {
                validCount++;
            }
            else
            {
                invalidCount++;
                totalIssues += issues.Count;
            }
        }

        Debug.Log($"📊 Validation Results: {validCount} valid, {invalidCount} invalid out of {allVideos.Length} total");
        if (totalIssues > 0)
        {
            Debug.Log($"📊 Total issues found: {totalIssues}");
        }
    }

    private List<string> ValidateEventTriggerSetup(GameObject target)
    {
        List<string> issues = new List<string>();

        if (target == null)
        {
            issues.Add("Target is null");
            return issues;
        }

        EnhancedVideoPlayer videoPlayer = target.GetComponent<EnhancedVideoPlayer>();
        if (videoPlayer == null)
        {
            issues.Add("Missing EnhancedVideoPlayer component");
            return issues;
        }

        BoxCollider boxCollider = target.GetComponent<BoxCollider>();
        EventTrigger eventTrigger = target.GetComponent<EventTrigger>();

        // Check BoxCollider
        if (boxCollider == null)
        {
            issues.Add("Missing BoxCollider");
        }
        else if (!boxCollider.isTrigger)
        {
            issues.Add("BoxCollider.isTrigger = false");
        }

        // Check EventTrigger
        if (eventTrigger == null)
        {
            issues.Add("Missing EventTrigger");
        }
        else
        {
            if (eventTrigger.triggers == null || eventTrigger.triggers.Count == 0)
            {
                issues.Add("No EventTrigger entries");
            }
            else
            {
                bool hasPointerEnter = eventTrigger.triggers.Any(t => t.eventID == EventTriggerType.PointerEnter);
                bool hasPointerExit = eventTrigger.triggers.Any(t => t.eventID == EventTriggerType.PointerExit);

                if (!hasPointerEnter) issues.Add("Missing PointerEnter");
                if (!hasPointerExit) issues.Add("Missing PointerExit");
            }
        }

        // FIXED: Check zone assignment with consistent property names
        if (string.IsNullOrEmpty(videoPlayer.LastKnownZone) || videoPlayer.LastKnownZone == "Home")
        {
            issues.Add("LastKnownZone not set properly");
        }

        // FIXED: Check video URL assignment
        if (string.IsNullOrEmpty(videoPlayer.VideoUrlLink))
        {
            issues.Add("VideoUrlLink not set");
        }

        // Check zone/URL synchronization
        if (!string.IsNullOrEmpty(videoPlayer.zoneName) && videoPlayer.zoneName != videoPlayer.LastKnownZone)
        {
            issues.Add("zoneName and LastKnownZone mismatch");
        }

        if (issues.Count > 0)
        {
            Debug.LogWarning($"❌ {target.name}: {string.Join(", ", issues)}");
        }
        else if (debugMode)
        {
            Debug.Log($"✅ {target.name}: Valid setup");
        }

        return issues;
    }

    [ContextMenu("Fix and Save All Positions")]
    public void FixAndSaveAllPositions()
    {
        Debug.Log("🔧 Fixing all EventTriggers and saving positions...");

        // First fix all event triggers
        FixAllEventTriggersInScene();

        // Then save all current positions
        if (zoneManager != null)
        {
            try
            {
                zoneManager.SaveCurrentPositions();
                Debug.Log("💾 Saved all positions after fixing EventTriggers");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to save positions: {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ No FilmZoneManager found - positions not saved");
        }
    }

    [ContextMenu("Batch Fix Videos in Zone")]
    public void BatchFixVideosInZone()
    {
        // This could be enhanced to fix only videos in a specific zone
        // For now, it's equivalent to fixing all
        FixAllEventTriggersInScene();
    }

    // FIXED: Enhanced zone synchronization method
    [ContextMenu("Synchronize All Zone Assignments")]
    public void SynchronizeAllZoneAssignments()
    {
        EnhancedVideoPlayer[] allVideos = FindObjectsOfType<EnhancedVideoPlayer>();
        int syncedCount = 0;
        int errorCount = 0;

        Debug.Log("🔄 Synchronizing zone assignments for all videos...");

        foreach (var video in allVideos)
        {
            if (video == null)
            {
                errorCount++;
                continue;
            }

            try
            {
                if (ValidateAndFixZoneAssignment(video))
                {
                    syncedCount++;
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(video);
#endif
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to sync zone for {video.gameObject.name}: {ex.Message}");
                errorCount++;
            }
        }

        if (syncedCount > 0 && autoSaveAfterFix && zoneManager != null)
        {
            try
            {
                zoneManager.SaveCurrentPositions();
                Debug.Log("💾 Saved positions after zone synchronization");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to save positions after sync: {ex.Message}");
            }
        }

        Debug.Log($"✅ Synchronized {syncedCount} zone assignments (Errors: {errorCount})");
    }

    // FIXED: Enhanced cleanup method with proper validation
    [ContextMenu("Clean Up All Video Components")]
    public void CleanUpAllVideoComponents()
    {
        if (!UnityEngine.Application.isEditor)
        {
            Debug.LogWarning("Component cleanup only available in editor mode");
            return;
        }

        EnhancedVideoPlayer[] allVideos = FindObjectsOfType<EnhancedVideoPlayer>();
        int cleanedCount = 0;
        int errorCount = 0;

        Debug.Log("🧹 Cleaning up video components...");

        foreach (var video in allVideos)
        {
            if (video == null || video.gameObject == null)
            {
                errorCount++;
                continue;
            }

            try
            {
                // Ensure proper component setup
                FixEventTriggersOnGameObject(video.gameObject);
                cleanedCount++;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to clean up {video.gameObject.name}: {ex.Message}");
                errorCount++;
            }
        }

        Debug.Log($"✅ Cleaned up {cleanedCount} video components (Errors: {errorCount})");
    }

    // ===== MENU ITEMS =====
#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/Event Trigger Fixer/Fix All Event Triggers")]
    public static void MenuFixAllEventTriggers()
    {
        EventTriggerFixer fixer = FindObjectOfType<EventTriggerFixer>();
        if (fixer != null)
        {
            fixer.FixAllEventTriggersInScene();
        }
        else
        {
            Debug.LogError("No EventTriggerFixer found in scene!");
        }
    }

    [UnityEditor.MenuItem("Tools/Event Trigger Fixer/Validate All Event Triggers")]
    public static void MenuValidateAllEventTriggers()
    {
        EventTriggerFixer fixer = FindObjectOfType<EventTriggerFixer>();
        if (fixer != null)
        {
            fixer.ValidateAllEventTriggers();
        }
        else
        {
            Debug.LogError("No EventTriggerFixer found in scene!");
        }
    }

    [UnityEditor.MenuItem("Tools/Event Trigger Fixer/Fix and Save All Positions")]
    public static void MenuFixAndSaveAllPositions()
    {
        EventTriggerFixer fixer = FindObjectOfType<EventTriggerFixer>();
        if (fixer != null)
        {
            fixer.FixAndSaveAllPositions();
        }
        else
        {
            Debug.LogError("No EventTriggerFixer found in scene!");
        }
    }

    [UnityEditor.MenuItem("Tools/Event Trigger Fixer/Synchronize Zone Assignments")]
    public static void MenuSynchronizeZoneAssignments()
    {
        EventTriggerFixer fixer = FindObjectOfType<EventTriggerFixer>();
        if (fixer != null)
        {
            fixer.SynchronizeAllZoneAssignments();
        }
        else
        {
            Debug.LogError("No EventTriggerFixer found in scene!");
        }
    }

    [UnityEditor.MenuItem("Tools/Event Trigger Fixer/Clean Up Video Components")]
    public static void MenuCleanUpVideoComponents()
    {
        EventTriggerFixer fixer = FindObjectOfType<EventTriggerFixer>();
        if (fixer != null)
        {
            fixer.CleanUpAllVideoComponents();
        }
        else
        {
            Debug.LogError("No EventTriggerFixer found in scene!");
        }
    }
#endif
}

/// <summary>
/// FIXED: Enhanced TransformSnapshot with better validation
/// </summary>
[System.Serializable]
public class TransformSnapshot
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 localScale;
    public bool isValid;

    public TransformSnapshot(Transform transform)
    {
        if (transform != null)
        {
            position = transform.position;
            rotation = transform.rotation;
            localScale = transform.localScale;
            isValid = true;
        }
        else
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            localScale = Vector3.one;
            isValid = false;
        }
    }

    public void RestoreToTransform(Transform transform)
    {
        if (transform == null || !isValid) return;

        try
        {
            transform.position = position;
            transform.rotation = rotation;
            transform.localScale = localScale;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to restore transform: {ex.Message}");
        }
    }
}