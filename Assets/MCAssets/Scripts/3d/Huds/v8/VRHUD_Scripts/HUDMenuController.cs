using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data-driven menu controller that supports any number of levels (depth).
/// - Uses a MenuNode tree (ScriptableObject config)
/// - Builds 3D button instances (prefab) in an arc layout
/// - Supports Open/Close (Level 1) plus Back navigation
/// - Supports leaf actions and toggle actions (e.g., walking)
/// </summary>
public class HUDMenuController : MonoBehaviour
{
    [Header("Menu Config")]
    [SerializeField] private HUDMenuConfig config;

    [Header("3D Button Build")]
    [SerializeField] private Transform buttonParent;     // put under HUDTilt
    [SerializeField] private GameObject buttonPrefab;    // must have collider + HudButtonRelay
    [SerializeField] private float radius = 0.35f;
    [SerializeField] private float arcDegrees = 130f;

    [Header("Buttons (Level 1 visuals)")]
    [Tooltip("Optional: if you have separate open/close icons, you can swap them here later.")]
    [SerializeField] private string openActionId = "hud_open";
    [SerializeField] private string closeActionId = "hud_close";

    [Header("Behavior")]
    [SerializeField] private bool startOpen = true;

    // State
    private bool isOpen;
    private readonly Stack<MenuNode> navStack = new Stack<MenuNode>();
    private readonly List<GameObject> spawned = new List<GameObject>();
    private readonly Dictionary<string, bool> toggles = new Dictionary<string, bool>();

    private void Start()
    {
        isOpen = startOpen;

        if (config == null || config.root == null)
        {
            Debug.LogError("HUDMenuController: Missing HUDMenuConfig or root. Create a HUDMenuConfig asset and assign it.");
            return;
        }

        navStack.Clear();
        navStack.Push(config.root);
        Rebuild();
    }

    public void HandleAction(string actionId)
    {
        // Level 1 open/close behavior
        if (actionId == openActionId)
        {
            isOpen = true;
            Rebuild();
            return;
        }
        if (actionId == closeActionId)
        {
            isOpen = false;
            Rebuild();
            return;
        }

        // Back
        if (actionId == "back")
        {
            if (navStack.Count > 1) navStack.Pop();
            Rebuild();
            return;
        }

        // Navigation into a submenu
        if (actionId.StartsWith("nav:"))
        {
            string nodeId = actionId.Substring("nav:".Length);
            var current = navStack.Peek();
            var child = current.children.Find(c => c.id == nodeId);
            if (child != null)
            {
                navStack.Push(child);
                Rebuild();
            }
            return;
        }

        // Example: walking toggle
        if (actionId == "walk_toggle")
        {
            bool next = !toggles.TryGetValue(actionId, out bool cur) || !cur;
            toggles[actionId] = next;
            Debug.Log($"WALKING => {(next ? "START" : "STOP")}");
            // You can also call into your locomotion system here.
            Rebuild();
            return;
        }

        // Default: log action (wire up real behaviors later)
        Debug.Log($"HUD ACTION => {actionId}");
    }

    private void Rebuild()
    {
        // Clear old buttons
        for (int i = 0; i < spawned.Count; i++)
            if (spawned[i] != null) Destroy(spawned[i]);
        spawned.Clear();

        if (buttonParent == null || buttonPrefab == null)
        {
            Debug.LogError("HUDMenuController: Missing buttonParent or buttonPrefab.");
            return;
        }

        // Always show Level 1 button
        SpawnButton(isOpen ? closeActionId : openActionId, index: 0);

        // If closed, stop here
        if (!isOpen) return;

        // If deeper than root, show Back
        if (navStack.Count > 1)
            SpawnButton("back", index: 1);

        // Current menu node is top of nav stack
        var node = navStack.Peek();
        int startIndex = (navStack.Count > 1) ? 2 : 1;

        // Spawn children of current node
        for (int i = 0; i < node.children.Count; i++)
        {
            var child = node.children[i];

            string action;
            bool hasChildren = child.children != null && child.children.Count > 0;

            if (hasChildren) action = $"nav:{child.id}";
            else action = child.actionId;

            SpawnButton(action, startIndex + i);
        }
    }

    private void SpawnButton(string actionId, int index)
    {
        // Arc layout around forward direction in local space
        float step = arcDegrees / Mathf.Max(1, (GetTotalSlotsEstimate() - 1));
        float angle = -arcDegrees * 0.5f + step * index;

        Vector3 localPos = Quaternion.Euler(0f, angle, 0f) * (Vector3.forward * radius);

        var go = Instantiate(buttonPrefab, buttonParent);
        go.name = $"Btn_{actionId}";
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;

        var relay = go.GetComponent<HudButtonRelay>();
        if (relay != null) relay.Bind(this, actionId);

        spawned.Add(go);
    }

    private int GetTotalSlotsEstimate()
    {
        // Conservative estimate to keep spacing consistent.
        // You can improve this later by calculating exact count for current menu.
        return 12;
    }
}
