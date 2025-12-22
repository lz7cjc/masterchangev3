using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;

/// <summary>
/// UPDATED: Scene changing with GazeReticlePointer support
/// Changed togglingXR to togglingXRFilm
/// </summary>
public class changeScene3d : MonoBehaviour
{
    public bool mousehover = false;
    public bool toForms;
    public float counter = 0;
    private string Switchscenename;
    [SerializeField] private togglingXRFilm togglingXRFilm;  // CHANGED: was togglingXR

    private void Awake()
    {
        // Ensure togglingXRFilm is assigned
        if (togglingXRFilm == null)
        {
            togglingXRFilm = FindFirstObjectByType<togglingXRFilm>();  // CHANGED: was togglingXR
            if (togglingXRFilm == null)
            {
                Debug.LogError("togglingXRFilm component not found!");
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

                if (togglingXRFilm != null)
                {
                    togglingXRFilm.StopXR();
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