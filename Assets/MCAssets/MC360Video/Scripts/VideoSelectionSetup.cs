using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
/// <summary>
/// Editor-only utility script to add colliders and event triggers to existing video objects
/// This is an editor script - it doesn't need to be attached to any GameObject
/// </summary>
public class VideoSelectionSetup : EditorWindow
{
    private Vector3 colliderSize = new Vector3(2, 2, 0.2f);
    private bool isTrigger = true;
    private float selectionTimeThreshold = 2.0f;
    private string playerPrefsKey = "VideoUrl";
    private string videoAppScene = "360VideoApp";
    private bool logDetails = true;
    private bool removeToggleShowHideVideo = true;
    private bool removeEmptyScripts = true;

    [MenuItem("Tools/Video Management/Video Selection Setup")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(VideoSelectionSetup), false, "Video Selection Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Video Selection Setup", EditorStyles.boldLabel);

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("Collider Settings", EditorStyles.boldLabel);
        colliderSize = EditorGUILayout.Vector3Field("Collider Size", colliderSize);
        isTrigger = EditorGUILayout.Toggle("Is Trigger", isTrigger);

        EditorGUILayout.Space(5);

        EditorGUILayout.LabelField("Selection Settings", EditorStyles.boldLabel);
        selectionTimeThreshold = EditorGUILayout.FloatField("Selection Time Threshold", selectionTimeThreshold);
        playerPrefsKey = EditorGUILayout.TextField("PlayerPrefs Key", playerPrefsKey);
        videoAppScene = EditorGUILayout.TextField("Video App Scene", videoAppScene);

        EditorGUILayout.Space(5);

        EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
        logDetails = EditorGUILayout.Toggle("Log Details", logDetails);
        removeToggleShowHideVideo = EditorGUILayout.Toggle("Remove Toggle Show Hide Video", removeToggleShowHideVideo);
        removeEmptyScripts = EditorGUILayout.Toggle("Remove Empty Scripts", removeEmptyScripts);

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Add Components To All Videos", GUILayout.Height(30)))
        {
            AddComponentsToAllVideos();
        }

        if (GUILayout.Button("Check Missing Components"))
        {
            CheckMissingComponents();
        }

        EditorGUILayout.Space(5);

        EditorGUILayout.HelpBox(
            "This utility adds BoxColliders and VideoSelectionHandlers to all video objects in your scene.\n\n" +
            "It works with both EnhancedVideoPlayer and legacy ToggleShowHideVideo components.\n\n" +
            "1. Set your desired collider and selection settings\n" +
            "2. Click 'Add Components To All Videos' to apply changes\n\n" +
            "Note: This is an editor-only utility and doesn't need to be attached to any GameObject.",
            MessageType.Info);
    }

    /// <summary>
    /// Add required components to all video objects in the scene with auto-sized colliders
    /// </summary>
    private void AddComponentsToAllVideos()
    {
        // Find all EnhancedVideoPlayer components in the scene
        EnhancedVideoPlayer[] videoPlayers = Object.FindObjectsOfType<EnhancedVideoPlayer>();

        if (videoPlayers.Length == 0)
        {
            Debug.Log("No EnhancedVideoPlayer components found in the scene.");

            // Try to find legacy video components
            var legacyPlayerType = System.Type.GetType("ToggleShowHideVideo");
            if (legacyPlayerType != null)
            {
                MonoBehaviour[] legacyPlayers = Object.FindObjectsOfType(legacyPlayerType) as MonoBehaviour[];
                if (legacyPlayers != null && legacyPlayers.Length > 0)
                {
                    Debug.Log($"Found {legacyPlayers.Length} legacy ToggleShowHideVideo components. Adding components to those instead.");

                    int collidersAdded = 0;
                    int handlersAdded = 0;
                    int titlesFixed = 0;

                    foreach (MonoBehaviour legacyPlayer in legacyPlayers)
                    {
                        GameObject videoObj = legacyPlayer.gameObject;

                        // Get the VideoUrlLink field using reflection
                        System.Reflection.FieldInfo urlField = legacyPlayerType.GetField("VideoUrlLink");
                        string videoUrl = urlField != null ? (string)urlField.GetValue(legacyPlayer) : "";

                        // Skip objects with no URL
                        if (string.IsNullOrEmpty(videoUrl))
                        {
                            Debug.LogWarning($"Skipping {videoObj.name} - No video URL assigned");
                            continue;
                        }

                        // Remove ToggleShowHideVideo if option is enabled
                        if (removeToggleShowHideVideo)
                        {
                            // Get the title if possible
                            string title = "";
                            System.Reflection.FieldInfo titleField = legacyPlayerType.GetField("TMP_title");
                            if (titleField != null)
                            {
                                TMPro.TextMeshProUGUI titleComponent = (TMPro.TextMeshProUGUI)titleField.GetValue(legacyPlayer);
                                if (titleComponent != null)
                                {
                                    title = titleComponent.text;
                                }
                            }

                            // Now we can safely remove it
                            DestroyImmediate(legacyPlayer);

                            if (logDetails) Debug.Log($"Removed ToggleShowHideVideo from {videoObj.name}");

                            // Add EnhancedVideoPlayer instead
                            EnhancedVideoPlayer enhancedPlayer = videoObj.AddComponent<EnhancedVideoPlayer>();
                            enhancedPlayer.VideoUrlLink = videoUrl;
                            enhancedPlayer.title = title;
                        }

                        // Add BoxCollider if missing
                        if (videoObj.GetComponent<BoxCollider>() == null)
                        {
                            BoxCollider boxCollider = videoObj.AddComponent<BoxCollider>();

                            // Auto-size the collider based on renderers or rect transform
                            AutoSizeCollider(videoObj, boxCollider);

                            boxCollider.isTrigger = isTrigger;
                            collidersAdded++;

                            if (logDetails) Debug.Log($"Added auto-sized BoxCollider to {videoObj.name}");
                        }
                        else
                        {
                            // If collider exists but might need resizing
                            BoxCollider existingCollider = videoObj.GetComponent<BoxCollider>();
                            AutoSizeCollider(videoObj, existingCollider);
                            existingCollider.isTrigger = isTrigger;
                            if (logDetails) Debug.Log($"Auto-resized existing BoxCollider on {videoObj.name}");
                        }

                        // Add VideoSelectionHandler if missing
                        VideoSelectionHandler selectionHandler = videoObj.GetComponent<VideoSelectionHandler>();
                        if (selectionHandler == null)
                        {
                            selectionHandler = videoObj.AddComponent<VideoSelectionHandler>();

                            // Use reflection to safely call Initialize if it exists
                            var initializeMethod = selectionHandler.GetType().GetMethod("Initialize");
                            if (initializeMethod != null)
                            {
                                try
                                {
                                    initializeMethod.Invoke(selectionHandler, new object[] { videoUrl, selectionTimeThreshold, playerPrefsKey, videoAppScene });
                                }
                                catch (System.Exception ex)
                                {
                                    Debug.LogError($"Error initializing VideoSelectionHandler: {ex.Message}");

                                    // Try to set fields directly
                                    var videoUrlField = selectionHandler.GetType().GetField("videoUrl");
                                    if (videoUrlField != null) videoUrlField.SetValue(selectionHandler, videoUrl);

                                    var thresholdField = selectionHandler.GetType().GetField("selectionTimeThreshold");
                                    if (thresholdField != null) thresholdField.SetValue(selectionHandler, selectionTimeThreshold);

                                    var keyField = selectionHandler.GetType().GetField("playerPrefsKey");
                                    if (keyField != null) keyField.SetValue(selectionHandler, playerPrefsKey);

                                    var sceneField = selectionHandler.GetType().GetField("videoAppScene");
                                    if (sceneField != null) sceneField.SetValue(selectionHandler, videoAppScene);
                                }
                            }

                            handlersAdded++;

                            if (logDetails) Debug.Log($"Added VideoSelectionHandler to {videoObj.name} with URL: {videoUrl}");
                        }

                        // Fix title text if it's "Sample Text"
                        TMPro.TextMeshProUGUI[] textComponents = videoObj.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
                        foreach (TMPro.TextMeshProUGUI textComponent in textComponents)
                        {
                            if (textComponent != null && textComponent.text == "Sample Text" && !string.IsNullOrEmpty(videoObj.name))
                            {
                                // Try to get a proper title
                                string newTitle = "Sample Text";

                                // Try to get title from EnhancedVideoPlayer
                                EnhancedVideoPlayer enhancedPlayer = videoObj.GetComponent<EnhancedVideoPlayer>();
                                if (enhancedPlayer != null && !string.IsNullOrEmpty(enhancedPlayer.title))
                                {
                                    newTitle = enhancedPlayer.title;
                                }
                                // Fallback to object name
                                else if (videoObj.name.StartsWith("Video_"))
                                {
                                    newTitle = videoObj.name.Substring(6); // Remove "Video_" prefix
                                }

                                if (newTitle != "Sample Text")
                                {
                                    textComponent.text = newTitle;
                                    titlesFixed++;
                                    if (logDetails) Debug.Log($"Updated text from 'Sample Text' to '{newTitle}' on {videoObj.name}");
                                }
                            }
                        }

                        // Remove empty scripts if option is enabled
                        if (removeEmptyScripts)
                        {
                            RemoveEmptyScripts(videoObj);
                        }

                        // Add VideoAdjustmentHandler if missing
                        if (videoObj.GetComponent<VideoAdjustmentHandler>() == null)
                        {
                            videoObj.AddComponent<VideoAdjustmentHandler>();
                            if (logDetails) Debug.Log($"Added VideoAdjustmentHandler to {videoObj.name}");
                        }
                    }

                    Debug.Log($"Process complete: Added {collidersAdded} colliders, {handlersAdded} selection handlers, and fixed {titlesFixed} titles for legacy video objects.");
                    return;
                }
            }

            Debug.Log("No video components found in the scene.");
            return;
        }

        int collidersAddedNew = 0;
        int handlersAddedNew = 0;
        int titlesFixedNew = 0;
        int adjustmentHandlersAdded = 0;

        foreach (EnhancedVideoPlayer player in videoPlayers)
        {
            GameObject videoObj = player.gameObject;
            string videoUrl = player.VideoUrlLink;

            // Skip objects with no URL
            if (string.IsNullOrEmpty(videoUrl))
            {
                Debug.LogWarning($"Skipping {videoObj.name} - No video URL assigned");
                continue;
            }

            // Remove ToggleShowHideVideo if option is enabled
            if (removeToggleShowHideVideo)
            {
                var legacyPlayerType = System.Type.GetType("ToggleShowHideVideo");
                if (legacyPlayerType != null)
                {
                    Component legacyComponent = videoObj.GetComponent(legacyPlayerType);
                    if (legacyComponent != null)
                    {
                        DestroyImmediate(legacyComponent);
                        if (logDetails) Debug.Log($"Removed ToggleShowHideVideo from {videoObj.name}");
                    }
                }
            }

            // Add BoxCollider if missing
            if (videoObj.GetComponent<BoxCollider>() == null)
            {
                BoxCollider boxCollider = videoObj.AddComponent<BoxCollider>();

                // Auto-size the collider based on renderers or rect transform
                AutoSizeCollider(videoObj, boxCollider);

                boxCollider.isTrigger = isTrigger;
                collidersAddedNew++;

                if (logDetails) Debug.Log($"Added auto-sized BoxCollider to {videoObj.name}");
            }
            else
            {
                // If collider exists but might need resizing
                BoxCollider existingCollider = videoObj.GetComponent<BoxCollider>();
                AutoSizeCollider(videoObj, existingCollider);
                existingCollider.isTrigger = isTrigger;
                if (logDetails) Debug.Log($"Auto-resized existing BoxCollider on {videoObj.name}");
            }

            // Add VideoSelectionHandler if missing
            VideoSelectionHandler selectionHandler = videoObj.GetComponent<VideoSelectionHandler>();
            if (selectionHandler == null)
            {
                selectionHandler = videoObj.AddComponent<VideoSelectionHandler>();

                // Try to use reflection to call Initialize
                var initializeMethod = selectionHandler.GetType().GetMethod("Initialize");
                if (initializeMethod != null)
                {
                    try
                    {
                        initializeMethod.Invoke(selectionHandler, new object[] { videoUrl, selectionTimeThreshold, playerPrefsKey, videoAppScene });
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"Error initializing VideoSelectionHandler: {ex.Message}");

                        // Try to set fields directly
                        var videoUrlField = selectionHandler.GetType().GetField("videoUrl");
                        if (videoUrlField != null) videoUrlField.SetValue(selectionHandler, videoUrl);

                        var thresholdField = selectionHandler.GetType().GetField("selectionTimeThreshold");
                        if (thresholdField != null) thresholdField.SetValue(selectionHandler, selectionTimeThreshold);

                        var keyField = selectionHandler.GetType().GetField("playerPrefsKey");
                        if (keyField != null) keyField.SetValue(selectionHandler, playerPrefsKey);

                        var sceneField = selectionHandler.GetType().GetField("videoAppScene");
                        if (sceneField != null) sceneField.SetValue(selectionHandler, videoAppScene);
                    }
                }

                handlersAddedNew++;
                if (logDetails) Debug.Log($"Added VideoSelectionHandler to {videoObj.name} with URL: {videoUrl}");
            }

            // Fix title text if it's "Sample Text"
            TMPro.TextMeshProUGUI[] textComponents = videoObj.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
            foreach (TMPro.TextMeshProUGUI textComponent in textComponents)
            {
                if (textComponent != null && textComponent.text == "Sample Text" && !string.IsNullOrEmpty(player.title))
                {
                    textComponent.text = player.title;
                    titlesFixedNew++;
                    if (logDetails) Debug.Log($"Updated text from 'Sample Text' to '{player.title}' on {videoObj.name}");
                }
            }

            // Remove empty scripts if option is enabled
            if (removeEmptyScripts)
            {
                RemoveEmptyScripts(videoObj);
            }

            // Add VideoAdjustmentHandler if missing
            if (videoObj.GetComponent<VideoAdjustmentHandler>() == null)
            {
                videoObj.AddComponent<VideoAdjustmentHandler>();
                adjustmentHandlersAdded++;
                if (logDetails) Debug.Log($"Added VideoAdjustmentHandler to {videoObj.name}");
            }
        }

        Debug.Log($"Process complete: Added {collidersAddedNew} colliders, {handlersAddedNew} selection handlers, {adjustmentHandlersAdded} adjustment handlers, and fixed {titlesFixedNew} titles for video objects.");
    }

    /// <summary>
    /// Remove MonoBehaviour components that have missing scripts
    /// </summary>
    private void RemoveEmptyScripts(GameObject obj)
    {
        // Get all components on the object
        Component[] components = obj.GetComponents<Component>();
        int removedCount = 0;

        // Check each component
        for (int i = 0; i < components.Length; i++)
        {
            // If component is null but there's a component slot, it's a missing script
            if (components[i] == null)
            {
                // We can't directly remove missing components, so we use SerializedObject
                SerializedObject serializedObject = new SerializedObject(obj);
                SerializedProperty property = serializedObject.FindProperty("m_Component");

                // Find the missing script index
                int propertyCount = property.arraySize;
                for (int j = 0; j < propertyCount; j++)
                {
                    SerializedProperty element = property.GetArrayElementAtIndex(j);
                    if (element.objectReferenceValue == null)
                    {
                        // Remove this element by moving all elements after it
                        for (int k = j; k < propertyCount - 1; k++)
                        {
                            SerializedProperty current = property.GetArrayElementAtIndex(k);
                            SerializedProperty next = property.GetArrayElementAtIndex(k + 1);
                            current.objectReferenceValue = next.objectReferenceValue;
                        }

                        // Reduce array size by 1
                        property.arraySize--;
                        serializedObject.ApplyModifiedProperties();
                        removedCount++;
                        break; // Start over since we modified the array
                    }
                }
            }
        }

        if (removedCount > 0 && logDetails)
        {
            Debug.Log($"Removed {removedCount} empty script references from {obj.name}");
        }
    }

    /// <summary>
    /// Automatically size a box collider based on renderers or rect transform
    /// </summary>
    private void AutoSizeCollider(GameObject obj, BoxCollider collider)
    {
        bool sizedSuccessfully = false;

        // Try to size based on renderers first
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            // Calculate combined bounds of all renderers
            Bounds bounds = renderers[0].bounds;
            for (int r = 1; r < renderers.Length; r++)
            {
                bounds.Encapsulate(renderers[r].bounds);
            }

            // Convert bounds to local space
            Vector3 worldCenter = bounds.center;
            Vector3 localCenter = obj.transform.InverseTransformPoint(worldCenter);

            // Get the local size based on world bounds
            Vector3 worldSize = bounds.size;
            Vector3 localSize = new Vector3(
                worldSize.x / obj.transform.lossyScale.x,
                worldSize.y / obj.transform.lossyScale.y,
                worldSize.z / obj.transform.lossyScale.z
            );

            // Apply to collider with some padding
            collider.center = localCenter;
            collider.size = localSize * 1.1f; // Add 10% padding

            sizedSuccessfully = true;
            if (logDetails) Debug.Log($"Auto-sized collider for {obj.name} based on renderers: {collider.size}");
        }

        // Try to size based on child objects if no renderer 
        if (!sizedSuccessfully && obj.transform.childCount > 0)
        {
            Bounds bounds = new Bounds(obj.transform.position, Vector3.zero);
            bool foundRenderer = false;

            // Collect all child renderers
            foreach (Transform child in obj.transform)
            {
                Renderer childRenderer = child.GetComponent<Renderer>();
                if (childRenderer != null)
                {
                    bounds.Encapsulate(childRenderer.bounds);
                    foundRenderer = true;
                }
            }

            if (foundRenderer)
            {
                // Convert bounds to local space
                Vector3 worldCenter = bounds.center;
                Vector3 localCenter = obj.transform.InverseTransformPoint(worldCenter);

                // Get the local size based on world bounds
                Vector3 worldSize = bounds.size;
                Vector3 localSize = new Vector3(
                    worldSize.x / obj.transform.lossyScale.x,
                    worldSize.y / obj.transform.lossyScale.y,
                    worldSize.z / obj.transform.lossyScale.z
                );

                // Apply to collider with some padding
                collider.center = localCenter;
                collider.size = localSize * 1.1f; // Add 10% padding

                sizedSuccessfully = true;
                if (logDetails) Debug.Log($"Auto-sized collider for {obj.name} based on child renderers: {collider.size}");
            }
        }

        // Try to size based on RectTransform if available
        if (!sizedSuccessfully)
        {
            RectTransform rectTransform = obj.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // For UI elements
                collider.size = new Vector3(rectTransform.rect.width, rectTransform.rect.height, 0.2f);
                sizedSuccessfully = true;
                if (logDetails) Debug.Log($"Auto-sized collider for {obj.name} based on RectTransform: {collider.size}");
            }
        }

        // Fall back to measuring by mesh bounds
        if (!sizedSuccessfully)
        {
            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                Bounds meshBounds = meshFilter.sharedMesh.bounds;
                Vector3 size = Vector3.Scale(meshBounds.size, obj.transform.localScale);
                collider.size = size;
                sizedSuccessfully = true;
                if (logDetails) Debug.Log($"Auto-sized collider for {obj.name} based on mesh bounds: {collider.size}");
            }
        }

        // Fall back to default size if no size reference is found
        if (!sizedSuccessfully)
        {
            collider.size = colliderSize;
            if (logDetails) Debug.Log($"Using default collider size for {obj.name}: {colliderSize}");
        }
    }

    /// <summary>
    /// Check which video objects are missing components
    /// </summary>
    private void CheckMissingComponents()
    {
        // Find all EnhancedVideoPlayer components in the scene
        EnhancedVideoPlayer[] videoPlayers = Object.FindObjectsOfType<EnhancedVideoPlayer>();

        // Try to find legacy video components
        MonoBehaviour[] legacyPlayers = null;
        var legacyPlayerType = System.Type.GetType("ToggleShowHideVideo");
        if (legacyPlayerType != null)
        {
            legacyPlayers = Object.FindObjectsOfType(legacyPlayerType) as MonoBehaviour[];
        }

        if ((videoPlayers == null || videoPlayers.Length == 0) &&
            (legacyPlayers == null || legacyPlayers.Length == 0))
        {
            Debug.Log("No video players found in the scene.");
            return;
        }

        int missingColliders = 0;
        int missingHandlers = 0;
        int missingUrls = 0;
        int missingScripts = 0;
        int sampleTexts = 0;
        int missingAdjustmentHandlers = 0;

        // Check EnhancedVideoPlayer components
        foreach (EnhancedVideoPlayer player in videoPlayers)
        {
            GameObject videoObj = player.gameObject;

            // Check for missing URL
            if (string.IsNullOrEmpty(player.VideoUrlLink))
            {
                missingUrls++;
                Debug.LogWarning($"{videoObj.name} is missing a video URL");
            }

            // Check for missing collider
            if (videoObj.GetComponent<BoxCollider>() == null)
            {
                missingColliders++;
                Debug.LogWarning($"{videoObj.name} is missing a BoxCollider");
            }

            // Check for missing selection handler
            if (videoObj.GetComponent<VideoSelectionHandler>() == null)
            {
                missingHandlers++;
                Debug.LogWarning($"{videoObj.name} is missing a VideoSelectionHandler");
            }

            // Check for missing adjustment handler
            if (videoObj.GetComponent<VideoAdjustmentHandler>() == null)
            {
                missingAdjustmentHandlers++;
                Debug.LogWarning($"{videoObj.name} is missing a VideoAdjustmentHandler");
            }

            // Check for "Sample Text"
            TMPro.TextMeshProUGUI[] textComponents = videoObj.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
            foreach (TMPro.TextMeshProUGUI textComponent in textComponents)
            {
                if (textComponent != null && textComponent.text == "Sample Text")
                {
                    sampleTexts++;
                    Debug.LogWarning($"{videoObj.name} has a TextMeshPro component with 'Sample Text'");
                    break;
                }
            }

            // Check for missing scripts
            Component[] components = videoObj.GetComponents<Component>();
            foreach (Component component in components)
            {
                if (component == null)
                {
                    missingScripts++;
                    Debug.LogWarning($"{videoObj.name} has a missing script reference");
                    break;
                }
            }
        }

        // Check legacy players if any
        if (legacyPlayers != null)
        {
            foreach (MonoBehaviour legacyPlayer in legacyPlayers)
            {
                if (legacyPlayer == null) continue;

                GameObject videoObj = legacyPlayer.gameObject;

                // Check for missing URL using reflection
                System.Reflection.FieldInfo urlField = legacyPlayerType.GetField("VideoUrlLink");
                string videoUrl = urlField != null ? (string)urlField.GetValue(legacyPlayer) : "";

                if (string.IsNullOrEmpty(videoUrl))
                {
                    missingUrls++;
                    Debug.LogWarning($"{videoObj.name} (legacy) is missing a video URL");
                }

                // Check for missing collider
                if (videoObj.GetComponent<BoxCollider>() == null)
                {
                    missingColliders++;
                    Debug.LogWarning($"{videoObj.name} (legacy) is missing a BoxCollider");
                }

                // Check for missing selection handler
                if (videoObj.GetComponent<VideoSelectionHandler>() == null)
                {
                    missingHandlers++;
                    Debug.LogWarning($"{videoObj.name} (legacy) is missing a VideoSelectionHandler");
                }

                // Check for missing adjustment handler
                if (videoObj.GetComponent<VideoAdjustmentHandler>() == null)
                {
                    missingAdjustmentHandlers++;
                    Debug.LogWarning($"{videoObj.name} (legacy) is missing a VideoAdjustmentHandler");
                }

                // Check for "Sample Text"
                TMPro.TextMeshProUGUI[] textComponents = videoObj.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
                foreach (TMPro.TextMeshProUGUI textComponent in textComponents)
                {
                    if (textComponent != null && textComponent.text == "Sample Text")
                    {
                        sampleTexts++;
                        Debug.LogWarning($"{videoObj.name} has a TextMeshPro component with 'Sample Text'");
                        break;
                    }
                }

                // Check for missing scripts
                Component[] components = videoObj.GetComponents<Component>();
                foreach (Component component in components)
                {
                    if (component == null)
                    {
                        missingScripts++;
                        Debug.LogWarning($"{videoObj.name} has a missing script reference");
                        break;
                    }
                }
            }
        }

        int totalVideos = (videoPlayers?.Length ?? 0) + (legacyPlayers?.Length ?? 0);

        Debug.Log($"Component check complete: Found {totalVideos} video objects");
        Debug.Log($"Issues found: {missingColliders} missing colliders, {missingHandlers} missing selection handlers, {missingAdjustmentHandlers} missing adjustment handlers, {missingUrls} missing URLs, {missingScripts} missing scripts, {sampleTexts} 'Sample Text' instances");

        if (missingColliders > 0 || missingHandlers > 0 || missingAdjustmentHandlers > 0 || sampleTexts > 0)
        {
            Debug.Log("Click 'Add Components To All Videos' to fix these issues");
        }
    }
}
#endif