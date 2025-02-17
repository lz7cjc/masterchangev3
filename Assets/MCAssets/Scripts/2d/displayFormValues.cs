using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;

public class displayFormValues : MonoBehaviour
{

    public Text value;
    public Slider slider;
    // Start is called before the first frame update
public void showValue()
    {
        value.text = slider.value.ToString();
    }
}
