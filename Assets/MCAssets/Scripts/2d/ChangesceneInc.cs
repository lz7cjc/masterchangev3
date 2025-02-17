using UnityEngine;
using UnityEngine.SceneManagement;


public class ChangesceneInc : MonoBehaviour
{

    //public bool mousehover = false;
    // public float counter = 0;
    //private string Switchscene;
    private int skipvrscreen;
  

    // Update is called once per frame

    public void ChangeSceneNow(string Switchscene)
    {

  //      Debug.Log("scene switch is: " + Switchscene);
      //  Debug.Log("habits done: " + PlayerPrefs.GetInt("habitsdone"));
     //   Debug.Log("returntoscene: " + PlayerPrefs.GetString("returntoscene"));
     //   Debug.Log("behaviour : " + PlayerPrefs.GetString("behaviour"));

        //then check if skipping VR instructions

        //check to see if they have done the health check if return to scene is hospital and behaviour is smoking

        if (((PlayerPrefs.GetInt("habitsdone") == 0) && (PlayerPrefs.GetString("returntoscene") == "hospital")) && (PlayerPrefs.GetString("behaviour") == "smoking"))
        {
            SceneManager.LoadScene("cancelhospital");

        }
        //do we show the vr help screen or is it switched off
        else if (Switchscene =="switchtovr")
        {
            if (PlayerPrefs.HasKey("SwitchtoVR"))
            {
                skipvrscreen = PlayerPrefs.GetInt("SwitchtoVR");
            }
            else
            {
                skipvrscreen = 1;
            }
            //checking if set to switchtovr and therefore to MasterChangeVR
                //has the user asked to skip the instructions
                //yes
                if (skipvrscreen == 0)
                {
                    SceneManager.LoadScene("everything");

                }

                else
                {
                    SceneManager.LoadScene("switchtovr");
                }
            
        }
        else
        {
            SceneManager.LoadScene(Switchscene);
        }    
      
        

    }



}