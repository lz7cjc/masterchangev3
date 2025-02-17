 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Gazetemplate : MonoBehaviour
{
   // public bool loop = false;
    public bool mouseHover = false;
    private bool move = false, toggler = false;
    public float counter = 0;
      public float Delay;
    //stop buggy after x seconds
    public float DelayStop;
   // private FollowPath FollowPath;
    void FixedUpdate()
    {
        if (mouseHover)
        {
            counter += Time.deltaTime;
            if (counter < Delay && !move)
            {
                counter += Time.deltaTime;
            }
            else if (counter >= Delay && !toggler)
            {
                toggler = !toggler;
               
            
                 
            }
           else if (counter < DelayStop && move)
            {
                counter += Time.deltaTime;
            }
            else if (counter >= DelayStop && !toggler)
            {
                toggler = !toggler;
           
                
               
            }
        }
       
    }
    // mouse Enter event
    public void OnMouseEnter()
    {
        mouseHover = true;
    }
    public void OnMouseExit()
    {
        mouseHover = false;
        toggler = false;
        counter = 0;
    }
  
}
