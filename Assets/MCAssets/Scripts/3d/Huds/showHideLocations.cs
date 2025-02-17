using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using System.Net;
using Unity;

public class showHideLocations : MonoBehaviour
{

    /// <summary>
    /// //display the zones sub menu on the mainvr hud////////
    /// </summary>
    /// 
    //private closeAllHuds closeAllHuds;
    [SerializeField] private showHideHUD showHideHUD;
    public bool mousehover = false;
   // public GameObject altImage;
    public float Counter = 0;
    // public bool turnoff;
    public GameObject locations;
    public GameObject hudMove;
    //  public GameObject hud;
    private bool turnon = true;

    public SpriteRenderer spriteRendererLocations;
    public Sprite defaultLocations;
    public Sprite hoverLocations;
    public Sprite selectedLocations;


    public float delay = 3;
    private hudCountdown hudCountdown;
    
        
    void Update()
    {
        //string behaviour = PlayerPrefs.GetString("behaviour");
        //if ((behaviour == "tips") || (behaviour == "film"))
        //    {
        //    hud.SetActive(false);

        //}
    //    // debug.log("12345" + turnon);
        if (mousehover)
        {


            Counter += Time.deltaTime;
            hudCountdown = hudCountdown.FindFirstObjectByType<hudCountdown>();
            hudCountdown.SetCountdown(delay, Counter);
            if (Counter >= delay)
            {

                mousehover = false;
                Counter = 0;
               

                if (turnon)
                {
                    Debug.Log("show locations x");
                    locations.SetActive(true);
                    hudMove.SetActive(false);
                    turnon = false;
                    spriteRendererLocations.sprite = selectedLocations;
                    hudCountdown.resetCountdown();
                }
                else if (!turnon)
                {
                    locations.SetActive(false);
                    hudMove.SetActive(false);
                    
                    turnon = true;
                    spriteRendererLocations.sprite = defaultLocations;
                    hudCountdown.resetCountdown();
                    showHideHUD = FindFirstObjectByType<showHideHUD>();
                    showHideHUD.directClick();

                }
            }
        }
    }

    // mouse Enter event
    public void MouseHoverChangeScene()
    {
        spriteRendererLocations.sprite = hoverLocations;
        mousehover = true;
 

    }

    // mouse Exit Event
    public void MouseExit()
    {
        spriteRendererLocations.sprite = defaultLocations;
        hudCountdown.resetCountdown();
         mousehover = false;
        Counter = 0;
    }

 
    
}
