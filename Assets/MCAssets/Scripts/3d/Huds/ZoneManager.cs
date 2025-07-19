using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

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

    // Runtime variables
    private ZoneDefinition currentHoverZone;
    private float hoverCounter = 0f;
    private bool isHovering = false;

    private void Start()
    {
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
    }

    private void Update()
    {
        if (isHovering && currentHoverZone != null)
        {
            hudCountdown.SetCountdown((int)hoverDelay, hoverCounter);
            hoverCounter += Time.deltaTime;

            if (hoverCounter >= hoverDelay)
            {
                // Store reference before moving (in case MoveToZone clears it)
                ZoneDefinition zoneToMoveTo = currentHoverZone;

                // Move to zone (this will reset hover state)
                MoveToZone(zoneToMoveTo);

                // Ensure state is completely reset
                isHovering = false;
                currentHoverZone = null;
                hoverCounter = 0f;
            }
        }
    }
    // Add these methods to match what other scripts are expecting
    // ----------------------------------------------------------
    // Called from other scripts that expect the StartHovering method
    public void StartHovering(string zoneName, MonoBehaviour caller)
    {
        OnHoverEnter(zoneName);
    }

    // Called from other scripts that expect the StopHovering method
    public void StopHovering()
    {
        if (currentHoverZone != null)
        {
            OnHoverExit(currentHoverZone.zoneName);
        }
        else
        {
            ResetHoverState();
        }
    }
    // ----------------------------------------------------------

    // Call this from UI Event Trigger with OnPointerEnter (pass the zone name as string parameter)
    public void OnHoverEnter(string zoneName)
    {
        // Find the zone by name
        ZoneDefinition zone = GetZoneByName(zoneName);
        if (zone == null) return;

        // Set current zone
        currentHoverZone = zone;
        isHovering = true;
        hoverCounter = 0f;

        // Update visuals
        if (zone.iconRenderer != null && zone.hoverSprite != null)
        {
            zone.iconRenderer.sprite = zone.hoverSprite;
        }

        Debug.Log($"Hovering over zone: {zoneName}");
    }

    // Call this from UI Event Trigger with OnPointerExit
    public void OnHoverExit(string zoneName)
    {
        // Find the zone by name
        ZoneDefinition zone = GetZoneByName(zoneName);
        if (zone == null) return;

        // Reset state using the private method
        ResetHoverState();

        // Reset visual
        if (zone.iconRenderer != null && zone.defaultSprite != null)
        {
            zone.iconRenderer.sprite = zone.defaultSprite;
        }

        Debug.Log($"Exited hover for zone: {zoneName}");
    }

    private void ResetHoverState()
    {
        isHovering = false;
        hoverCounter = 0f;

        if (currentHoverZone != null && currentHoverZone.iconRenderer != null && currentHoverZone.defaultSprite != null)
        {
            // Reset sprite to default if we're not in selected state
            currentHoverZone.iconRenderer.sprite = currentHoverZone.defaultSprite;
        }

        currentHoverZone = null;

        // Reset countdown display
        if (hudCountdown != null)
        {
            hudCountdown.resetCountdown();
        }
    }

    private void MoveToZone(ZoneDefinition zone)
    {
        if (zone == null || zone.zoneTarget == null) return;

        // IMMEDIATELY reset hover state to prevent re-triggering
        ResetHoverState();

        // Update visual to selected state
        if (zone.iconRenderer != null && zone.selectedSprite != null)
        {
            zone.iconRenderer.sprite = zone.selectedSprite;
        }

        // Close all HUDs
        if (closeAllHuds != null)
        {
            closeAllHuds.CloseTheHuds();
        }

        // Set physics properties and move player
        if (player != null)
        {
            // First, unparent the player to avoid transform conflicts
            player.transform.SetParent(null);

            // Stop all physics
            player.isKinematic = true;
            player.linearVelocity = Vector3.zero;
            player.angularVelocity = Vector3.zero;

            // Set position directly in world space instead of parenting
            player.transform.position = zone.zoneTarget.transform.position;
            player.transform.rotation = zone.zoneTarget.transform.rotation;
            player.transform.localScale = Vector3.one;

            // Set gravity after positioning
            player.useGravity = useGravity;

            // Optional: Only parent if you specifically need the player to follow the zone target
            // player.transform.SetParent(zone.zoneTarget.transform);
        }

        // Save to PlayerPrefs
        PlayerPrefs.SetString("lastknownzone", zone.zoneName);
        Debug.Log($"Successfully moved to zone: {zone.zoneName} at position {zone.zoneTarget.transform.position}");
    }
    private ZoneDefinition GetZoneByName(string name)
    {
        return zones.Find(z => z.zoneName == name);
    }

    // Public method to move to a zone by name (can be called from other scripts)
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

    // NEW METHOD: Move to zone by GameObject reference - more efficient and consistent
    public void MoveToZoneByGameObject(GameObject targetObject)
    {
        if (targetObject == null)
        {
            Debug.LogWarning("Target GameObject is null");
            return;
        }

        // Find the zone that matches this target GameObject
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

    // For use with direct GameObject reference in Event Trigger
    public void OnHoverEnterFromGameObject(GameObject targetObject)
    {
        // Find the zone with this target
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

    // For use with direct GameObject reference in Event Trigger
    public void OnHoverExitFromGameObject(GameObject targetObject)
    {
        // Find the zone with this target
        ZoneDefinition zone = zones.Find(z => z.zoneTarget == targetObject);
        if (zone != null)
        {
            OnHoverExit(zone.zoneName);
        }
    }
}