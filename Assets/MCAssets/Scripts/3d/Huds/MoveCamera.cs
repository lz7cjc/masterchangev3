using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MoveCamera : MonoBehaviour
{
    public bool mousehover = false;
    public float Counter = 0;
    private hudCountdown hudCountdown;
    public int delay = 3;
    public Rigidbody player;
    private GameObject cameraTarget;
    public bool isTitle = false;
    public TMP_Text TMP_title;
    public bool gravity = true;
    [SerializeField] private closeAllHuds closeAllHuds;
     [SerializeField] private ToggleActiveIcons toggleActiveIcons;

    public void Start()
    {
        Debug.Log("in start of toggle movecamera");
        hudCountdown = FindFirstObjectByType<hudCountdown>();
      
    }
    void Update()
    {
        if (mousehover)
        {
            toggleActiveIcons.HoverIcon();
            hudCountdown.SetCountdown(delay, Counter);
            Counter += Time.deltaTime;

            if (Counter >= delay)
            {
                Debug.Log("in select of toggle movecamera");

             //   toggleActiveIcons.SelectIcon();
                mousehover = false;
                Counter = 0;
                hudCountdown.resetCountdown();
                showandhide();
            }
        }
    }

    public void MouseHoverChangeScene(GameObject _cameraTarget)
    {
        Debug.Log("in hover of toggle movecamera");

     
        if (isTitle)
        {
            TMP_title.color = Color.white;
        }
        mousehover = true;
        cameraTarget = _cameraTarget;
    }

    public void MouseExit()
    {
        Debug.Log("in default of toggle movecamera");

        toggleActiveIcons.DefaultIcon();
        mousehover = false;
        Counter = 0;
       
        hudCountdown.resetCountdown();
    }

    private void showandhide()
    {
        toggleActiveIcons.SelectIcon();
        Counter = 0;
        player.useGravity = gravity;
        closeAllHuds = FindFirstObjectByType<closeAllHuds>();
        closeAllHuds.CloseTheHuds();

        player.transform.position = cameraTarget.transform.position;
        player.transform.SetParent(cameraTarget.transform);

        PlayerPrefs.SetString("lastknownzone", cameraTarget.name);
    }
}