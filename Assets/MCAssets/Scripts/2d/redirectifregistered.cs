using UnityEngine;
using UnityEngine.SceneManagement;


public class redirectifregistered : MonoBehaviour
{
            public bool mousehover = false;
    public float counter = 0;
    public string Switchscenename;
    private bool registered;
    // Start is called before the first frame update
    void Start()
    {

        registered = PlayerPrefs.HasKey("dbuserid");
        if (registered)
        {
            PlayerPrefs.DeleteKey("nextscene");
            SceneManager.LoadScene("dashboard");

        }
    }
    

    



}