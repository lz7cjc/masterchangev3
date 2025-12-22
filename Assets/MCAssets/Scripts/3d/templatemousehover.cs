using UnityEngine;
public class templatemousehover : MonoBehaviour

{

    public bool mousehover = false;

public float Counter = 0;

public GameObject player;
public GameObject cameratarget;

public int delay = 3;

private hudCountdown hudCountdown;
Vector3 rot = new Vector3(0, 0, 1);
Vector3 rotationDirection = new Vector3();


void Update()
{

    if (mousehover)
    {
    
        hudCountdown = hudCountdown.FindFirstObjectByType<hudCountdown>();
        hudCountdown.SetCountdown(delay, Counter);
        Counter += Time.deltaTime;
        if (Counter >= delay)
        {

            mousehover = false;
            Counter = 0;
            //Do Stuff e.g move player to target    
            showandhide();
        }

    }
          

      
}

// mouse Enter event
public void MouseHoverChangeScene()
{
    
    mousehover = true;

}



// mouse Exit Event
public void MouseExit()
{
  
    mousehover = false;
    Counter = 0;
    hudCountdown.resetCountdown();
}


public void showandhide()
{
    //   // debug.log("calling showhide3d kkk");
    //    TargetObject.SetActive(true);
    player.transform.position = cameratarget.transform.position;
    //set rotation


    //TargetObject.SetActive(true);
    player.transform.SetParent(cameratarget.transform);
    player.transform.rotation = Quaternion.identity;
    //showhide3d = FindFirstObjectByType<showhide3d>();

    //showhide3d.ResetScene();


}



}
