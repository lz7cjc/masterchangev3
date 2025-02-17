 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class ReticleTriggerTemplate : MonoBehaviour
{
   // public bool loop = false;
    public bool mouseHover = false;
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
            if (counter < Delay)
            {
                //put some code here whilst waiting to trigger the action 
            }
            else if (counter >= Delay)

            {
            //put some code here once triggered the action 

            }
            
        }
       
    }
    // mouse Enter event
    public void OnReticleEnter()
    {
        mouseHover = true;
    }
    public void OnReticleExit()
    {
        mouseHover = false;
        
        counter = 0;
    }
  
}
