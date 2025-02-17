using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Networking;

public class smokinghabitsdone : MonoBehaviour
{
    // Start is called before the first frame update
   public void setHabitsDone()
    {
        PlayerPrefs.SetInt("habitsdone", 1);
    
    }
}
