using UnityEngine;

public class ToggleActiveIcons : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Sprite hoverSprite;
    [SerializeField] private Sprite selectedSprite;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public void Start()
    {
        Debug.Log("in start of toggle active icons");
        spriteRenderer.sprite = defaultSprite;

    }
    public void SelectIcon()
    {
        Debug.Log("in SelectIcon of toggle active icons");

        spriteRenderer.sprite = selectedSprite;

    }

    // Update is called once per frame
    public void HoverIcon()
    {
        Debug.Log("in HoverIcon of toggle active icons");

        spriteRenderer.sprite = hoverSprite;


    }

    public void DefaultIcon()
    {
        Debug.Log("in DefaultIcon of toggle active icons");

        spriteRenderer.sprite = defaultSprite;

    }
}
