using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.Events;


#if UNITY_EDITOR

/// <summary>
/// Editor script to help set up video prefabs with their data before saving them as prefab assets
/// Works with any prefab that has the EnhancedVideoPlayer component
/// </summary>
[CustomEditor(typeof(EnhancedVideoPlayer))]
public class VideoPrefabEditor : Editor
{
    // Serialized properties
    private SerializedProperty videoUrlLink;
    private SerializedProperty title;
    private SerializedProperty description;
    private SerializedProperty lastKnownZone;
    private SerializedProperty hoverTimeRequired;
    private SerializedProperty titleTextProp;
    private SerializedProperty descriptionTextProp;

    private void OnEnable()
    {
        // Cache serialized properties
        videoUrlLink = serializedObject.FindProperty("VideoUrlLink");
        title = serializedObject.FindProperty("title");
        description = serializedObject.FindProperty("description");
        lastKnownZone = serializedObject.FindProperty("LastKnownZone");
        hoverTimeRequired = serializedObject.FindProperty("hoverTimeRequired");
        titleTextProp = serializedObject.FindProperty("titleText");
        descriptionTextProp = serializedObject.FindProperty("descriptionText");
    }

    public override void OnInspectorGUI()
    {
        EnhancedVideoPlayer player = (EnhancedVideoPlayer)target;

        // Draw the default inspector first
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Video Prefab Setup", EditorStyles.boldLabel);

        // Quick setup section
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

        if (GUILayout.Button("Setup EventTrigger"))
        {
            SetupEventTrigger(player);
        }

        if (GUILayout.Button("Auto-Find Text Components"))
        {
            AutoFindTextComponents(player);
        }

        if (GUILayout.Button("Update Text Preview"))
        {
            UpdateTextPreview(player);
        }

        if (GUILayout.Button("Validate Prefab Setup"))
        {
            ValidatePrefabSetup(player);
        }

        EditorGUILayout.EndVertical();

        // Prefab saving section
        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Prefab Management", EditorStyles.boldLabel);

        if (PrefabUtility.IsPartOfPrefabInstance(player.gameObject))
        {
            if (GUILayout.Button("Apply Changes to Prefab"))
            {
                ApplyPrefabChanges(player);
            }
        }
        else if (!PrefabUtility.IsPartOfPrefabAsset(player.gameObject))
        {
            if (GUILayout.Button("Save as New Prefab"))
            {
                SaveAsNewPrefab(player);
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void SetupEventTrigger(EnhancedVideoPlayer player)
    {
        // Ensure BoxCollider
        BoxCollider boxCollider = player.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = player.gameObject.AddComponent<BoxCollider>();
            Debug.Log("Added BoxCollider");
        }
        boxCollider.isTrigger = true;

        // Setup EventTrigger
        EventTrigger eventTrigger = player.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = player.gameObject.AddComponent<EventTrigger>();
            Debug.Log("Added EventTrigger");
        }

        if (eventTrigger.triggers == null)
        {
            eventTrigger.triggers = new List<EventTrigger.Entry>();
        }

        // Clear and rebuild triggers
        eventTrigger.triggers.Clear();

        // Add PointerEnter
        EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
        pointerEnter.eventID = EventTriggerType.PointerEnter;
        pointerEnter.callback = new EventTrigger.TriggerEvent();
        pointerEnter.callback.AddListener((data) => player.OnPointerEnter());
        eventTrigger.triggers.Add(pointerEnter);

        // Add PointerExit
        EventTrigger.Entry pointerExit = new EventTrigger.Entry();
        pointerExit.eventID = EventTriggerType.PointerExit;
        pointerExit.callback = new EventTrigger.TriggerEvent();
        pointerExit.callback.AddListener((data) => player.OnPointerExit());
        eventTrigger.triggers.Add(pointerExit);

        // Add PointerClick
        EventTrigger.Entry pointerClick = new EventTrigger.Entry();
        pointerClick.eventID = EventTriggerType.PointerClick;
        pointerClick.callback = new EventTrigger.TriggerEvent();
        pointerClick.callback.AddListener((data) => player.OnPointerClick());
        eventTrigger.triggers.Add(pointerClick);

        EditorUtility.SetDirty(player.gameObject);
        Debug.Log("✅ EventTrigger setup complete");
    }

    private void AutoFindTextComponents(EnhancedVideoPlayer player)
    {
        // Look for TMP_Text components in children
        TMP_Text[] tmpComponents = player.GetComponentsInChildren<TMP_Text>(true);

        foreach (var tmp in tmpComponents)
        {
            string lowerName = tmp.gameObject.name.ToLower();

            if (player.titleText == null && (lowerName.Contains("title") || lowerName.Contains("name")))
            {
                player.titleText = tmp;
                Debug.Log($"Found title component: {tmp.gameObject.name}");
            }
            else if (player.descriptionText == null && (lowerName.Contains("desc") || lowerName.Contains("info")))
            {
                player.descriptionText = tmp;
                Debug.Log($"Found description component: {tmp.gameObject.name}");
            }
        }

        EditorUtility.SetDirty(player);

        if (player.titleText == null && player.descriptionText == null)
        {
            Debug.LogWarning("No text components found. Please assign them manually.");
        }
    }

    private void UpdateTextPreview(EnhancedVideoPlayer player)
    {
        if (player.titleText != null && !string.IsNullOrEmpty(player.title))
        {
            player.titleText.text = player.title;
            EditorUtility.SetDirty(player.titleText);
        }

        if (player.descriptionText != null && !string.IsNullOrEmpty(player.description))
        {
            player.descriptionText.text = player.description;
            EditorUtility.SetDirty(player.descriptionText);
        }

        Debug.Log("✅ Text preview updated");
    }

    private void ValidatePrefabSetup(EnhancedVideoPlayer player)
    {
        List<string> issues = new List<string>();
        List<string> warnings = new List<string>();

        // Check essential data
        if (string.IsNullOrEmpty(player.VideoUrlLink))
            issues.Add("VideoUrlLink is not set");

        if (string.IsNullOrEmpty(player.title))
            warnings.Add("Title is not set");

        if (string.IsNullOrEmpty(player.LastKnownZone) || player.LastKnownZone == "Home")
            warnings.Add("Zone is not properly set");

        // Check components
        if (player.GetComponent<BoxCollider>() == null)
            issues.Add("Missing BoxCollider");

        if (player.GetComponent<EventTrigger>() == null)
            issues.Add("Missing EventTrigger");

        if (player.titleText == null)
            warnings.Add("Title text component not assigned");

        if (player.descriptionText == null)
            warnings.Add("Description text component not assigned");

        // Report results
        if (issues.Count == 0 && warnings.Count == 0)
        {
            EditorUtility.DisplayDialog("Validation Success",
                "✅ Prefab is properly configured!",
                "OK");
        }
        else
        {
            string message = "";

            if (issues.Count > 0)
            {
                message += "❌ ISSUES:\n" + string.Join("\n", issues) + "\n\n";
            }

            if (warnings.Count > 0)
            {
                message += "⚠️ WARNINGS:\n" + string.Join("\n", warnings);
            }

            EditorUtility.DisplayDialog("Validation Results", message, "OK");
        }
    }

    private void ApplyPrefabChanges(EnhancedVideoPlayer player)
    {
        GameObject prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(player.gameObject);

        if (prefabRoot != null)
        {
            PrefabUtility.ApplyPrefabInstance(prefabRoot, InteractionMode.UserAction);
            Debug.Log("✅ Changes applied to prefab");
        }
    }

    private void SaveAsNewPrefab(EnhancedVideoPlayer player)
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Video Prefab",
            string.IsNullOrEmpty(player.title) ? "NewVideoPrefab" : player.title.Replace(" ", "_"),
            "prefab",
            "Save the video prefab"
        );

        if (!string.IsNullOrEmpty(path))
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(player.gameObject, path);
            Debug.Log($"✅ Saved prefab to: {path}");

            // Select the newly created prefab
            Selection.activeObject = prefab;
        }
    }
}

/// <summary>
/// Window for batch setup of multiple video prefabs
/// </summary>
public class VideoPrefabBatchSetupWindow : EditorWindow
{
    private List<VideoSetupData> videoDataList = new List<VideoSetupData>();
    private GameObject templatePrefab;
    private string targetZone = "Beaches";
    private Vector2 scrollPosition;

    [System.Serializable]
    public class VideoSetupData
    {
        public GameObject prefab;
        public string videoUrl = "";
        public string title = "";
        public string description = "";
        public string zone = "Beaches";
        public bool setupComplete = false;
    }

    [MenuItem("Tools/Video Prefab Batch Setup")]
    public static void ShowWindow()
    {
        GetWindow<VideoPrefabBatchSetupWindow>("Video Batch Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Video Prefab Batch Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Template section
        EditorGUILayout.BeginVertical("box");
        templatePrefab = (GameObject)EditorGUILayout.ObjectField("Template Prefab", templatePrefab, typeof(GameObject), false);
        targetZone = EditorGUILayout.TextField("Default Zone", targetZone);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // Add buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Video Entry"))
        {
            videoDataList.Add(new VideoSetupData { zone = targetZone });
        }

        if (GUILayout.Button("Load Sample Data"))
        {
            LoadSampleData();
        }

        if (GUILayout.Button("Clear All"))
        {
            videoDataList.Clear();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Video list
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        for (int i = 0; i < videoDataList.Count; i++)
        {
            var data = videoDataList[i];

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Video {i + 1}", EditorStyles.boldLabel);

            if (data.setupComplete)
            {
                EditorGUILayout.LabelField("✅", GUILayout.Width(20));
            }

            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                videoDataList.RemoveAt(i);
                continue;
            }
            EditorGUILayout.EndHorizontal();

            data.prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", data.prefab, typeof(GameObject), false);
            data.videoUrl = EditorGUILayout.TextField("Video URL", data.videoUrl);
            data.title = EditorGUILayout.TextField("Title", data.title);
            data.description = EditorGUILayout.TextArea(data.description, GUILayout.Height(40));
            data.zone = EditorGUILayout.TextField("Zone", data.zone);

            if (data.prefab != null && GUILayout.Button("Setup This Prefab"))
            {
                SetupSinglePrefab(data);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // Batch operations
        if (GUILayout.Button("Setup All Prefabs", GUILayout.Height(30)))
        {
            SetupAllPrefabs();
        }

        if (GUILayout.Button("Apply All Changes", GUILayout.Height(30)))
        {
            ApplyAllChanges();
        }
    }

    private void LoadSampleData()
    {
        videoDataList.Clear();

        // Sample beach videos
        videoDataList.Add(new VideoSetupData
        {
            title = "Brazil Beach Walk",
            videoUrl = "https://example.com/beaches/brazil/beach1.mp4",
            description = "Relax on the beautiful beaches of Brazil",
            zone = "Beaches"
        });

        videoDataList.Add(new VideoSetupData
        {
            title = "Isle of Wight Coastline",
            videoUrl = "https://example.com/beaches/wight/beach2.mp4",
            description = "Explore the stunning coastline",
            zone = "Beaches"
        });
    }

    private void SetupSinglePrefab(VideoSetupData data)
    {
        if (data.prefab == null)
        {
            Debug.LogError("Prefab is null!");
            return;
        }

        EnhancedVideoPlayer player = data.prefab.GetComponent<EnhancedVideoPlayer>();
        if (player == null)
        {
            Debug.LogError($"Prefab {data.prefab.name} doesn't have EnhancedVideoPlayer component!");
            return;
        }

        // Set the data
        player.VideoUrlLink = data.videoUrl;
        player.title = data.title;
        player.description = data.description;
        player.LastKnownZone = data.zone;

        // Update text components if they exist
        if (player.titleText != null)
        {
            player.titleText.text = data.title;
            EditorUtility.SetDirty(player.titleText);
        }

        if (player.descriptionText != null)
        {
            player.descriptionText.text = data.description;
            EditorUtility.SetDirty(player.descriptionText);
        }

        // Setup EventTrigger
        SetupEventTriggerForPrefab(player);

        EditorUtility.SetDirty(player);
        data.setupComplete = true;

        Debug.Log($"✅ Setup complete for: {data.title}");
    }

    private void SetupEventTriggerForPrefab(EnhancedVideoPlayer player)
    {
        // Ensure BoxCollider
        BoxCollider boxCollider = player.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = player.gameObject.AddComponent<BoxCollider>();
        }
        boxCollider.isTrigger = true;

        // Setup EventTrigger
        EventTrigger eventTrigger = player.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = player.gameObject.AddComponent<EventTrigger>();
        }

        if (eventTrigger.triggers == null)
        {
            eventTrigger.triggers = new List<EventTrigger.Entry>();
        }

        // Rebuild triggers
        eventTrigger.triggers.Clear();

        // Add triggers
        EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
        pointerEnter.eventID = EventTriggerType.PointerEnter;
        pointerEnter.callback = new EventTrigger.TriggerEvent();
        pointerEnter.callback.AddListener((data) => player.OnPointerEnter());
        eventTrigger.triggers.Add(pointerEnter);

        EventTrigger.Entry pointerExit = new EventTrigger.Entry();
        pointerExit.eventID = EventTriggerType.PointerExit;
        pointerExit.callback = new EventTrigger.TriggerEvent();
        pointerExit.callback.AddListener((data) => player.OnPointerExit());
        eventTrigger.triggers.Add(pointerExit);
    }

    private void SetupAllPrefabs()
    {
        int successCount = 0;

        foreach (var data in videoDataList)
        {
            if (data.prefab != null && !data.setupComplete)
            {
                SetupSinglePrefab(data);
                successCount++;
            }
        }

        Debug.Log($"✅ Setup complete for {successCount} prefabs");
    }

    private void ApplyAllChanges()
    {
        int appliedCount = 0;

        foreach (var data in videoDataList)
        {
            if (data.prefab != null && data.setupComplete)
            {
                if (PrefabUtility.IsPartOfPrefabInstance(data.prefab))
                {
                    GameObject prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(data.prefab);
                    PrefabUtility.ApplyPrefabInstance(prefabRoot, InteractionMode.UserAction);
                    appliedCount++;
                }
                else
                {
                    // Save as new prefab
                    string path = $"Assets/Prefabs/Videos/{data.title.Replace(" ", "_")}.prefab";

                    // Ensure directory exists
                    string directory = System.IO.Path.GetDirectoryName(path);
                    if (!AssetDatabase.IsValidFolder(directory))
                    {
                        System.IO.Directory.CreateDirectory(directory);
                        AssetDatabase.Refresh();
                    }

                    PrefabUtility.SaveAsPrefabAsset(data.prefab, path);
                    appliedCount++;
                }
            }
        }

        Debug.Log($"✅ Applied changes to {appliedCount} prefabs");
        AssetDatabase.Refresh();
    }
}

/// <summary>
/// Context menu additions for quick prefab setup
/// </summary>
public static class VideoPrefabContextMenu
{
    [MenuItem("GameObject/Video Prefab/Quick Setup", false, 10)]
    static void QuickSetup()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null) return;

        EnhancedVideoPlayer player = selected.GetComponent<EnhancedVideoPlayer>();
        if (player == null)
        {
            player = selected.AddComponent<EnhancedVideoPlayer>();
        }

        // Basic setup
        if (player.GetComponent<BoxCollider>() == null)
        {
            BoxCollider collider = selected.AddComponent<BoxCollider>();
            collider.isTrigger = true;
        }

        if (player.GetComponent<EventTrigger>() == null)
        {
            selected.AddComponent<EventTrigger>();
        }

        Debug.Log($"✅ Quick setup complete for {selected.name}");
    }

    [MenuItem("GameObject/Video Prefab/Quick Setup", true)]
    static bool ValidateQuickSetup()
    {
        return Selection.activeGameObject != null;
    }
}

#endif