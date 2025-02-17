using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class adddrink : MonoBehaviour
{
        public Text beer_str;
        public Text bottle_str;
        public Text beer_nor;
        public Text bottle_nor;
        public Text wine;
        public Text shot;
        public Text liqueur;



    public void addBeerStr(bool decrease)
    {
            Debug.Log(" is: ");
   int oldbeer = System.Convert.ToInt32(beer_str.text);
        if ((decrease) && (oldbeer > 0))
        {
            beer_str.text = (oldbeer - 1).ToString();
        }
        else if (!decrease)

        {
            beer_str.text = (oldbeer + 1).ToString();
        }
    }


    public void addBottleStr(bool decrease)
    {
        int oldbottle = System.Convert.ToInt32(bottle_str.text);
         if ((decrease) && (oldbottle > 0))
        {
            Debug.Log("decrease");
            bottle_str.text = (oldbottle - 1).ToString();
        }
        else if (!decrease)

        {
            Debug.Log("increase");

            bottle_str.text = (oldbottle + 1).ToString();
        }

    }
    public void addBeerNor(bool decrease)
    {
        int oldbeer = System.Convert.ToInt32(beer_nor.text);
        if ((decrease) && (oldbeer > 0))
        {
            beer_nor.text = (oldbeer - 1).ToString();
        }
        else if (!decrease)

        {
            beer_nor.text = (oldbeer + 1).ToString();
        }
    }


    public void addBottleNor(bool decrease)
    {
        int oldbottle = System.Convert.ToInt32(bottle_nor.text);
        Debug.Log("bool is: " + decrease);
        if ((decrease) && (oldbottle > 0))
        {
            Debug.Log("decrease");
            bottle_nor.text = (oldbottle - 1).ToString();
        }
        else if (!decrease)

        {
            Debug.Log("increase");

            bottle_nor.text = (oldbottle + 1).ToString();
        }

    }

    public void addsWine(bool decrease)
    {
        int oldsWine = System.Convert.ToInt32(wine.text);
        if ((decrease) && (oldsWine > 0))
        {
            wine.text = (oldsWine - 1).ToString();
        }
        else if (!decrease)

        {
            wine.text = (oldsWine + 1).ToString();
        }
      
    }

  

    public void addShot(bool decrease)
    {
        int oldShot = System.Convert.ToInt32(shot.text);
        Debug.Log("old value: " + oldShot);
        Debug.Log("decrease " + decrease);

        if ((decrease) && (oldShot > 0))
        {
            Debug.Log("decrease function " + decrease);

            shot.text = (oldShot - 1).ToString();
        }
        else if (!decrease)

        {
            Debug.Log("decrease function " + decrease);

            shot.text = (oldShot + 1).ToString();
        }
   
    }

   
    public void addliquer(bool decrease)
    {
        int oldliqueur = System.Convert.ToInt32(liqueur.text);
        if ((decrease) && (oldliqueur > 0))
        {
            liqueur.text = (oldliqueur - 1).ToString();
        }
        else if (!decrease)

        {
            liqueur.text = (oldliqueur + 1).ToString();
        }
      
    }



}
