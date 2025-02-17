using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
public class showPassword : MonoBehaviour
{

    public InputField passwordField;
    public Toggle show;
    public void Update()
    {
      //  Debug.Log("am i showing? " + show.isOn);
        if (show.isOn)
        {
            //Debug.Log("switching to showing");
            passwordField.contentType = InputField.ContentType.Standard;
            passwordField.ForceLabelUpdate();
        }

        else
        {
            //Debug.Log("switching to hiding");
            passwordField.contentType = InputField.ContentType.Password;
            passwordField.ForceLabelUpdate();
        }
    }
}