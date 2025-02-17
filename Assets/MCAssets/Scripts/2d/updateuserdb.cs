using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class updateuserdb : MonoBehaviour
{
    // Start is called before the first frame update
     [Tooltip("IntroScreen, SwitchtoVR, SkipLearningScreenInt, creditsgiven, returnToScene, stage, bioPaid, foodPaid, envPaid, factorsPaid, habitsPaid, CTstartpoint, delaynotification, behaviour, habitsdone")]
  public string fieldName;
 
    public bool isString;
    private int fieldInt;
    private string fieldValue;
   
    string updatePP = "https://masterchange.today/php_scripts/updateUser.php";
    private int dbuserid;

    public void callToUpdate()
    {
        dbuserid = PlayerPrefs.GetInt("dbuserid");
        StartCoroutine(upDatePP());

    }

    IEnumerator upDatePP()
    {
        WWWForm forma = new WWWForm();

        if (isString)
        {
            fieldValue = PlayerPrefs.GetString(fieldName);
      
        }
        else
        {
            fieldValue = PlayerPrefs.GetInt(fieldName).ToString();
            
           

        }
        Debug.Log("fieldname: " + fieldName + " fieldValue = " + fieldValue );
        forma.AddField("fieldName", fieldName);
        forma.AddField("fieldValue", fieldValue);
        forma.AddField("user", dbuserid);
        UnityWebRequest wwwa = UnityWebRequest.Post(updatePP, forma); // The file location for where my .php file is.
        yield return wwwa.SendWebRequest();
        if (wwwa.isNetworkError || wwwa.isHttpError)
        {

        }
        else
        {


        }
    }
}
