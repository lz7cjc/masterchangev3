using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class firstLoad : MonoBehaviour
{
    public Toggle hideScreen;
    int HideStartScreen;
 
    // Start is called before the first frame update
    void Start()
    {
        if (!PlayerPrefs.HasKey("creditsGiven"))
        {

            PlayerPrefs.SetInt("creditsGiven", 1);
            PlayerPrefs.SetInt("rirosEarnt", 10000);
            PlayerPrefs.SetInt("rirosBalance", 10000);

        
            //PlayerPrefs.SetInt("rirosSpent", 0);
        }

        if (!PlayerPrefs.HasKey("trainingDone"))
        {
            PlayerPrefs.SetInt("trainingDone", 0);
        }

        
        redirect();

    }

   void redirect()
    { 
    //PlayerPrefs.SetInt("firstScreenHide", 0);

    HideStartScreen = PlayerPrefs.GetInt("IntroScreen");
     
        if (HideStartScreen == 1)
        {
            SceneManager.LoadScene("dashboard");
        }


    }

    
}
