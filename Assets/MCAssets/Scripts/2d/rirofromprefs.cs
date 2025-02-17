using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class rirofromprefs : MonoBehaviour
{
        public TMP_Text riros;
     // Start is called before the first frame update
    void Start()
    {

;
        riros.text = "R$" + PlayerPrefs.GetInt("rirosBalance").ToString();

    }
}
