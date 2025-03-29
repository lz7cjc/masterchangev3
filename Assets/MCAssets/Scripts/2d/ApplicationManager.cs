using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class ApplicationManager : MonoBehaviour
{
    private static ApplicationManager instance;

    void Awake()
    {
        // Singleton pattern to prevent duplicates
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("ApplicationManager initialized and set to persist between scenes");
        }
        else
        {
            Debug.Log("Duplicate ApplicationManager found, destroying this instance");
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // If this is your bootstrap scene, you may want to load your first gameplay scene
        // Uncomment and modify the line below with your first actual gameplay scene name
        // SceneManager.LoadScene("YourFirstGameplayScene");
    }

    void Update()
    {
        // Check for Escape key as a backup method to quit using the new Input System
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Debug.Log("Escape key pressed, quitting application");
            QuitApplication();
        }
    }

    public void QuitApplication()
    {
        Debug.Log("Application quit requested");

#if UNITY_EDITOR
        Debug.Log("Running in editor, stopping play mode");
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Debug.Log("Quitting application");
            Application.Quit();
#endif
    }

    // This is called automatically when the OS close button is clicked
    void OnApplicationQuit()
    {
        Debug.Log("Application is quitting");
        // Perform any cleanup here before the application closes
    }
}