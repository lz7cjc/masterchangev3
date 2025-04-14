using UnityEngine;
using UnityEditor;
using System.IO;

public class TextToAssetConverter : EditorWindow
{
    private string sourceFilePath = "";
    private string assetName = "VideoDatabase";

    [MenuItem("Tools/Video System/Text To TextAsset Converter")]
    public static void ShowWindow()
    {
        GetWindow<TextToAssetConverter>("Text Converter");
    }

    void OnGUI()
    {
        GUILayout.Label("Convert Text File to TextAsset", EditorStyles.boldLabel);

        EditorGUILayout.Space(10);

        sourceFilePath = EditorGUILayout.TextField("Text File Path:", sourceFilePath);

        if (GUILayout.Button("Browse File..."))
        {
            string path = EditorUtility.OpenFilePanel("Select Text File", "", "txt");
            if (!string.IsNullOrEmpty(path))
            {
                sourceFilePath = path;
            }
        }

        assetName = EditorGUILayout.TextField("Asset Name:", assetName);

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Create TextAsset"))
        {
            if (string.IsNullOrEmpty(sourceFilePath))
            {
                EditorUtility.DisplayDialog("Error", "Please select a text file first.", "OK");
                return;
            }

            ConvertTextFileToAsset(sourceFilePath, assetName);
        }
    }

    private void ConvertTextFileToAsset(string filePath, string assetName)
    {
        try
        {
            // Read the file content
            string content = File.ReadAllText(filePath);

            // Ensure Resources directory exists
            if (!Directory.Exists("Assets/Resources"))
            {
                Directory.CreateDirectory("Assets/Resources");
            }

            // Write the content to a file in the Resources folder
            string resourcePath = $"Assets/Resources/{assetName}.txt";
            File.WriteAllText(resourcePath, content);

            // Refresh the AssetDatabase
            AssetDatabase.Refresh();

            // Find the created asset
            TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(resourcePath);

            if (textAsset != null)
            {
                // Select the asset
                Selection.activeObject = textAsset;
                EditorGUIUtility.PingObject(textAsset);

                EditorUtility.DisplayDialog("Success",
                    $"TextAsset created successfully at {resourcePath}\n\nNow drag this asset to the 'Cloud Database File' field in your VideoDatabaseManager.",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Error",
                    "Failed to load the created asset. Check the console for details.",
                    "OK");
            }
        }
        catch (System.Exception ex)
        {
            EditorUtility.DisplayDialog("Error",
                $"An error occurred: {ex.Message}",
                "OK");

            Debug.LogError($"Text to TextAsset conversion error: {ex}");
        }
    }
}