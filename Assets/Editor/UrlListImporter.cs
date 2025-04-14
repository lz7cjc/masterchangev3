using UnityEngine;
using UnityEditor;
using System.IO;

public class UrlListImporter : EditorWindow
{
    public TextAsset urlListFile;
    private VideoDatabaseManager databaseManager;

    [MenuItem("Tools/Video System/URL List Importer")]
    public static void ShowWindow()
    {
        GetWindow<UrlListImporter>("URL Importer");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Import Video URLs", EditorStyles.boldLabel);

        urlListFile = (TextAsset)EditorGUILayout.ObjectField("URL List File", urlListFile, typeof(TextAsset), false);

        // Find database manager
        if (databaseManager == null)
        {
            databaseManager = FindObjectOfType<VideoDatabaseManager>();
        }

        if (databaseManager == null)
        {
            EditorGUILayout.HelpBox("No VideoDatabaseManager found in the scene!", MessageType.Error);
            if (GUILayout.Button("Create Database Manager"))
            {
                GameObject obj = new GameObject("VideoDatabaseManager");
                databaseManager = obj.AddComponent<VideoDatabaseManager>();
            }
            return;
        }

        if (GUILayout.Button("Import URLs") && urlListFile != null)
        {
            ImportUrls();
        }
    }

    void ImportUrls()
    {
        string[] lines = urlListFile.text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

        // Create a temporary file in the expected format
        string tempContent = "filename\tpublic_url\n";

        int videoCount = 0;
        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();

            // Only process video URLs
            if (trimmedLine.EndsWith(".mp4") &&
                trimmedLine.Contains("storage.googleapis.com/masterchange/"))
            {
                // Extract filename from URL
                string filename = Path.GetFileName(trimmedLine);
                tempContent += filename + "\t" + trimmedLine + "\n";
                videoCount++;
            }
        }

        // Create a temporary TextAsset
        string tempPath = "Assets/Resources/TempDatabase.txt";
        File.WriteAllText(tempPath, tempContent);
        AssetDatabase.Refresh();

        // Load the temp file
        TextAsset tempAsset = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/TempDatabase.txt");

        // Assign to database manager and parse
        databaseManager.cloudDatabaseFile = tempAsset;
        databaseManager.ParseCloudDatabase();

        // Save to JSON
        databaseManager.SaveDatabaseToJson();

        // Clean up
        AssetDatabase.DeleteAsset(tempPath);

        EditorUtility.DisplayDialog("Import Complete",
            $"Successfully imported {videoCount} video URLs into the database.",
            "OK");
    }
}