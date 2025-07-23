using UnityEngine;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Enhanced ZoneManager - Single source of truth for zone teleportation
/// Consolidated from CameraManager functionality
/// </summary>
public class ZoneManager : MonoBehaviour
{
    [System.Serializable]
    public class ZoneDefinition
    {
        public string zoneName;           // "Beaches", "Travel", etc.
        public GameObject zoneTarget;     // The transform to move to
        public SpriteRenderer iconRenderer; // The icon sprite renderer to modify
        public Sprite defaultSprite;      // Default state sprite
        public Sprite hoverSprite;        // Hover state sprite
        public Sprite selectedSprite;     // Selected state sprite
    }

    [Header("Core References")]
    [SerializeField] private Rigidbody player;
    [SerializeField] private hudCountdown hudCountdown;
    [SerializeField] private closeAllHuds closeAllHuds;

    [Header("Settings")]
    [SerializeField] private float hoverDelay = 3.0f;
    [SerializeField] private bool useGravity = true;

    [Header("Zone Definitions")]
    [SerializeField] private List<ZoneDefinition> zones = new List<ZoneDefinition>();

    // Runtime variables - same as original
    private ZoneDefinition currentHoverZone;
    private float hoverCounter = 0f;
    private bool isHovering = false;

    // Enhanced internal coordination
    private HUDSystemCoordinator hudCoordinator;
    private ToggleActiveIcons toggleActiveIcons;

    private void Start()
    {
        // Try to find enhanced coordinator and icon controller
        hudCoordinator = FindFirstObjectByType<HUDSystemCoordinator>();
        toggleActiveIcons = FindFirstObjectByType<ToggleActiveIcons>();

        // Ensure we have a hudCountdown reference
        if (hudCountdown == null)
        {
            hudCountdown = FindFirstObjectByType<hudCountdown>();
        }

        // Initialize all zones to default state
        foreach (var zone in zones)
        {
            if (zone.iconRenderer != null && zone.defaultSprite != null)
            {
                zone.iconRenderer.sprite = zone.defaultSprite;
            }
        }

        Debug.Log("Enhanced ZoneManager initialized - Single zone teleportation system");
    }

    private void Update()
    {
        if (isHovering && currentHoverZone != null)
        {
            hudCountdown?.SetCountdown((int)hoverDelay, hoverCounter);

            // Support both sprite-based and ToggleActiveIcons-based feedback
            if (toggleActiveIcons != null)
            {
                toggleActiveIcons.HoverIcon();
            }

            hoverCounter += Time.deltaTime;

            if (hoverCounter >= hoverDelay)
            {
                // Store reference before moving (in case MoveToZone clears it)
                ZoneDefinition zoneToMoveTo = currentHoverZone;
                MoveToZone(zoneToMoveTo);
                ClearHoverState();
            }
        }
    }

    #region PUBLIC API - Enhanced with CameraManager compatibility

    // Original ZoneManager methods
    public void StartHovering(string zoneName, MonoBehaviour caller)
    {
        OnHoverEnter(zoneName);
    }

    public void StopHovering()
    {
        if (currentHoverZone != null)
        {
            OnHoverExit(currentHoverZone.zoneName);
        }
        else
        {
            ClearHoverState();
        }
    }

    public void OnHoverEnter(string zoneName)
    {
        // Reset any other hover states first
        if (hudCoordinator != null)
        {
            hudCoordinator.OnZoneHoverStarted(zoneName);
        }

        ZoneDefinition zone = GetZoneByName(zoneName);
        if (zone == null) return;

        currentHoverZone = zone;
        isHovering = true;
        hoverCounter = 0f;

        // Update visuals - support both methods
        UpdateZoneVisuals(zone, VisualState.Hover);

        Debug.Log($"Hovering over zone: {zoneName}");
    }

    public void OnHoverExit(string zoneName)
    {
        ZoneDefinition zone = GetZoneByName(zoneName);
        if (zone == null) return;

        ClearHoverState();

        // Reset visual
        UpdateZoneVisuals(zone, VisualState.Default);

        // Notify coordinator if available
        hudCoordinator?.OnZoneHoverEnded(zoneName);

        Debug.Log($"Exited hover for zone: {zoneName}");
    }

    public void MoveToZoneByName(string zoneName)
    {
        ZoneDefinition zone = GetZoneByName(zoneName);
        if (zone != null)
        {
            MoveToZone(zone);
        }
        else
        {
            Debug.LogWarning($"Zone not found: {zoneName}");
        }
    }

    public void MoveToZoneByGameObject(GameObject targetObject)
    {
        if (targetObject == null)
        {
            Debug.LogWarning("Target GameObject is null");
            return;
        }

        ZoneDefinition zone = zones.Find(z => z.zoneTarget == targetObject);
        if (zone != null)
        {
            MoveToZone(zone);
        }
        else
        {
            Debug.LogWarning($"No zone found for target GameObject: {targetObject.name}");
        }
    }

    public void OnHoverEnterFromGameObject(GameObject targetObject)
    {
        ZoneDefinition zone = zones.Find(z => z.zoneTarget == targetObject);
        if (zone != null)
        {
            OnHoverEnter(zone.zoneName);
        }
        else
        {
            Debug.LogWarning($"No zone found for target: {targetObject.name}");
        }
    }

    public void OnHoverExitFromGameObject(GameObject targetObject)
    {
        ZoneDefinition zone = zones.Find(z => z.zoneTarget == targetObject);
        if (zone != null)
        {
            OnHoverExit(zone.zoneName);
        }
    }

    // CameraManager compatibility methods (for UI event triggers that might still reference them)
    public void OnPointerEnter(string zoneName)
    {
        OnHoverEnter(zoneName);
    }

    public void OnPointerExit()
    {
        if (currentHoverZone != null)
        {
            OnHoverExit(currentHoverZone.zoneName);
        }
        else
        {
            ClearHoverState();
        }
    }

    /// <summary>
    /// Public method to reset all hover states (called by coordinator)
    /// </summary>
    public void ResetAllHoverStates()
    {
        ClearHoverState();
        Debug.Log("Zone hover states reset by coordinator");
    }

    #endregion

    #region Enhanced Internal Implementation

    private enum VisualState { Default, Hover, Selected }

    private void UpdateZoneVisuals(ZoneDefinition zone, VisualState state)
    {
        // Update zone-specific sprite renderer
        if (zone.iconRenderer != null)
        {
            Sprite targetSprite = null;
            switch (state)
            {
                case VisualState.Default:
                    targetSprite = zone.defaultSprite;
                    break;
                case VisualState.Hover:
                    targetSprite = zone.hoverSprite;
                    break;
                case VisualState.Selected:
                    targetSprite = zone.selectedSprite;
                    break;
            }

            if (targetSprite != null)
            {
                zone.iconRenderer.sprite = targetSprite;
            }
        }

        // Update global ToggleActiveIcons if available (for CameraManager compatibility)
        if (toggleActiveIcons != null)
        {
            switch (state)
            {
                case VisualState.Default:
                    toggleActiveIcons.DefaultIcon();
                    break;
                case VisualState.Hover:
                    toggleActiveIcons.HoverIcon();
                    break;
                case VisualState.Selected:
                    toggleActiveIcons.SelectIcon();
                    break;
            }
        }
    }

    private void ClearHoverState()
    {
        isHovering = false;
        hoverCounter = 0f;

        if (currentHoverZone != null)
        {
            UpdateZoneVisuals(currentHoverZone, VisualState.Default);
        }

        currentHoverZone = null;

        if (hudCountdown != null)
        {
            hudCountdown.resetCountdown();
        }
    }

    private void MoveToZone(ZoneDefinition zone)
    {
        if (zone == null || zone.zoneTarget == null) return;

        // IMMEDIATELY clear hover state to prevent re-triggering
        ClearHoverState();

        // Update visual to selected state
        UpdateZoneVisuals(zone, VisualState.Selected);

        // Close all HUDs - use coordinator if available, otherwise fallback
        if (hudCoordinator != null)
        {
            hudCoordinator.CloseAllHUDs();
        }
        else if (closeAllHuds != null)
        {
            closeAllHuds.CloseTheHuds($"Zone teleport to {zone.zoneName}");
        }

        // Enhanced movement logic with better error handling
        if (player != null)
        {
            try
            {
                // First, unparent the player to avoid transform conflicts
                player.transform.SetParent(null);

                // Stop all physics
                player.isKinematic = true;
                player.linearVelocity = Vector3.zero;
                player.angularVelocity = Vector3.zero;

                // Set position and rotation directly in world space
                player.transform.position = zone.zoneTarget.transform.position;
                player.transform.rotation = zone.zoneTarget.transform.rotation;
                player.transform.localScale = Vector3.one;

                // Re-enable physics
                player.useGravity = useGravity;
                player.isKinematic = false;

                Debug.Log($"Player moved to zone: {zone.zoneName} at position {zone.zoneTarget.transform.position}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error during player movement: {e.Message}");
            }
        }

        // Save to PlayerPrefs - single source of truth
        PlayerPrefs.SetString("lastknownzone", zone.zoneName);

        // Notify coordinator
        hudCoordinator?.OnZoneChanged(zone.zoneName);

        Debug.Log($"Successfully moved to zone: {zone.zoneName}");
    }

    private ZoneDefinition GetZoneByName(string name)
    {
        return zones.Find(z => z.zoneName == name);
    }

    #endregion

    #region Enhanced Public Properties

    /// <summary>
    /// Get current hover zone
    /// </summary>
    public string CurrentHoverZone => currentHoverZone?.zoneName ?? "None";

    /// <summary>
    /// Check if currently hovering
    /// </summary>
    public bool IsHovering => isHovering;

    /// <summary>
    /// Get hover progress 0-1
    /// </summary>
    public float HoverProgress => isHovering ? Mathf.Clamp01(hoverCounter / hoverDelay) : 0f;

    /// <summary>
    /// Get all available zone names
    /// </summary>
    public string[] GetAllZoneNames()
    {
        return zones.ConvertAll(z => z.zoneName).ToArray();
    }

    #endregion

    #region Inspector Helpers

    [ContextMenu("Auto-Find Zone References")]
    private void AutoFindZoneReferences()
    {
        if (player == null)
        {
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
            {
                player = playerGO.GetComponent<Rigidbody>();
            }
        }

        if (hudCountdown == null)
        {
            hudCountdown = FindFirstObjectByType<hudCountdown>();
        }

        if (closeAllHuds == null)
        {
            closeAllHuds = FindFirstObjectByType<closeAllHuds>();
        }

        Debug.Log($"Auto-found references: Player={player?.name}, HudCountdown={hudCountdown?.name}, CloseAllHuds={closeAllHuds?.name}");
    }

    [ContextMenu("Test Zone Movement")]
    private void TestZoneMovement()
    {
        if (Application.isPlaying && zones.Count > 0)
        {
            string testZone = zones[0].zoneName;
            MoveToZoneByName(testZone);
            Debug.Log($"Test moved to: {testZone}");
        }
    }

    [ContextMenu("List All Zones")]
    private void ListAllZones()
    {
        Debug.Log($"Available zones ({zones.Count}):");
        for (int i = 0; i < zones.Count; i++)
        {
            var zone = zones[i];
            Debug.Log($"  [{i}] {zone.zoneName} -> {zone.zoneTarget?.name}");
        }
    }

    #endregion
}