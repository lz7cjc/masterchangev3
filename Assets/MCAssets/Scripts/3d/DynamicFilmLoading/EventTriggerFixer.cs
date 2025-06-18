using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

// Utility script to fix EventTrigger issues on video prefabs
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

        foreach (var video in allVideos)
        {
            if (FixEventTriggersOnGameObject(video.gameObject))
            {
                fixedCount++;
            }
        }

        Debug.Log($"✅ Fixed EventTriggers on {fixedCount} out of {allVideos.Length} video objects");
    }

    public bool FixEventTriggersOnGameObject(GameObject target)
    {
        if (target == null) return false;

        bool madeChanges = false;
        EnhancedVideoPlayer videoPlayer = target.GetComponent<EnhancedVideoPlayer>();

        if (videoPlayer == null)
        {
            if (debugMode) Debug.LogWarning($"No EnhancedVideoPlayer found on {target.name}");
            return false;
        }

        if (debugMode) Debug.Log($"🔧 Fixing EventTriggers on: {target.name}");

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

        if (eventTrigger == null) return madeChanges;

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

        // 6. Final validation
        LogEventTriggerStatus(target, eventTrigger);

#if UNITY_EDITOR
        if (madeChanges)
        {
            UnityEditor.EditorUtility.SetDirty(target);
        }
#endif

        return madeChanges;
    }

    private bool ValidateAndFixEventTriggerTargets(EventTrigger eventTrigger, EnhancedVideoPlayer videoPlayer)
    {
        bool madeChanges = false;

        foreach (var trigger in eventTrigger.triggers)
        {
            if (trigger.callback == null) continue;

            for (int i = 0; i < trigger.callback.GetPersistentEventCount(); i++)
            {
                var target = trigger.callback.GetPersistentTarget(i);
                var methodName = trigger.callback.GetPersistentMethodName(i);

                // If target is null or wrong type, we might need to fix it
                if (target == null || !(target is EnhancedVideoPlayer))
                {
                    if (debugMode)
                    {
                        Debug.LogWarning($"  ⚠️ EventTrigger on {videoPlayer.gameObject.name} has invalid target for {trigger.eventID} -> {methodName}");
                        Debug.LogWarning($"    Target: {target}, Expected: EnhancedVideoPlayer");
                    }

                    // Note: Fixing persistent event targets programmatically is complex
                    // This would require SerializedObject manipulation
                    // For now, we'll just log the issue
                }
                else if (target == videoPlayer)
                {
                    // Validate method names
                    bool isValidMethod = false;

                    if (trigger.eventID == EventTriggerType.PointerEnter)
                    {
                        isValidMethod = (methodName == "MouseHoverChangeScene" || methodName == "OnPointerEnter");
                    }
                    else if (trigger.eventID == EventTriggerType.PointerExit)
                    {
                        isValidMethod = (methodName == "MouseExit" || methodName == "OnPointerExit");
                    }
                    else if (trigger.eventID == EventTriggerType.PointerClick)
                    {
                        isValidMethod = (methodName == "SetVideoUrl" || methodName == "MouseHoverChangeScene");
                    }

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

    private bool AddMissingEventTriggers(EventTrigger eventTrigger, EnhancedVideoPlayer videoPlayer)
    {
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
            pointerEnter.callback.AddListener((data) => { videoPlayer.MouseHoverChangeScene(); });
            eventTrigger.triggers.Add(pointerEnter);
            madeChanges = true;
            if (debugMode) Debug.Log($"  ✅ Added PointerEnter trigger to {videoPlayer.gameObject.name}");
        }

        // Add missing PointerExit
        if (!hasPointerExit)
        {
            EventTrigger.Entry pointerExit = new EventTrigger.Entry();
            pointerExit.eventID = EventTriggerType.PointerExit;
            pointerExit.callback.AddListener((data) => { videoPlayer.MouseExit(); });
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
                // Save zone before triggering
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
        if (!debugMode) return;

        Debug.Log($"🔍 EventTrigger Status for {target.name}:");
        Debug.Log($"  - Triggers count: {eventTrigger.triggers?.Count ?? 0}");

        if (eventTrigger.triggers != null)
        {
            foreach (var trigger in eventTrigger.triggers)
            {
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

        Debug.Log("🔍 Validating all EventTriggers in scene...");

        foreach (var video in allVideos)
        {
            bool isValid = ValidateEventTriggerSetup(video.gameObject);
            if (isValid)
            {
                validCount++;
            }
            else
            {
                invalidCount++;
            }
        }

        Debug.Log($"📊 Validation Results: {validCount} valid, {invalidCount} invalid out of {allVideos.Length} total");
    }

    private bool ValidateEventTriggerSetup(GameObject target)
    {
        EnhancedVideoPlayer videoPlayer = target.GetComponent<EnhancedVideoPlayer>();
        if (videoPlayer == null) return false;

        BoxCollider boxCollider = target.GetComponent<BoxCollider>();
        EventTrigger eventTrigger = target.GetComponent<EventTrigger>();

        List<string> issues = new List<string>();

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

        // Check zone assignment
        if (string.IsNullOrEmpty(videoPlayer.LastKnownZone) || videoPlayer.LastKnownZone == "Home")
        {
            issues.Add("LastKnownZone not set");
        }

        if (issues.Count > 0)
        {
            Debug.LogWarning($"❌ {target.name}: {string.Join(", ", issues)}");
            return false;
        }
        else
        {
            Debug.Log($"✅ {target.name}: Valid setup");
            return true;
        }
    }
}

// Note: Editor menu items are now in EventTriggerFixerEditor.cs (put in Editor folder)