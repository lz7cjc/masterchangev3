using UnityEngine;
using System.Collections.Generic;

public class RingHUDManager : MonoBehaviour
{
    [Header("Ring Settings")]
    [SerializeField] private float primaryRingHeight = -1.56f;
    [SerializeField] private float secondaryRingHeight = -1.76f;
    [SerializeField] private float tertiaryRingHeight = -1.96f;

    [Header("Position Settings")]
    [SerializeField] private Vector3 baseOffset = new Vector3(0, 0, -3.14f);
    [SerializeField] private float itemScale = 0.00226f;

    [Header("Ring References")]
    [SerializeField] private RingGenerator primaryRing;
    [SerializeField] private RingGenerator secondaryRing;
    [SerializeField] private RingGenerator tertiaryRing;

    private Transform playerTransform;
    private Dictionary<RingLevel, List<GameObject>> menuItems;

    public enum RingLevel
    {
        Primary,
        Secondary,
        Tertiary
    }

    private void Awake()
    {
        InitializeRings();
    }

    private void Start()
    {
        InitializePlayerReference();
        InitializeMenuItems();
        UpdateAllRingPositions();
    }

    private void InitializeRings()
    {
        // Initialize dictionary
        menuItems = new Dictionary<RingLevel, List<GameObject>>();

        // Initialize dictionary for each ring level
        foreach (RingLevel level in System.Enum.GetValues(typeof(RingLevel)))
        {
            menuItems[level] = new List<GameObject>();
        }
    }

    private void InitializePlayerReference()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform != null)
        {
            transform.position = playerTransform.position + baseOffset;
        }
    }

    private void InitializeMenuItems()
    {
        // Clear existing items
        foreach (var level in menuItems.Keys)
        {
            menuItems[level].Clear();
        }

        // Collect and organize menu items by ring level
        foreach (Transform child in transform)
        {
            if (child.CompareTag("PrimaryMenuItem"))
                menuItems[RingLevel.Primary].Add(child.gameObject);
            else if (child.CompareTag("SecondaryMenuItem"))
                menuItems[RingLevel.Secondary].Add(child.gameObject);
            else if (child.CompareTag("TertiaryMenuItem"))
                menuItems[RingLevel.Tertiary].Add(child.gameObject);
        }
    }

    private void LateUpdate()
    {
        if (playerTransform != null)
        {
            transform.position = playerTransform.position + baseOffset;
        }
    }

    private void UpdateAllRingPositions()
    {
        // Check if dictionary is initialized
        if (menuItems == null)
        {
            InitializeRings();
        }

        UpdateRingItems(RingLevel.Primary, primaryRing, primaryRingHeight);
        UpdateRingItems(RingLevel.Secondary, secondaryRing, secondaryRingHeight);
        UpdateRingItems(RingLevel.Tertiary, tertiaryRing, tertiaryRingHeight);
    }

    private void UpdateRingItems(RingLevel level, RingGenerator ring, float height)
    {
        // Safety check
        if (ring == null || !menuItems.ContainsKey(level) || menuItems[level].Count == 0) return;

        float radius = ring.InnerRadius;
        int itemCount = menuItems[level].Count;

        for (int i = 0; i < itemCount; i++)
        {
            GameObject item = menuItems[level][i];
            if (item == null) continue;  // Skip if item is null

            float angle = i * (360f / itemCount);
            float rad = Mathf.Deg2Rad * angle;

            // Position on ring
            Vector3 newPos = new Vector3(
                radius * Mathf.Cos(rad),
                height,
                radius * Mathf.Sin(rad)
            );

            item.transform.localPosition = newPos;

            // Rotate to face center
            float rotationAngle = angle - 180;
            item.transform.localRotation = Quaternion.Euler(0, rotationAngle, 0);
            item.transform.localScale = new Vector3(itemScale, itemScale, itemScale);
        }
    }

    public void ShowHideRingLevel(RingLevel level, bool show)
    {
        if (menuItems == null || !menuItems.ContainsKey(level)) return;

        foreach (GameObject item in menuItems[level])
        {
            if (item != null)
                item.SetActive(show);
        }
    }

    public void AddMenuItem(GameObject item, RingLevel level)
    {
        if (menuItems == null || !menuItems.ContainsKey(level)) return;

        menuItems[level].Add(item);
        UpdateAllRingPositions();
    }

    public void RemoveMenuItem(GameObject item, RingLevel level)
    {
        if (menuItems == null || !menuItems.ContainsKey(level)) return;

        if (menuItems[level].Remove(item))
        {
            UpdateAllRingPositions();
        }
    }

    private void OnValidate()
    {
        // Only update if in play mode and dictionary is initialized
        if (Application.isPlaying && menuItems != null)
        {
            UpdateAllRingPositions();
        }
    }
}