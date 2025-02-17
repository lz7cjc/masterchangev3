using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using System.Net;
using Unity;

public class showHideHUDMove : MonoBehaviour
{

    /// <summary>
    /// //display the zones sub menu on the mainvr hud////////
    /// </summary>
    /// 

    public bool mousehover = false;
   // public GameObject altImage;
    private float Counter = 0;
    public float delay;
    private hudCountdown hudCountdown;
    // public bool turnoff;
    public GameObject speedSet;
    //  public GameObject hud;
    private bool turnon = true;
    public GameObject hudZones;

    public Sprite sprite;
    public Sprite spriteHover;
    public Sprite spriteSelect;
   

    public SpriteRenderer spriterenderer;
    public SpriteRenderer spriterenderer1;
    public SpriteRenderer spriterenderer2;
    public SpriteRenderer spriterenderer3;

    private bool showWalkMenu; 

    void Update()
    {
       
        if (mousehover)
        {

           // // debug.log("999 mousehover pre count");
            Counter += Time.deltaTime;
            hudCountdown = FindObjectOfType<hudCountdown>();
            hudCountdown.SetCountdown(delay, Counter);

            if (Counter >= delay)
            {
              //  // debug.log("turnon 2" + turnon);
              //  // debug.log("999 mousehover post count");

                mousehover = false;
                Counter = 0;
                //hudCountdown = FindObjectOfType<hudCountdown>();
                //hudCountdown.resetCountdown();
                if (turnon)
                {
               //     // debug.log("999 mousehover post count turnon");
               
                    showWalkSub();

                }
                else
                {
               //     // debug.log("999 mousehover post count turnoff");
                    hideWalkSub();
               }
            }
        }
    }

    public void showWalkSub()
    {
     //   // debug.log("999 showWalkSub showWalkSub()");
     //   // debug.log("12345 update");
        speedSet.SetActive(true);
        hudZones.SetActive(false);
        turnon = false;
        showWalkMenu = true;
        spriterenderer.sprite = spriteSelect;
        spriterenderer1.sprite = spriteSelect;
        spriterenderer2.sprite = spriteSelect;
        spriterenderer3.sprite = spriteSelect;
   }

    public void hideWalkSub()
    {
     //   // debug.log("999  hideWalkSub()");
        spriterenderer.sprite = sprite;
        spriterenderer1.sprite = sprite;
        spriterenderer2.sprite = sprite;
        spriterenderer3.sprite = sprite;
        speedSet.SetActive(false);
        turnon = true;
        showWalkMenu = false;
    }
    // mouse Enter event
    public void MouseHoverChangeScene()
    {
   //     // debug.log("999 MouseHoverChangeScene()");
        spriterenderer.sprite = spriteHover;
        spriterenderer1.sprite = spriteHover;
        spriterenderer2.sprite = spriteHover;
        spriterenderer3.sprite = spriteHover;
        mousehover = true;
        //ChangeSprite(true);

    }

    // mouse Exit Event
    public void MouseExit()
    {
        hudCountdown = FindObjectOfType<hudCountdown>();
        hudCountdown.resetCountdown();
     //   // debug.log("999 MouseExit()");
     //   // debug.log("999 status of menu, menu"+ showWalkMenu);
        if (showWalkMenu)
        {
           // // debug.log("999 showWalkMenu()");
            spriterenderer.sprite = spriteSelect;
            spriterenderer1.sprite = spriteSelect;
            spriterenderer2.sprite = spriteSelect;
            spriterenderer3.sprite = spriteSelect;
        }
        else if (!showWalkMenu)
        {
            //// debug.log("999 status of menu no menu" + showWalkMenu);
            spriterenderer.sprite = sprite;
            spriterenderer1.sprite = sprite;
            spriterenderer2.sprite = sprite;
            spriterenderer3.sprite = sprite;
        }
        //ChangeSprite(false);
        mousehover = false;
        Counter = 0;
     }    
}
