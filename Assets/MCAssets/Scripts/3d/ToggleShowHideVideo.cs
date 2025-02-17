using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class ToggleShowHideVideo : MonoBehaviour
{
    public bool mouseHover = false;
    public float counter = 0;
   // public string Switchscenename;
    public string VideoUrlLink;
    private int riroAmount;
    
    public string returntoscene;
    public string nextscene;
    public int returnstage;
    public string behaviour;
    public TMP_Text TMP_title;
    public bool hasText = true;
    private GameObject cameraTarget;
    [SerializeField] private StartUp StartUp;
    private riroStopGo riroStopGo;
    private Rigidbody player;
    private floorceilingmove floorceilingmove;
    public void Start()
    {

        riroAmount = PlayerPrefs.GetInt("rirosBalance");
     //   Debug.Log("riro balance" + riroAmount);
    }
            

    // Update is called once per frame
    void Update()
    {
        if (mouseHover)
        {
            //Debug.Log("xxx setting mousehover");
            counter += Time.deltaTime;  
           
                floorceilingmove.stopTheCamera();

            if (counter >= 3)
            {
               // Debug.Log("ppp togglescript what is behaviour " + behaviour);
              //  Debug.Log("ppp togglescript what is nextscene " + nextscene);
              //  Debug.Log("ppp togglescript what is returntoscene " + returntoscene);


             //   Debug.Log("xxx setting counted");
                mouseHover = false;
                counter = 0;
                PlayerPrefs.SetString("returntoscene", returntoscene);
                PlayerPrefs.SetString("behaviour", behaviour);
                PlayerPrefs.SetInt("stage", returnstage);
                PlayerPrefs.SetString("nextscene", nextscene);
              
                if (riroAmount >= 50)
                {
                    PlayerPrefs.DeleteKey("stopFilm");
            //        Debug.Log("eeeee");
                    PlayerPrefs.SetString("VideoUrl", VideoUrlLink);
           //         print("---->>>" + PlayerPrefs.GetString("VideoUrl"));
                                  
              //      player.useGravity = false;
                }

                else
                {
             //       Debug.Log("111eeeee");
                    PlayerPrefs.SetInt("stopFilm", 0);
                   
                    riroStopGo.doNotPass(0);
                    
                //    player.MovePosition(cameraTarget.transform.position);
                //    player.transform.SetParent(cameraTarget.transform);

                }
                SceneManager.LoadScene("videoplayer");
                //showhide3d = FindObjectOfType<showhide3d>();
                //showhide3d.ResetScene();
            }

        }
        
    }

    // mouse Enter event
    public void MouseHoverChangeScene()
    {
        if (hasText)
        {
            TMP_title.color = Color.green;
        }
        mouseHover = true;
    }

    // mouse Exit Event
    public void MouseExit()
    {
        if (hasText)
        {
            TMP_title.color = Color.white;
        }
      //  Debug.Log("cancelling scene change");
        mouseHover = false;
        counter = 0;
    }
           
}

   


