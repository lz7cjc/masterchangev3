using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour
{
    public string newScene;

    void Awake()
    {
        Debug.Log($"ChangeScene initialized on {gameObject.name}");
        SceneManager.LoadScene(newScene);
    }

    //public void SetNewScene(string sceneName)
    //{
    //    Debug.Log($"SetNewScene called with scene name: {sceneName}");
    //    newScene = sceneName;
    //    Debug.Log("Changescene2dv2" + newScene);
    //    SceneManager.LoadScene(newScene);
    //}

    //internal void ChangeSceneNow()
    //{
    //    Debug.Log("ChangeSceneNow called");
    //    throw new NotImplementedException();
    //}
}