//using UnityEngine;
//using UnityEngine.SceneManagement;
//using Unity.XR.Management;
//using UnityEngine.XR.Management;

//public class VRSceneManager : MonoBehaviour
//{
//    private void Awake()
//    {
//        // Make this object persist between scene loads
//        DontDestroyOnLoad(this.gameObject);

//        // Register for scene load events
//        SceneManager.sceneLoaded += OnSceneLoaded;
//    }

//    private void OnDestroy()
//    {
//        // Unregister when destroyed
//        SceneManager.sceneLoaded -= OnSceneLoaded;
//    }

//    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
//    {
//        // Check if the loaded scene is in the 2D folder or any subfolder
//        if (scene.path.Contains("Assets/MCAssets/Scripts/2d"))
//        {
//            StopXR();
//        }
//        else
//        {
//            // Optionally restart XR for non-2D scenes
//            // StartXR();
//        }
//    }

//    public void StopXR()
//    {
//        if (XRGeneralSettings.Instance == null || XRGeneralSettings.Instance.Manager == null)
//        {
//            Debug.LogError("XRGeneralSettings or XRManagerSettings is null!");
//            return;
//        }
//        XRGeneralSettings.Instance.Manager.StopSubsystems();
//        XRGeneralSettings.Instance.Manager.DeinitializeLoader();
//        Debug.Log("XR stopped for 2D scene: " + SceneManager.GetActiveScene().name);
//    }

//    // Optional: Add this method if you want to restart XR for non-2D scenes
//    /*
//    public void StartXR()
//    {
//        if (XRGeneralSettings.Instance == null || XRGeneralSettings.Instance.Manager == null)
//        {
//            Debug.LogError("XRGeneralSettings or XRManagerSettings is null!");
//            return;
//        }
        
//        XRGeneralSettings.Instance.Manager.InitializeLoaderSync();
//        if (XRGeneralSettings.Instance.Manager.activeLoader != null)
//        {
//            XRGeneralSettings.Instance.Manager.StartSubsystems();
//            Debug.Log("XR started for scene: " + SceneManager.GetActiveScene().name);
//        }
//    }
//    */
//}