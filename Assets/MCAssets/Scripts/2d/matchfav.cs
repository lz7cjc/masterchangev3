
using System.Collections.Generic;
using UnityEngine;

using System.IO;

using System;


public class matchfav : MonoBehaviour
{
   
    //public void Start()
    //{
    //   MyFaves();
    //}


  
    public void MyFaves()
    {
       
        // read the json containing all the user's favourites
        string favs = File.ReadAllText(Application.persistentDataPath + "/favourites.json");
        bool result = string.IsNullOrEmpty(favs as string);
        //Debug.Log("result" + result);
        if (!result)
        {

     
            //Debug.Log("json from file: " + favs);
        sites loadedPlayerData = JsonUtility.FromJson<sites>(favs);

        //get all the signposts and place in signs object
        var signs = GameObject.FindGameObjectsWithTag("signpost");

           //loop through the user favourites 
        for (int i = 0; i < loadedPlayerData.data.Count; i++)
            {
            //trim the url of escape characters /\/\
          //  string myfaves = loadedPlayerData.data[i].URL.Substring(8);
            string myfaves = loadedPlayerData.data[i].URL.Trim();

            //Debug.Log("my favourite url: " + myfaves);
            //loop through all the signs
            int ol = 0;
                foreach (var sign in signs)
                {
                    ol++;
                    //Debug.Log("in loop number" + ol);
                    //assign the specific url to signUrls
                    string signUrls = sign.GetComponent<ToggleShowHideVideo>().VideoUrlLink.Trim();

                    //trim the url so can match against the url stored in JSON which has escape characters /\/\
                    // signUrls = signUrls.Substring(8);
                    //Debug.Log("signUrls is: " + signUrls);


                    //do the urls match
                    if (myfaves == signUrls)

                    {
                        //Debug.Log("Got a match: " + signUrls + " with: " + myfaves);
                        //get all the child objects so you can identify the heart to activate
                        Transform[] allSignChildren = sign.transform.GetComponentsInChildren<Transform>(includeInactive: true);
                        //for storing the matching node
                        Transform foundNode = null;

                        //check all children of sign for the correct node
                        foreach (Transform child in allSignChildren)
                        {
                            //get ready to store the found node
                            //Debug.Log("child name" + child.name);
                            if (child.name == "node_id34")
                            {
                                foundNode = child;
                                //Debug.Log("node_id34 matched" + foundNode);
                                foundNode.gameObject.SetActive(true);
                                break;
                            }


                            //check if we found it
                            //if (foundNode != null)
                            //{
                            ////Debug.Log("in final money shot: " + foundNode.name);
                            //    //enable it if we found the right node
                            //    foundNode.gameObject.SetActive(true);
                            //}
                        }
                    }
                }
            }
        }
    }

   
    [Serializable]
    public class Favourites
    {
        public string URL;
    }

    [Serializable]
    public class sites
    {
        public List<Favourites> data;
    }



}
