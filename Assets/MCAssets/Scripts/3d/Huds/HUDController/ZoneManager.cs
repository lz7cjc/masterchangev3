using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Enhanced ZoneManager - Single source of truth for zone teleportation
/// UPDATED: Converted from Rigidbody to CharacterController for better VR teleportation
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
    [SerializeField] private CharacterController player; // CHANGED: From Rigidbody to CharacterController
    [SerializeField] private hudCountdown hudCountdown;
    [SerializeField] private closeAllHuds closeAllHuds;

    [Header("Settings")]
    [SerializeField] private float hoverDelay = 3.0f;

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

        Debug.Log("Enhanced ZoneManager initialized - Single zone teleportation system with CharacterController");
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

        // Also update ToggleActiveIcons if available
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

        // CHANGED: Simplified teleportation for CharacterController
        if (player != null)
        {
            try
            {
                Debug.Log($"Moving to zone: {zone.zoneName}");
                Debug.Log($"Target object: {zone.zoneTarget.name}");
                Debug.Log($"Player current position: {player.transform.position}");

                // Teleport using CharacterController - much simpler!
                TeleportToZone(zone);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error during player movement: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
            }
        }
    }

    private void TeleportToZone(ZoneDefinition zone)
    {
        // CHANGED: Simple CharacterController teleportation
        if (zone.zoneTarget != null)
        {
            // Step 1: Disable CharacterController (required for teleportation)
            player.enabled = false;

            // Step 2: Clear any existing parent
            player.transform.SetParent(null);

            // Step 3: Reset to world origin first
            player.transform.position = Vector3.zero;
            player.transform.rotation = Quaternion.identity;

            // Step 4: Set the parent and local position (same as StartUp script)
            player.transform.SetParent(zone.zoneTarget.transform);
            player.transform.localPosition = Vector3.zero;        // Reset to (0,0,0) relative to target
            player.transform.localRotation = Quaternion.identity;
            player.transform.localScale = Vector3.one;

            // Step 5: Re-enable CharacterController
            player.enabled = true;

            Debug.Log($"Player parented to: {zone.zoneTarget.name}");
            Debug.Log($"Local position set to: {player.transform.localPosition}");
            Debug.Log($"World position is now: {player.transform.position}");
        }
        else
        {
            // Fallback to world position if no target
            player.enabled = false;
            player.transform.position = Vector3.zero;
            player.transform.rotation = Quaternion.identity;
            player.transform.localScale = Vector3.one;
            player.enabled = true;
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
                player = playerGO.GetComponent<CharacterController>(); // CHANGED: Look for CharacterController
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

    [ContextMenu("Debug Player Position")]
    private void DebugPlayerPosition()
    {
        if (player != null)
        {
            Debug.Log($"Player Position: {player.transform.position}");
            Debug.Log($"Player Rotation: {player.transform.rotation}");
            Debug.Log($"Player Parent: {(player.transform.parent != null ? player.transform.parent.name : "None")}");
            Debug.Log($"Player Scale: {player.transform.localScale}");
            Debug.Log($"Player CharacterController Enabled: {player.enabled}");
            Debug.Log($"Player IsGrounded: {player.isGrounded}");
        }
    }

    [ContextMenu("Debug All Zone Targets")]
    private void DebugAllZoneTargets()
    {
        Debug.Log("=== All Zone Target Positions ===");
        foreach (var zone in zones)
        {
            if (zone.zoneTarget != null)
            {
                Debug.Log($"{zone.zoneName}: Position = {zone.zoneTarget.transform.position}, Rotation = {zone.zoneTarget.transform.rotation}");
            }
            else
            {
                Debug.LogWarning($"{zone.zoneName}: Zone target is NULL!");
            }
        }
    }

    #endregion
}