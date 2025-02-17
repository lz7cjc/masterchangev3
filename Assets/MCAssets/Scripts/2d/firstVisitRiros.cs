using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

/// <summary>
/// This is a copy of the original justSetGetRiros code but adapted to take dynamic values initially for the quiz - should be a way to consolidate into a single script
/// 2903023
/// 
/// </summary>
public class firstVisitRiros : MonoBehaviour
{
    public int rirosEarnt;
    private bool firstTime;


    private void Start()
    {
        firstTime = PlayerPrefs.HasKey("ReturnVisitor");
        if (!firstTime)
        { 
        
        PlayerPrefs.SetInt("rirosEarnt", rirosEarnt);
        PlayerPrefs.SetInt("rirosBalance", rirosEarnt);
        PlayerPrefs.SetInt("ReturnVisitor", 1);
        }
    }


}
