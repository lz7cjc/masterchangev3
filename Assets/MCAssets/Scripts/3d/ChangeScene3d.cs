using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;

public class changeScene3d : MonoBehaviour
{
    public bool mousehover = false;
    public bool toForms;
    public float counter = 0;
    private string Switchscenename;
    [SerializeField] private OptimizedToggleVR OptimizedToggleVR;

    private void Awake()
    {
        if (OptimizedToggleVR == null)
        {
          
            if (OptimizedToggleVR == null)
            {
                Debug.LogError("OptimizedToggleVR component not found!");
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (mousehover)
        {
            counter += Time.deltaTime;
            if (counter >= 3)
            {
                mousehover = false;
                counter = 0;

                OptimizedToggleVR.StopVR();
                SceneManager.LoadScene(Switchscenename);
            }
        }
    }

    // mouse Enter event
    public void MouseHoverChangeScene(string Scenename)
    {
        Switchscenename = Scenename;
        mousehover = true;
    }

    // mouse Exit Event
    public void MouseExit()
    {
        mousehover = false;
        counter = 0;
    }
}
