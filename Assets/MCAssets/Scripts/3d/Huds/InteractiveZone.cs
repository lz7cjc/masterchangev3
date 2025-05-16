using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

// This component goes on each interactive object
public class InteractiveZone : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Zone Settings")]
    [SerializeField] private string zoneName;  // "Beaches", "Travel", etc.

    [Header("Visual Elements")]
    [SerializeField] private SpriteRenderer iconRenderer;
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Sprite hoverSprite;
    [SerializeField] private Sprite selectedSprite;

    private ZoneManager zoneManager;
    private bool isHovering = false;

    private void Start()
    {
        // Find the ZoneManager
        zoneManager = FindFirstObjectByType<ZoneManager>();
        if (zoneManager == null)
        {
            Debug.LogError("ZoneManager not found in scene!");
        }

        // Set default icon
        if (iconRenderer != null)
        {
            iconRenderer.sprite = defaultSprite;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (zoneManager == null) return;

        isHovering = true;
        SetHoverIcon();

        // Tell the ZoneManager we've started hovering
        zoneManager.StartHovering(zoneName, this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (zoneManager == null) return;

        isHovering = false;
        SetDefaultIcon();

        // Tell the ZoneManager we've stopped hovering
        zoneManager.StopHovering();
    }

    public void SetDefaultIcon()
    {
        if (iconRenderer != null)
        {
            iconRenderer.sprite = defaultSprite;
        }
    }

    public void SetHoverIcon()
    {
        if (iconRenderer != null)
        {
            iconRenderer.sprite = hoverSprite;
        }
    }

    public void SetSelectedIcon()
    {
        if (iconRenderer != null)
        {
            iconRenderer.sprite = selectedSprite;
        }
    }

    // Method to get the zone GameObject (for camera positioning)
    public GameObject GetGameObject()
    {
        return gameObject;
    }

    // Getter for the zone name
    public string GetZoneName()
    {
        return zoneName;
    }
}