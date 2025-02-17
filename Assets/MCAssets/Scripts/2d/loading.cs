using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class loading : MonoBehaviour
{
    [SerializeField]
    private Image _progressBar;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(LoadAsyncOperation());
    }

    IEnumerator LoadAsyncOperation()
    {
        AsyncOperation gamelevel = SceneManager.LoadSceneAsync("mainVR");
        while (gamelevel.progress < 1)
        {
            Debug.Log("progress is " + gamelevel.progress);
            _progressBar.fillAmount = gamelevel.progress;
            yield return new WaitForEndOfFrame();
            
        }     
        
  
 
    }    
}
