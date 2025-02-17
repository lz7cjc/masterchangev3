using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using System.Net;
using Unity;

public class showHideHUDtop : MonoBehaviour
{

    /// <summary>
    /// //display the zones sub menu on the mainvr hud////////
    /// </summary>
    /// 

    public bool mousehover = false;
   // public GameObject altImage;
    public float Counter = 0;
   // public bool turnoff;
   public GameObject zones;
    //  public GameObject hud;
    private bool turnon = true;

    public SpriteRenderer spriteRenderer;
    public Sprite newSprite;
    public Sprite oldSprite;
    void ChangeSprite(bool state)
    {
        if (state)
        {
            spriteRenderer.sprite = newSprite;
        }
        else
        {
            spriteRenderer.sprite = oldSprite;
        }
    }

    void Update()
    {
        
        if (mousehover)
        {


            Counter += Time.deltaTime;

            if (Counter >= 3)
            {
                mousehover = false;
                Counter = 0;
                if (turnon)
                {
                    // debug.log("12345 update");
                    zones.SetActive(true);
                    turnon = false;

                }
                else
                {
                    zones.SetActive(false);
                    turnon = true;


                }
            }
        }
    }

    // mouse Enter event
    public void MouseHoverChangeScene()
    {
        
        mousehover = true;
        ChangeSprite(true);

    }

    // mouse Exit Event
    public void MouseExit()
    {

        ChangeSprite(false);
         mousehover = false;
        Counter = 0;
    }

 
    
}
