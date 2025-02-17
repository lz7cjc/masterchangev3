using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour
{
    private string newScene;

    void Start()
    {
        Debug.Log($"ChangeScene initialized on {gameObject.name}");
    }

    public void SetNewScene(string sceneName)
    {
        Debug.Log($"SetNewScene called with scene name: {sceneName}");
        newScene = sceneName;
        Debug.Log("Changescene2dv2" + newScene);
        SceneManager.LoadScene(newScene);
    }

    internal void ChangeSceneNow()
    {
        Debug.Log("ChangeSceneNow called");
        throw new NotImplementedException();
    }
}