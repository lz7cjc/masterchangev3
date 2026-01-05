using UnityEngine;

[DefaultExecutionOrder(-999)]
public class LoadingManagerInspector : MonoBehaviour
{
    void Awake()
    {
        Debug.Log("[LOADINSPECT] === CHECKING ALL LOADING PANELS ===");

        // Find ALL VRLoadingManager instances
        VRLoadingManager[] managers = FindObjectsOfType<VRLoadingManager>(true);
        Debug.Log($"[LOADINSPECT] Found {managers.Length} VRLoadingManager instances");

        foreach (VRLoadingManager manager in managers)
        {
            Debug.Log($"[LOADINSPECT] Manager on: {manager.gameObject.name}");
            Debug.Log($"[LOADINSPECT]   Active: {manager.gameObject.activeSelf}");
            Debug.Log($"[LOADINSPECT]   Enabled: {manager.enabled}");
            Debug.Log($"[LOADINSPECT]   Scene: {manager.gameObject.scene.name}");

            // Check if it has a loading panel
            Transform loadingPanel = manager.transform.Find("LoadingPanel");
            if (loadingPanel != null)
            {
                Debug.Log($"[LOADINSPECT]   LoadingPanel active: {loadingPanel.gameObject.activeSelf}");

                // FORCE HIDE IT
                loadingPanel.gameObject.SetActive(false);
                Debug.Log($"[LOADINSPECT]   ⚠️ FORCE DISABLED LoadingPanel");
            }
        }

        // Find ALL Canvas objects that might be blocking
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        Debug.Log($"[LOADINSPECT] Found {canvases.Length} Canvas objects");

        foreach (Canvas canvas in canvases)
        {
            if (canvas.gameObject.name.ToLower().Contains("load"))
            {
                Debug.Log($"[LOADINSPECT] ⚠️ Loading-related Canvas: {canvas.gameObject.name}");
                Debug.Log($"[LOADINSPECT]   Active: {canvas.gameObject.activeSelf}, SortOrder: {canvas.sortingOrder}");

                if (canvas.sortingOrder > 100)
                {
                    Debug.Log($"[LOADINSPECT]   ⚠️ HIGH SORT ORDER - might be blocking view!");
                }
            }
        }
    }
}