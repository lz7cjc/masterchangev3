using UnityEngine;
using UnityEngine.SceneManagement; // Added for Scene class
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;

// Make sure this is in an Editor folder!
public class FindPersistentObjects : EditorWindow
{
    private Vector2 scrollPosition;
    private List<GameObject> persistentObjects = new List<GameObject>();

    // This is the menu item that should appear in the Tools menu
    [MenuItem("Tools/Find Persistent Objects", false, 100)]
    public static void ShowWindow()
    {
        // Get existing open window or create a new one
        FindPersistentObjects window = EditorWindow.GetWindow<FindPersistentObjects>("Persistent Objects Finder");
        window.Show();
    }

    private void OnEnable()
    {
        FindAllPersistentObjects();
    }

    private void FindAllPersistentObjects()
    {
        persistentObjects.Clear();

        // Get all root GameObjects in DontDestroyOnLoad scene
        List<GameObject> rootObjects = GetDontDestroyOnLoadObjects();
        foreach (var obj in rootObjects)
        {
            persistentObjects.Add(obj);
            FindChildAudioSources(obj);
        }
    }

    private List<GameObject> GetDontDestroyOnLoadObjects()
    {
        List<GameObject> result = new List<GameObject>();

        // This is a bit hacky but works to find DontDestroyOnLoad objects
        GameObject temp = new GameObject();
        Object.DontDestroyOnLoad(temp);
        Scene dontDestroyOnLoadScene = temp.scene;
        Object.DestroyImmediate(temp);

        // Get all root objects in the DontDestroyOnLoad scene
        GameObject[] allObjects = dontDestroyOnLoadScene.GetRootGameObjects();
        foreach (var obj in allObjects)
        {
            result.Add(obj);
        }

        return result;
    }

    private void FindChildAudioSources(GameObject obj)
    {
        // Check if this object has an AudioSource
        AudioSource[] sources = obj.GetComponentsInChildren<AudioSource>(true);
        if (sources.Length > 0)
        {
            if (!persistentObjects.Contains(obj))
            {
                persistentObjects.Add(obj);
            }
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginVertical();

        if (GUILayout.Button("Refresh Persistent Objects List"))
        {
            FindAllPersistentObjects();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Found {persistentObjects.Count} Persistent Objects:", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        foreach (GameObject obj in persistentObjects)
        {
            EditorGUILayout.BeginHorizontal("box");

            // Display object information
            EditorGUILayout.BeginVertical();
            EditorGUILayout.ObjectField(obj, typeof(GameObject), true);

            // Check for audio sources
            AudioSource[] audioSources = obj.GetComponentsInChildren<AudioSource>(true);
            if (audioSources.Length > 0)
            {
                EditorGUILayout.LabelField($"Contains {audioSources.Length} AudioSource components", EditorStyles.boldLabel);

                foreach (AudioSource source in audioSources)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.ObjectField(source, typeof(AudioSource), true);

                    EditorGUILayout.BeginVertical();
                    if (source.clip != null)
                    {
                        EditorGUILayout.LabelField($"Clip: {source.clip.name}");
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Clip: None");
                    }

                    EditorGUILayout.LabelField($"Play On Awake: {source.playOnAwake}");
                    EditorGUILayout.LabelField($"Loop: {source.loop}");
                    EditorGUILayout.LabelField($"Is Playing: {source.isPlaying}");
                    EditorGUILayout.EndVertical();

                    if (GUILayout.Button("Select", GUILayout.Width(80)))
                    {
                        Selection.activeGameObject = source.gameObject;
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndVertical();

            // Actions column
            EditorGUILayout.BeginVertical(GUILayout.Width(100));

            if (GUILayout.Button("Select"))
            {
                Selection.activeGameObject = obj;
            }

            if (GUILayout.Button("Destroy"))
            {
                Object.DestroyImmediate(obj);
                persistentObjects.Remove(obj);
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }
}