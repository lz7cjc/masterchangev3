using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MoveCameraNoToggleIcons : MonoBehaviour
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
    public bool toggleIcons;
   
    public void Start()
    {
        Debug.Log("in start of toggle movecamera");
        hudCountdown = FindFirstObjectByType<hudCountdown>();
        // No need for conditional declaration of variables
    }

    void Update()
    {
        if (mousehover)
        {
            // Conditionally call HoverIcon if toggleIcons is true
         
            
            hudCountdown.SetCountdown(delay, Counter);
            Counter += Time.deltaTime;

            if (Counter >= delay)
            {
                Debug.Log("in select of toggle movecamera");

              
                
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

        // Conditionally call DefaultIcon if toggleIcons is true
        
        
        mousehover = false;
        Counter = 0;
        hudCountdown.resetCountdown();
    }

    private void showandhide()
    {
        Counter = 0;
        player.useGravity = gravity;

        closeAllHuds.CloseTheHuds();

        // Set the player as a child of the cameraTarget
        player.transform.SetParent(cameraTarget.transform);

        // Reset the player's position to (0, 0, 0) relative to the cameraTarget
        player.transform.localPosition = Vector3.zero;

        PlayerPrefs.SetString("lastknownzone", cameraTarget.name);
    }
}
