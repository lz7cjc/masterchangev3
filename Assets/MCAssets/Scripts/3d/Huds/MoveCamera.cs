using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// MoveCamera - Teleportation system for non-standard sections with manual film placement
/// UPDATED: Converted from Rigidbody to CharacterController for better VR teleportation
/// </summary>
public class MoveCamera : MonoBehaviour
{
    [Header("Hover Settings")]
    public bool mousehover = false;
    public float Counter = 0;
    public int delay = 3;

    [Header("Player Reference")]
    public CharacterController player; // CHANGED: From Rigidbody to CharacterController

    [Header("Target Settings")]
    private GameObject cameraTarget;
    public bool isTitle = false;
    public TMP_Text TMP_title;

    [Header("HUD Integration")]
    [SerializeField] private closeAllHuds closeAllHuds;
    [SerializeField] private ToggleActiveIcons toggleActiveIcons;

    [Header("Zone Mappings")]
    [SerializeField] private List<ZoneMapping> zoneMappings = new List<ZoneMapping>();

    // Cached references
    private hudCountdown hudCountdown;

    [System.Serializable]
    public class ZoneMapping
    {
        public GameObject targetObject;
        public string zoneID;  // This should match the exact ID expected by StartUp.cs
    }

    public void Start()
    {
        Debug.Log("[MoveCamera] Start - Initializing");

        // Cache hudCountdown reference
        hudCountdown = FindFirstObjectByType<hudCountdown>();

        if (hudCountdown == null)
        {
            Debug.LogWarning("[MoveCamera] hudCountdown not found in scene!");
        }

        if (closeAllHuds == null)
        {
            Debug.LogWarning("[MoveCamera] closeAllHuds not assigned!");
        }

        if (toggleActiveIcons == null)
        {
            Debug.LogWarning("[MoveCamera] toggleActiveIcons not assigned!");
        }

        if (player == null)
        {
            Debug.LogError("[MoveCamera] Player CharacterController not assigned!");
        }

        Debug.Log("[MoveCamera] Initialization complete");
    }

    void Update()
    {
        if (mousehover)
        {
            // Update icon state
            if (toggleActiveIcons != null)
            {
                toggleActiveIcons.HoverIcon();
            }

            // Update countdown display
            if (hudCountdown != null)
            {
                hudCountdown.SetCountdown(delay, Counter);
            }

            Counter += Time.deltaTime;

            if (Counter >= delay)
            {
                Debug.Log("[MoveCamera] Delay reached - executing teleport");

                mousehover = false;
                Counter = 0;

                if (hudCountdown != null)
                {
                    hudCountdown.resetCountdown();
                }

                showandhide();
            }
        }
    }

    public void MouseHoverChangeScene(GameObject _cameraTarget)
    {
        Debug.Log($"[MoveCamera] Mouse hover started - Target: {(_cameraTarget != null ? _cameraTarget.name : "NULL")}");

        if (isTitle && TMP_title != null)
        {
            TMP_title.color = Color.white;
        }

        mousehover = true;
        cameraTarget = _cameraTarget;
    }

    public void MouseExit()
    {
        Debug.Log("[MoveCamera] Mouse exit - resetting hover state");

        if (toggleActiveIcons != null)
        {
            toggleActiveIcons.DefaultIcon();
        }

        mousehover = false;
        Counter = 0;

        if (hudCountdown != null)
        {
            hudCountdown.resetCountdown();
        }
    }

    private void showandhide()
    {
        if (player == null)
        {
            Debug.LogError("[MoveCamera] Cannot teleport - player is null!");
            return;
        }

        if (cameraTarget == null)
        {
            Debug.LogError("[MoveCamera] Cannot teleport - cameraTarget is null!");
            return;
        }

        Debug.Log($"[MoveCamera] Executing teleport to: {cameraTarget.name}");

        // Update icon to selected state
        if (toggleActiveIcons != null)
        {
            toggleActiveIcons.SelectIcon();
        }

        Counter = 0;

        // Close all HUDs before teleporting
        if (closeAllHuds != null)
        {
            closeAllHuds.CloseTheHuds("MoveCamera teleport");
        }

        // CHANGED: CharacterController teleportation (much simpler!)
        TeleportPlayer();

        // Get the correct zone ID from our mappings
        string zoneID = GetZoneIDForTarget(cameraTarget);
        if (!string.IsNullOrEmpty(zoneID))
        {
            PlayerPrefs.SetString("lastknownzone", zoneID);
            Debug.Log($"[MoveCamera] Set lastknownzone to: {zoneID}");
        }
        else
        {
            Debug.LogWarning($"[MoveCamera] No zone ID mapping found for target: {cameraTarget.name}. Using name directly.");
            PlayerPrefs.SetString("lastknownzone", cameraTarget.name);
        }
    }

    /// <summary>
    /// CHANGED: Simple CharacterController teleportation
    /// </summary>
    private void TeleportPlayer()
    {
        // Step 1: Disable CharacterController (required for teleportation)
        player.enabled = false;

        // Step 2: Set the player as a child of the cameraTarget
        player.transform.SetParent(cameraTarget.transform);

        // Step 3: Reset the player's position to (0, 0, 0) relative to the cameraTarget
        player.transform.localPosition = Vector3.zero;
        player.transform.localRotation = Quaternion.identity;
        player.transform.localScale = Vector3.one;

        // Step 4: Re-enable CharacterController
        player.enabled = true;

        Debug.Log($"[MoveCamera] Player teleported to: {cameraTarget.name}");
        Debug.Log($"[MoveCamera] Player world position: {player.transform.position}");
    }

    /// <summary>
    /// Helper method to find the zone ID for a target GameObject
    /// </summary>
    private string GetZoneIDForTarget(GameObject target)
    {
        foreach (var mapping in zoneMappings)
        {
            if (mapping.targetObject == target)
            {
                return mapping.zoneID;
            }
        }
        return null;  // No mapping found
    }

    #region Public Methods

    /// <summary>
    /// Force teleport without countdown (for testing or scripted teleportation)
    /// </summary>
    public void ForceTeleport(GameObject target)
    {
        if (target != null && player != null)
        {
            cameraTarget = target;
            showandhide();
        }
        else
        {
            Debug.LogError("[MoveCamera] ForceTeleport failed - null references");
        }
    }

    /// <summary>
    /// Check if currently hovering
    /// </summary>
    public bool IsHovering => mousehover;

    /// <summary>
    /// Get hover progress (0-1)
    /// </summary>
    public float HoverProgress => mousehover ? Mathf.Clamp01(Counter / delay) : 0f;

    #endregion

    #region Inspector Helpers

    [ContextMenu("Auto-Find References")]
    private void AutoFindReferences()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.GetComponent<CharacterController>();
            }
        }

        if (closeAllHuds == null)
        {
            closeAllHuds = FindFirstObjectByType<closeAllHuds>();
        }

        if (toggleActiveIcons == null)
        {
            toggleActiveIcons = GetComponent<ToggleActiveIcons>();
        }

        Debug.Log($"[MoveCamera] Auto-found: Player={player?.name}, CloseHuds={closeAllHuds?.name}, ToggleIcons={toggleActiveIcons?.name}");
    }

    [ContextMenu("Validate Setup")]
    private void ValidateSetup()
    {
        string status = "[MoveCamera] Validation:\n";
        status += $"- Player: {(player != null ? "✓" : "✗ MISSING")}\n";
        status += $"- CloseAllHuds: {(closeAllHuds != null ? "✓" : "✗ MISSING")}\n";
        status += $"- ToggleActiveIcons: {(toggleActiveIcons != null ? "✓" : "✗ MISSING")}\n";
        status += $"- Zone Mappings: {zoneMappings.Count} defined";

        Debug.Log(status);
    }

    [ContextMenu("List Zone Mappings")]
    private void ListZoneMappings()
    {
        Debug.Log($"[MoveCamera] Zone Mappings ({zoneMappings.Count}):");
        for (int i = 0; i < zoneMappings.Count; i++)
        {
            var mapping = zoneMappings[i];
            Debug.Log($"  [{i}] Target: {mapping.targetObject?.name ?? "NULL"} → Zone ID: {mapping.zoneID ?? "NULL"}");
        }
    }

    #endregion
}