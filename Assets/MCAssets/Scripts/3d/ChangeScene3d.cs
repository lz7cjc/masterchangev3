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
    [SerializeField] private togglingXR togglingXR;

    private void Awake()
    {
        // Ensure togglingXR is assigned
        if (togglingXR == null)
        {
            togglingXR = FindFirstObjectByType<togglingXR>();
            if (togglingXR == null)
            {
                Debug.LogError("togglingXR component not found!");
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

                if (togglingXR != null)
                {
                    togglingXR.StopXR();
                }
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
