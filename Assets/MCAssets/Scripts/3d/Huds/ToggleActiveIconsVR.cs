using UnityEngine;

public class ToggleActiveIconsVR : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite headsetDefaultSprite;
    [SerializeField] private Sprite headsetHoverSprite;
    [SerializeField] private Sprite noHeadsetDefaultSprite;
    [SerializeField] private Sprite noHeadsetHoverSprite;

    private bool useHeadsetSprites = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Start()
    {
        Debug.Log("in start of ToggleActiveIconsVR");
        spriteRenderer.sprite = useHeadsetSprites ? headsetDefaultSprite : noHeadsetDefaultSprite;
    }

    public void HoverIcon()
    {
        Debug.Log("in HoverIcon of ToggleActiveIconsVR");
        spriteRenderer.sprite = useHeadsetSprites ? headsetHoverSprite : noHeadsetHoverSprite;
    }

    public void DefaultIcon()
    {
        Debug.Log("in DefaultIcon of ToggleActiveIconsVR");
        spriteRenderer.sprite = useHeadsetSprites ? headsetDefaultSprite : noHeadsetDefaultSprite;
    }

    public void SetHeadsetIcon(bool isHeadset)
    {
        Debug.Log("in SetHeadsetIcon of ToggleActiveIconsVR");
        useHeadsetSprites = isHeadset;
        spriteRenderer.sprite = useHeadsetSprites ? headsetDefaultSprite : noHeadsetDefaultSprite;
    }
}
