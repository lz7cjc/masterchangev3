using UnityEngine;
using UnityEngine.SceneManagement;


public class toMasterChange : MonoBehaviour
{
    private int SkipSwitchScreenInt;



    public void MasterChange()
    {

        SkipSwitchScreenInt = PlayerPrefs.GetInt("SwitchtoVR");

        if (SkipSwitchScreenInt == 0)
        {
            SceneManager.LoadScene("switchtoVR");
        }

        else
        {
            SceneManager.LoadScene("everything");
        }



    }



}