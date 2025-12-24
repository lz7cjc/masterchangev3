using UnityEngine;

/// <summary>
/// HomeButton - Returns player to home/starting zone
/// Works with GazeHoverTrigger for gaze-based activation
/// </summary>
public class HomeButton : MonoBehaviour
{
    [Header("Home Zone Settings")]
    [SerializeField] private string homeZoneName = "Beach";
    
    [Header("System References")]
    [SerializeField] private ZoneManager zoneManager;
    [SerializeField] private ToggleActiveIcons toggleActiveIcons;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    void Start()
    {
        // Auto-find references
        if (zoneManager == null)
        {
            zoneManager = FindFirstObjectByType<ZoneManager>();
        }

        if (toggleActiveIcons == null)
        {
            toggleActiveIcons = GetComponent<ToggleActiveIcons>();
        }

        LogDebug($"HomeButton initialized - Home zone: {homeZoneName}");
    }

    /// <summary>
    /// Teleport to home zone - called by GazeHoverTrigger on completion
    /// </summary>
    public void GoToHome()
    {
        if (zoneManager == null)
        {
            Debug.LogError("[HomeButton] ZoneManager not found!");
            return;
        }

        LogDebug($"Going to home zone: {homeZoneName}");

        // Set icon to selected state
        if (toggleActiveIcons != null)
        {
            toggleActiveIcons.SelectIcon();
        }

        // Trigger zone teleportation
        zoneManager.MoveToZoneByName(homeZoneName);
    }

    /// <summary>
    /// Set a different home zone at runtime
    /// </summary>
    public void SetHomeZone(string newHomeZone)
    {
        homeZoneName = newHomeZone;
        PlayerPrefs.SetString("homeZone", newHomeZone);
        PlayerPrefs.Save();
        LogDebug($"Home zone updated to: {homeZoneName}");
    }

    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[HomeButton] {message}");
        }
    }

    #region Inspector Helpers

    [ContextMenu("Test Go Home")]
    private void TestGoHome()
    {
        if (Application.isPlaying)
        {
            GoToHome();
        }
    }

    [ContextMenu("List Available Zones")]
    private void ListZones()
    {
        if (zoneManager != null)
        {
            string[] zones = zoneManager.GetAllZoneNames();
            Debug.Log($"[HomeButton] Available zones ({zones.Length}):");
            foreach (string zone in zones)
            {
                Debug.Log($"  - {zone}");
            }
        }
    }

    #endregion
}
