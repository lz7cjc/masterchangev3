using UnityEngine;

/// <summary>
/// Handles icon sprite switching for VR/360 mode toggle button
/// - Shows headset sprites when in 360 mode (to indicate "switch TO VR")
/// - Shows no-headset sprites when in VR mode (to indicate "switch TO 360")
/// </summary>
public class ToggleActiveIconsVR : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite headsetDefaultSprite;
    [SerializeField] private Sprite headsetHoverSprite;
    [SerializeField] private Sprite noHeadsetDefaultSprite;
    [SerializeField] private Sprite noHeadsetHoverSprite;

    private bool useHeadsetSprites = false;

    /// <summary>
    /// Initialize sprite based on current VR mode from PlayerPrefs
    /// </summary>
    public void Start()
    {
        Debug.Log("in start of ToggleActiveIconsVR");

        // Check current VR mode and set sprites accordingly
        // If in VR mode (toggleToVR = 1), show no-headset sprites
        // If in 360 mode (toggleToVR = 0), show headset sprites
        bool isCurrentlyVR = PlayerPrefs.GetInt("toggleToVR", 0) == 1;
        useHeadsetSprites = !isCurrentlyVR; // Inverse logic: show opposite of current mode

        // Set initial sprite
        spriteRenderer.sprite = useHeadsetSprites ? headsetDefaultSprite : noHeadsetDefaultSprite;

        Debug.Log($"[ToggleActiveIconsVR] Current mode: {(isCurrentlyVR ? "VR" : "360")}, showing {(useHeadsetSprites ? "headset" : "no-headset")} sprites");
    }

    /// <summary>
    /// Show hover sprite (called by Event Trigger or switchformatreload)
    /// </summary>
    public void HoverIcon()
    {
        Debug.Log("in HoverIcon of ToggleActiveIconsVR");
        spriteRenderer.sprite = useHeadsetSprites ? headsetHoverSprite : noHeadsetHoverSprite;
    }

    /// <summary>
    /// Show default sprite (called when hover exits)
    /// </summary>
    public void DefaultIcon()
    {
        Debug.Log("in DefaultIcon of ToggleActiveIconsVR");
        spriteRenderer.sprite = useHeadsetSprites ? headsetDefaultSprite : noHeadsetDefaultSprite;
    }

    /// <summary>
    /// Update sprite set based on whether we're in VR mode
    /// Called by switchformatreload after mode switch
    /// </summary>
    /// <param name="isInVRMode">True if currently in VR mode, False if in 360 mode</param>
    public void SetHeadsetIcon(bool isInVRMode)
    {
        Debug.Log($"in SetHeadsetIcon of ToggleActiveIconsVR - isInVRMode: {isInVRMode}");

        // Inverse logic: if IN VR, show no-headset sprites (to indicate switch TO 360)
        // If IN 360, show headset sprites (to indicate switch TO VR)
        useHeadsetSprites = !isInVRMode;

        // Update to default sprite of the new set
        spriteRenderer.sprite = useHeadsetSprites ? headsetDefaultSprite : noHeadsetDefaultSprite;
    }

    /// <summary>
    /// Legacy method kept for compatibility
    /// </summary>
    public void ToggleIcons(bool isVRMode)
    {
        Debug.Log($"Toggling icons for VR mode: {isVRMode}");
        SetHeadsetIcon(isVRMode);
    }
}