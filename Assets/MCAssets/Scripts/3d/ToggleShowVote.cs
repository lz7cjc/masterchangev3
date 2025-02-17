using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ToggleShowVote : MonoBehaviour
{
    public bool mousehover = false;
    public float counter = 0;

    //public GameObject filmstuff;
    //public GameObject terrain;
    //public GameObject main;
    //public GameObject training;
    //public GameObject voting;
    //public GameObject player;
    //public GameObject target;
    
    private showfilm showfilm;

    public void Start()
    {


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

                PlayerPrefs.SetString("nextscene", "tip");
               
                showfilm.tipping();

                //changing 4.2.2023 as film in new scene
                //showhide3d = FindObjectOfType<showhide3d>();
                //showhide3d.ResetScene();


                //terrain.SetActive(false);
                //    main.SetActive(false);
                //    training.SetActive(false);
                //    filmstuff.SetActive(false);
                //    voting.SetActive(true);

                //    player.transform.position = target.transform.position;
                //    player.transform.SetParent(target.transform);


            }
        }
    }

    // mouse Enter event
    public void MouseHoverChangeScene(string Scenename)
    {
        mousehover = true;
    }

    // mouse Exit Event
    public void MouseExit()
    {
       // Debug.Log("cancelling scene change");
        mousehover = false;
        counter = 0;
    }
           
}

   


