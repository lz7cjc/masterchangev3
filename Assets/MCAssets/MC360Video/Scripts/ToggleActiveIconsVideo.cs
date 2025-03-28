using UnityEngine;

public class ToggleActiveIconsVideo : MonoBehaviour
{
    //public GameObject defaultPlayIcon;
    //public GameObject hoverPlayIcon;
    //public GameObject selectedPlayIcon;
    //public GameObject defaultPauseIcon;
    //public GameObject hoverPauseIcon;
    //public GameObject selectedPauseIcon;
    public SpriteRenderer spriteRenderer;
    public Sprite defaultSprite;
    public Sprite hoverSprite;

    private bool isPlaying = false;

    //public void PlayIcon()
    //{
    //    defaultPlayIcon.SetActive(true);
    //    hoverPlayIcon.SetActive(false);
    //    selectedPlayIcon.SetActive(false);
    //    defaultPauseIcon.SetActive(false);
    //    hoverPauseIcon.SetActive(false);
    //    selectedPauseIcon.SetActive(false);
    //}

    //public void PauseIcon()
    //{
    //    defaultPlayIcon.SetActive(false);
    //    hoverPlayIcon.SetActive(false);
    //    selectedPlayIcon.SetActive(false);
    //    defaultPauseIcon.SetActive(true);
    //    hoverPauseIcon.SetActive(false);
    //    selectedPauseIcon.SetActive(false);
    //}

    public void SelectIcon()
    {
        spriteRenderer.sprite = defaultSprite;
    }

    public void HoverIcon()
    {
        spriteRenderer.sprite = hoverSprite;
    }

    public void DefaultIcon()
    {
        spriteRenderer.sprite = defaultSprite;
    }

    public void SetPlaying(bool playing)
    {
        isPlaying = playing;
        DefaultIcon();
    }
}

