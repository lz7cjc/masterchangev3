using UnityEngine;
using TMPro;

public class floorceilingmove : MonoBehaviour
{
    public bool loop = false;
    public bool mouseHover;
    private bool move = false;
    private bool toggler = false;
    private float Counter1 = 0;
    private float speed;
    private float speedSet;
    private string status;
    public float Delay;
    public float DelayStop;
    public Rigidbody player;
    private float deltaSpeed;
    private float displaySpeed;

    private hudCountdown hudCountdown;

    public TMP_Text speedvalue;
    public TMP_Text speedvalue1;
    public TMP_Text speedvalue2;
    public TMP_Text speedvalue3;


    public GameObject walkStopIcon1;
    public GameObject walkStartIcon1;
    public GameObject walkStopIcon2;
    public GameObject walkStartIcon2;
    public GameObject walkStopIcon3;
    public GameObject walkStartIcon3;
    public GameObject walkStopIcon4;
    public GameObject walkStartIcon4;

    private bool changeSpeed;
    private bool startWalking;

    private showHideHUDMove showHideHUDMove;

    private bool stopNowTrigger;

    public void FixedUpdate()
    {
        speedvalue.text = speedSet.ToString();
        speedvalue1.text = speedSet.ToString();
        speedvalue2.text = speedSet.ToString();
        speedvalue3.text = speedSet.ToString();

        if (mouseHover)
        {
            Counter1 += Time.deltaTime;
          //  // debug.log("Counter1: " + Counter1);
          //  // debug.log("MouseHover active, speed: " + speedSet + ", moving: " + move);
            hudCountdown = FindFirstObjectByType<hudCountdown>();
            hudCountdown.SetCountdown(Delay, Counter1);

            if (Counter1 >= Delay)
            {
                hudCountdown = FindFirstObjectByType<hudCountdown>();
                hudCountdown.resetCountdown();
                mouseHover = false;
                Counter1 = 0;

                if (stopNowTrigger)
                {
                    // // debug.log("StopNowTrigger activated");
                    toggler = !toggler;
                    stopTheCamera();
                }
                else if (!toggler && changeSpeed)
                {
                    // // debug.log("Changing speed");
                    toggler = !toggler;
                    move = !move;
                    speedSet = speedSet + deltaSpeed;
                }
                else if (!toggler && startWalking)
                {
                    // // debug.log("Starting walking");
                    toggler = !toggler;
                    move = !move;
                    speedSet = speed;
                }
                else if (move && !changeSpeed)
                {
                    // // debug.log("Stopping due to move and no changeSpeed");
                }
                else if (!toggler && !changeSpeed)
                {
                    toggler = !toggler;
                    move = !move;
                    speedSet = 0;
                    PlayerPrefs.SetInt("walkspeed", (int)speedSet);
                }
            }
        }

        if (speedSet > 0)
        {
            LetsGo();
        }
        else if (speedSet == 0)
        {
            stopTheCamera();
        }
    }

    public void OnMouseEnterStartWalk(float speed1)
    {
        mouseHover = true;
        changeSpeed = false;
        startWalking = true;
        speed = speed1;
        // // debug.log("OnMouseEnterStartWalk: speed " + speed1);
    }

    public void OnMouseEnterChangeSpeed(float deltaSpeed1)
    {
        mouseHover = true;
        changeSpeed = true;
        startWalking = false;
        deltaSpeed = deltaSpeed1;
        // // debug.log("OnMouseEnterChangeSpeed: deltaSpeed " + deltaSpeed1);
    }

    public void OnMouseEnterStop()
    {
        mouseHover = true;
        stopNowTrigger = true;
        changeSpeed = false;
        startWalking = false;
        // // debug.log("OnMouseEnterStop");
        hudCountdown = FindFirstObjectByType<hudCountdown>();
        hudCountdown.resetCountdown();
    }

    public void OnMouseExit()
    {
        mouseHover = false;
        toggler = false;
        Counter1 = 0;
        stopNowTrigger = false;
        changeSpeed = false;
        startWalking = false;
    //    // debug.log("OnMouseExit");
        hudCountdown = FindFirstObjectByType<hudCountdown>();
        hudCountdown.resetCountdown();
    }

    public void LetsGo()
    {
        player.MovePosition(transform.position + Camera.main.transform.forward * speedSet * Time.deltaTime);

        walkStartIcon1.SetActive(false);
        walkStopIcon1.SetActive(true);
        walkStartIcon2.SetActive(false);
        walkStopIcon2.SetActive(true);
        walkStartIcon3.SetActive(false);
        walkStopIcon3.SetActive(true);
        walkStartIcon4.SetActive(false);
        walkStopIcon4.SetActive(true);

        //     // debug.log("LetsGo: Moving with speed " + speedSet);
    }

    public void stopTheCamera()
    {
        speedSet = 0;
        player.MovePosition(transform.position + Camera.main.transform.forward * 0 * Time.deltaTime);
        toggler = false;

        walkStartIcon1.SetActive(true);
        walkStopIcon1.SetActive(false);
        walkStartIcon2.SetActive(true);
        walkStopIcon2.SetActive(false);
        walkStartIcon3.SetActive(true);
        walkStopIcon3.SetActive(false);
        walkStartIcon4.SetActive(true);
        walkStopIcon4.SetActive(false);
        // // debug.log("stopTheCamera: Stopped moving");
    }
}
