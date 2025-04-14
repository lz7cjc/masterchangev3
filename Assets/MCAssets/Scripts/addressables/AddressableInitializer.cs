using UnityEngine;
using UnityEngine.SceneManagement;

public class AddressableInitializer : MonoBehaviour
{
    [SerializeField] private string[] labelsToPreload = { "Cottage", "Terrain" };
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private bool logDebugMessages = true;

    private static bool hasInitialized = false;

    private void Awake()
    {
        if (!hasInitialized)
        {
            hasInitialized = true;

            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            InitializeBackgroundPreloader();
            Debug.Log("[AddressableInitializer] First-time initialization complete");
        }
        else
        {
            // Already initialized, destroy this instance
            Debug.Log("[AddressableInitializer] Already initialized, destroying duplicate");
            Destroy(gameObject);
        }
    }

    public void Start()
    {
        Debug.Log("[AddressableInitializer] Start method called.");
        var startTime = Time.realtimeSinceStartup;

        // Existing initialization logic
        // ...

        Debug.Log($"[AddressableInitializer] Initialization completed in {Time.realtimeSinceStartup - startTime} seconds.");
    }

    private void InitializeBackgroundPreloader()
    {
        // Check if preloader already exists
        BackgroundPreloader existingPreloader = FindFirstObjectByType<BackgroundPreloader>();

        if (existingPreloader != null)
        {
            Debug.Log("[AddressableInitializer] BackgroundPreloader already exists");
            return;
        }

        // Create new preloader
        GameObject preloaderObj = new GameObject("BackgroundPreloader");
        BackgroundPreloader preloader = preloaderObj.AddComponent<BackgroundPreloader>();

        // Set serialized fields via code since we're creating it dynamically
        // Use reflection to set private field
        System.Reflection.FieldInfo labelsField = typeof(BackgroundPreloader).GetField("labelsToPreload",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (labelsField != null)
        {
            labelsField.SetValue(preloader, labelsToPreload);
        }

        System.Reflection.FieldInfo debugField = typeof(BackgroundPreloader).GetField("showDebugLogs",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (debugField != null)
        {
            debugField.SetValue(preloader, logDebugMessages);
        }

        DontDestroyOnLoad(preloaderObj);
        Debug.Log("[AddressableInitializer] Created new BackgroundPreloader");
    }
}