using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class pickVideoPlayer : MonoBehaviour
{
    // Start is called before the first frame update
    private int VRorNot;
    private Scene scenename;


    public void unloadVideoFormat()
    {
        VRorNot = PlayerPrefs.GetInt("toggletovr");
        scenename  = SceneManager.GetActiveScene();
        if ((scenename.name == "videoplayer") || (scenename.name == "videoplayer2d"))
        {
            
            if (VRorNot == 1)
            {
                SceneManager.UnloadSceneAsync("videoplayer");
            }
            else if (VRorNot == 0)
            {
                SceneManager.UnloadSceneAsync("videoplayer2d");
            }
        }
    }

    public void pickVideoFormat()
    {
        VRorNot = PlayerPrefs.GetInt("toggletovr");

        if (VRorNot == 1)
            {
            // debug.log("vr videplayer");
                SceneManager.LoadScene("videoplayer");
            }
            else if (VRorNot == 0)
            {
            // debug.log("novr videplayer");
            SceneManager.LoadScene("videoplayer2d");
            }
        
    }

}
