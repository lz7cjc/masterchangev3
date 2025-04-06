using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class FindAudioSources : EditorWindow
{
    private Vector2 scrollPosition;
    private List<AudioSource> audioSources = new List<AudioSource>();

    [MenuItem("Tools/Find Audio Sources")]
    public static void ShowWindow()
    {
        GetWindow<FindAudioSources>("Audio Sources Finder");
    }

    private void OnEnable()
    {
        FindAllAudioSources();
    }

    private void FindAllAudioSources()
    {
        audioSources.Clear();

        // Find all audio sources in the scene
        AudioSource[] allAudioSources = Resources.FindObjectsOfTypeAll<AudioSource>();

        foreach (AudioSource source in allAudioSources)
        {
            // Only include sources in the current scene (not prefabs)
            if (source.gameObject.scene.isLoaded)
            {
                audioSources.Add(source);
            }
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginVertical();

        if (GUILayout.Button("Refresh Audio Sources List"))
        {
            FindAllAudioSources();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Found {audioSources.Count} Audio Sources:", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        foreach (AudioSource source in audioSources)
        {
            EditorGUILayout.BeginHorizontal("box");

            // Display source information
            EditorGUILayout.BeginVertical();
            EditorGUILayout.ObjectField(source.gameObject, typeof(GameObject), true);

            string path = GetGameObjectPath(source.gameObject);
            EditorGUILayout.LabelField($"Path: {path}");

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
            EditorGUILayout.LabelField($"Volume: {source.volume}");
            EditorGUILayout.LabelField($"Is Playing: {source.isPlaying}");

            EditorGUILayout.EndVertical();

            // Actions column
            EditorGUILayout.BeginVertical(GUILayout.Width(100));

            if (GUILayout.Button("Select"))
            {
                Selection.activeGameObject = source.gameObject;
            }

            bool newEnabled = GUILayout.Toggle(source.enabled, "Enabled");
            if (newEnabled != source.enabled)
            {
                source.enabled = newEnabled;
                EditorUtility.SetDirty(source);
            }

            bool newPlayOnAwake = GUILayout.Toggle(source.playOnAwake, "Play On Awake");
            if (newPlayOnAwake != source.playOnAwake)
            {
                source.playOnAwake = newPlayOnAwake;
                EditorUtility.SetDirty(source);
            }

            if (source.isPlaying && GUILayout.Button("Stop"))
            {
                source.Stop();
            }
            else if (!source.isPlaying && source.clip != null && GUILayout.Button("Play"))
            {
                source.Play();
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;

        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }
}