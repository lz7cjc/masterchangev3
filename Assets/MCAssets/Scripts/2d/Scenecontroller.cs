using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Scenecontroller : MonoBehaviour
{
    public void ChangeScene (string scenename)
    {
        SceneManager.LoadScene(scenename);

    }

    
}
