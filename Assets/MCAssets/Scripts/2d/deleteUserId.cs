using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class deleteUserId : MonoBehaviour
{
    public GameObject logoffbtn;
    public GameObject logonbtn;
    // Start is called before the first frame update
   public void logoff()

    {
        PlayerPrefs.DeleteAll();
        SceneManager.LoadScene("dashboard");
       // logoffbtn.SetActive(false);
      //  logonbtn.SetActive(true);

    }
}
