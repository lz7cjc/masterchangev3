using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CameraManager : MonoBehaviour
{
    [System.Serializable]
    public class ZoneConfig
    {
        public string zoneName;           // "Beaches", "Travel", etc.
        public GameObject zoneTarget;     // The transform to move to
    }

    [Header("Core References")]
    [SerializeField] private Rigidbody player;
    [SerializeField] private hudCountdown hudCountdown;
    [SerializeField] private closeAllHuds closeAllHuds;
    [SerializeField] private ToggleActiveIcons toggleActiveIcons;

    [Header("Camera Settings")]
    [SerializeField] private float hoverDelay = 3.0f;
    [SerializeField] private bool useGravity = true;

    [Header("Zone Configuration")]
    [SerializeField] private List<ZoneConfig> zones = new List<ZoneConfig>();

    // Runtime variables
    private float hoverCounter = 0;
    private bool isHovering = false;
    private ZoneConfig currentHoverZone;

    private void Start()
    {
        hudCountdown = FindFirstObjectByType<hudCountdown>();
    }

    private void Update()
    {
        if (isHovering && currentHoverZone != null)
        {
            toggleActiveIcons.HoverIcon();
            hudCountdown.SetCountdown((int)hoverDelay, hoverCounter);
            hoverCounter += Time.deltaTime;

            if (hoverCounter >= hoverDelay)
            {
                MoveToZone(currentHoverZone);
                ResetHoverState();
            }
        }
    }

    // Call this from event triggers
    public void OnPointerEnter(string zoneName)
    {
        ZoneConfig zone = GetZoneByName(zoneName);
        if (zone != null)
        {
            currentHoverZone = zone;
            isHovering = true;
            hoverCounter = 0;
            toggleActiveIcons.HoverIcon();
        }
    }

    // Call this from event triggers
    public void OnPointerExit()
    {
        ResetHoverState();
    }

    private void ResetHoverState()
    {
        isHovering = false;
        hoverCounter = 0;
        currentHoverZone = null;
        toggleActiveIcons.DefaultIcon();
        hudCountdown.resetCountdown();
    }

    private void MoveToZone(ZoneConfig zone)
    {
        if (zone == null || zone.zoneTarget == null) return;

        // Visual feedback
        toggleActiveIcons.SelectIcon();

        // Close all HUDs
        if (closeAllHuds != null)
        {
            closeAllHuds.CloseTheHuds();
        }

        // Set physics properties
        if (player != null)
        {
            player.useGravity = useGravity;

            // Set the player as a child of the target
            player.transform.SetParent(zone.zoneTarget.transform);
            player.transform.localPosition = Vector3.zero;
        }

        // Save to PlayerPrefs - use the actual zone name directly
        PlayerPrefs.SetString("lastknownzone", zone.zoneName);
        Debug.Log($"Moving to zone: {zone.zoneName}");
    }

    // Helper method to find zone by name
    private ZoneConfig GetZoneByName(string name)
    {
        return zones.Find(z => z.zoneName == name);
    }

    // Public method that can be called from other scripts
    public void MoveToZoneByName(string zoneName)
    {
        ZoneConfig zone = GetZoneByName(zoneName);
        if (zone != null)
        {
            MoveToZone(zone);
        }
        else
        {
            Debug.LogWarning($"Zone not found: {zoneName}");
        }
    }
}