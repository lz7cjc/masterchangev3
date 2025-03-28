using UnityEngine;

public class TogglePlayPauseIcons : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public Sprite defaultPlaySprite;
    public Sprite hoverPlaySprite;
 //   public Sprite selectedPlaySprite;
    public Sprite defaultPauseSprite;
    public Sprite hoverPauseSprite;
  //  public Sprite selectedPauseSprite;

    private bool isPlaying = false;

    public void PlayIcon()
    {
        spriteRenderer.sprite = defaultPlaySprite;
    }

    public void PauseIcon()
    {
        spriteRenderer.sprite = defaultPauseSprite;
    }

    //public void SelectIcon()
    //{
    //    spriteRenderer.sprite = isPlaying ? selectedPauseSprite : selectedPlaySprite;
    //}

    public void HoverIcon()
    {
        spriteRenderer.sprite = isPlaying ? hoverPauseSprite : hoverPlaySprite;
    }

    public void DefaultIcon()
    {
        spriteRenderer.sprite = isPlaying ? defaultPauseSprite : defaultPlaySprite;
    }

    public void SetPlaying(bool playing)
    {
        isPlaying = playing;
        DefaultIcon();
    }
}
