using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor;
using System;
using System.Linq;

// This component imports film JSON data into the video database
public class FilmJsonImporter : MonoBehaviour
{
    [Header("Import Settings")]
    [SerializeField] private TextAsset jsonFile;
    [SerializeField] private bool importOnAwake = false;
    [SerializeField] private bool overwriteExisting = false;

    [Header("Prefab Mapping")]
    [Tooltip("Maps zone names to prefab types")]
    [SerializeField] private List<ZonePrefabMapping> zonePrefabMappings = new List<ZonePrefabMapping>();

    [System.Serializable]
    public class ZonePrefabMapping
    {
        public string ZoneKeyword;
        public string PrefabType;
    }

    [System.Serializable]
    public class FilmData
    {
        public string filmname;
        public string publicURL;
        public string Label;
        public string Description;
        public string Country;
        public string Locale;
        public string zones;
    }

    [System.Serializable]
    public class FilmDataList
    {
        public List<FilmData> films;
    }

    private VideoDatabaseManager databaseManager;

    private void Awake()
    {
        // Find database manager
        databaseManager = FindObjectOfType<VideoDatabaseManager>();

        if (databaseManager == null)
        {
            Debug.LogError("No VideoDatabaseManager found. Cannot import JSON.");
            return;
        }

        if (importOnAwake && jsonFile != null)
        {
            ImportJson();
        }
    }

    // Import JSON data into the database
    public void ImportJson()
    {
        if (jsonFile == null)
        {
            Debug.LogError("No JSON file assigned for import.");
            return;
        }

        if (databaseManager == null)
        {
            databaseManager = FindObjectOfType<VideoDatabaseManager>();
            if (databaseManager == null)
            {
                Debug.LogError("No VideoDatabaseManager found. Cannot import JSON.");
                return;
            }
        }

        try
        {
            // Parse the JSON file
            List<FilmData> films = ParseJsonFile();

            if (films == null || films.Count == 0)
            {
                Debug.LogWarning("No film data found in JSON file.");
                return;
            }

            // Load existing database or create a new one
            if (!overwriteExisting)
            {
                databaseManager.LoadDatabaseFromJson();
            }

            // Process each film entry
            int imported = 0;
            foreach (FilmData film in films)
            {
                if (string.IsNullOrEmpty(film.publicURL))
                    continue;

                if (ImportFilm(film))
                    imported++;
            }

            // Save the database
            databaseManager.SaveDatabaseToJson();

            Debug.Log($"Successfully imported {imported} films from JSON.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error importing JSON: {ex.Message}\n{ex.StackTrace}");
        }
    }

    // Parse the JSON file into a list of FilmData objects
    private List<FilmData> ParseJsonFile()
    {
        try
        {
            // First try parsing as an array
            string jsonText = jsonFile.text;

            // Unity's JsonUtility doesn't directly support JSON arrays, so we need a wrapper
            // Check if it starts with '[' indicating an array
            if (jsonText.TrimStart().StartsWith("["))
            {
                // Wrap the array in an object
                jsonText = "{\"films\":" + jsonText + "}";
                FilmDataList dataList = JsonUtility.FromJson<FilmDataList>(jsonText);
                return dataList.films;
            }
            else
            {
                // Try parsing as a single object
                FilmData singleFilm = JsonUtility.FromJson<FilmData>(jsonText);
                if (singleFilm != null)
                {
                    return new List<FilmData> { singleFilm };
                }
            }

            // If both approaches fail, try manual parsing using simple string operations
            // This is a fallback method when the JSON format is not compatible with JsonUtility
            return ManualJsonParse(jsonText);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error parsing JSON: {ex.Message}");
            return null;
        }
    }

    // Manual JSON parsing as a fallback
    private List<FilmData> ManualJsonParse(string jsonText)
    {
        List<FilmData> results = new List<FilmData>();

        // Simple splitting by object boundaries
        string[] objects = jsonText.Split(new[] { "},{" }, StringSplitOptions.None);

        foreach (string obj in objects)
        {
            // Clean up the object string
            string cleanObj = obj.Replace("[{", "").Replace("}]", "").Replace("{", "").Replace("}", "");

            // Split by properties
            string[] properties = cleanObj.Split(',');

            FilmData film = new FilmData();

            foreach (string prop in properties)
            {
                string[] keyValue = prop.Split(':');
                if (keyValue.Length != 2)
                    continue;

                string key = keyValue[0].Trim().Replace("\"", "");
                string value = keyValue[1].Trim().Replace("\"", "");

                // Skip null values
                if (value == "null")
                    continue;

                // Assign to the appropriate property
                switch (key.ToLower())
                {
                    case "filmname":
                        film.filmname = value;
                        break;
                    case "publicurl":
                        film.publicURL = value;
                        break;
                    case "label":
                        film.Label = value;
                        break;
                    case "description":
                        film.Description = value;
                        break;
                    case "country":
                        film.Country = value;
                        break;
                    case "locale":
                        film.Locale = value;
                        break;
                    case "zones":
                        film.zones = value;
                        break;
                }
            }

            // Only add if it has a URL
            if (!string.IsNullOrEmpty(film.publicURL))
            {
                results.Add(film);
            }
        }

        return results;
    }

    // Import a single film into the database
    private bool ImportFilm(FilmData film)
    {
        try
        {
            // Skip if no zones are assigned
            if (string.IsNullOrEmpty(film.zones))
            {
                Debug.Log($"Skipping film with no zones: {film.publicURL}");
                return false;
            }

            // Check if this film already exists in the database
            VideoEntry existingEntry = databaseManager.GetEntryByUrl(film.publicURL);

            if (existingEntry != null && !overwriteExisting)
            {
                Debug.Log($"Skipping existing entry for URL: {film.publicURL}");
                return false;
            }

            // Create a new entry or update existing one
            VideoEntry entry = existingEntry ?? new VideoEntry();

            // Set basic properties
            entry.PublicUrl = film.publicURL;
            entry.FileName = !string.IsNullOrEmpty(film.filmname) ? film.filmname : Path.GetFileName(film.publicURL);
            entry.Title = !string.IsNullOrEmpty(film.Label) ? film.Label : entry.FileName;
            entry.Description = film.Description ?? "";

            // Handle zones (comma-separated)
            if (!string.IsNullOrEmpty(film.zones))
            {
                string[] zoneArray = film.zones.Split(',');
                foreach (string zone in zoneArray)
                {
                    string trimmedZone = zone.Trim();
                    if (!string.IsNullOrEmpty(trimmedZone) && !entry.Zones.Contains(trimmedZone))
                    {
                        entry.Zones.Add(trimmedZone);
                    }
                }

                // Double check we have at least one valid zone after processing
                if (entry.Zones.Count == 0)
                {
                    Debug.Log($"Skipping film with no valid zones after processing: {film.publicURL}");
                    return false;
                }
            }

            // Determine prefab type based on zone keywords
            entry.Prefab = DeterminePrefabType(film.zones);

            // Add to database if new
            if (existingEntry == null)
            {
                databaseManager.GetType().GetMethod("AddEntry", System.Reflection.BindingFlags.Instance |
                                                  System.Reflection.BindingFlags.Public |
                                                  System.Reflection.BindingFlags.NonPublic)
                    ?.Invoke(databaseManager, new object[] { entry });
            }

            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error importing film {film.publicURL}: {ex.Message}");
            return false;
        }
    }

    // Determine prefab type based on film zones
    private string DeterminePrefabType(string zones)
    {
        if (string.IsNullOrEmpty(zones))
            return "Default";

        foreach (ZonePrefabMapping mapping in zonePrefabMappings)
        {
            if (!string.IsNullOrEmpty(mapping.ZoneKeyword) &&
                zones.Contains(mapping.ZoneKeyword))
            {
                return mapping.PrefabType;
            }
        }

        return "Default";
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(FilmJsonImporter))]
public class FilmJsonImporterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        FilmJsonImporter importer = (FilmJsonImporter)target;

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Import JSON Now"))
        {
            importer.ImportJson();
        }
    }
}
#endif