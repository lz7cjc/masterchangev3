using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
public class DatabaseDebugger : EditorWindow
{
    private VideoDatabaseManager databaseManager;

    [MenuItem("Tools/Video System/Database Debugger")]
    public static void ShowWindow()
    {
        GetWindow<DatabaseDebugger>("Database Debugger");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Database Debugger", EditorStyles.boldLabel);

        // Find database manager
        if (databaseManager == null)
        {
            databaseManager = FindObjectOfType<VideoDatabaseManager>();
        }

        if (databaseManager == null)
        {
            EditorGUILayout.HelpBox("No VideoDatabaseManager found in the scene!", MessageType.Error);
            return;
        }

        EditorGUILayout.LabelField($"Database initialized: {databaseManager.IsInitialized}");
        EditorGUILayout.LabelField($"Entry count: {databaseManager.EntryCount}");

        if (GUILayout.Button("Force Save Database"))
        {
            ForceCreateDatabaseWithUrls();
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Check Database File"))
        {
            string path = "Assets/Resources/VideoDatabase.json";
            if (System.IO.File.Exists(path))
            {
                string content = System.IO.File.ReadAllText(path);
                Debug.Log($"Database file exists with content: {content.Substring(0, Mathf.Min(100, content.Length))}...");
            }
            else
            {
                Debug.LogError($"Database file does not exist at {path}");
            }
        }
    }

    private void ForceCreateDatabaseWithUrls()
    {
        // This will manually create a database with entries from your URL list
        TextAsset urlsAsset = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/public_urls.txt");

        if (urlsAsset == null)
        {
            Debug.LogError("Could not find public_urls.txt in Resources folder!");
            return;
        }

        string[] urls = urlsAsset.text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

        // Create database directly
        VideoDatabase database = new VideoDatabase();
        database.Entries = new System.Collections.Generic.List<VideoEntry>();

        int videoCount = 0;
        foreach (string url in urls)
        {
            string trimmedUrl = url.Trim();
            if (trimmedUrl.EndsWith(".mp4"))
            {
                VideoEntry entry = new VideoEntry();
                entry.PublicUrl = trimmedUrl;
                entry.FileName = System.IO.Path.GetFileName(trimmedUrl);

                database.Entries.Add(entry);
                videoCount++;
            }
        }

        // Save directly to file
        string json = JsonUtility.ToJson(database, true);
        System.IO.File.WriteAllText("Assets/Resources/VideoDatabase.json", json);

        AssetDatabase.Refresh();

        Debug.Log($"Manually created database with {videoCount} entries");

        // Try to reload the database
        databaseManager.LoadDatabaseFromJson();
    }
}
#endif