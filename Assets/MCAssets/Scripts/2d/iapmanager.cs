using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Networking;
using UnityEngine.UI;
public class iapmanager : MonoBehaviour
{
    private string onepound = "com.beriro.masterchange.pound1";
    private string fivepound = "com.beriro.masterchange.pound5";
    private string tenpound = "com.beriro.masterchange.pound10";
    private string twentypound = "com.beriro.masterchange.pound20";
    private int rirosValue;
    string posturl = "https://masterchange.today/php_scripts/setgetriros.php";
    public Text errormessage;

    // Start is called before the first frame update
    public void OnPurchaseComplete(Product product)
    {
        if (product.definition.id == onepound)
        {
            Debug.Log("one pound purchase");
            rirosValue = 10000;
        }
        if (product.definition.id == fivepound)
        {
            Debug.Log("five pound purchase");
            rirosValue = 70000;
        }
        if (product.definition.id == tenpound)
        {
            Debug.Log("ten pound purchase");
            rirosValue = 150000;
        }
        if (product.definition.id == twentypound)
        {
            Debug.Log("twenty pound purchase");
            rirosValue = 400000;
        }
        errormessage.text = "Thank you for supporting MasterChange. Your payment has been processed and your Riro account has been updated";

        StartCoroutine(SetRirosDB());
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        Debug.Log(product.definition.id + " failed because " + failureReason);
        errormessage.text =  "Sorry, something went wrong with your purchase. You haven't been charged so please try again. If the problem continues please contact us at payments@masterchange.today including the error code: " + failureReason;

    }

    private IEnumerator SetRirosDB()
    {
            WWWForm form = new WWWForm();
        form.AddField("riroType", "Bought");
        form.AddField("rirosValue", rirosValue);
        form.AddField("description", "Bought");
          form.AddField("userid", PlayerPrefs.GetInt("dbuserid"));


        UnityWebRequest www = UnityWebRequest.Post(posturl, form); // The file location for where my .php file is.
        yield return www.SendWebRequest();
        if (www.isNetworkError || www.isHttpError)
        {
            //        Debug.Log(www.error);
            // errormessage.text = "We hit a problem: (" + www.error + "). Please send the receipt of your payment to purchases@masterchange.today and we will sort it out";

        }
        else
        {
            string json = www.downloadHandler.text;
            //      Debug.Log("returning riros srting: " + json);
            riros riros = JsonUtility.FromJson<riros>(json);
            int rirosEarntOut = riros.Earnt;
            int rirosBoughtOut = riros.Bought;
            int rirosSpentOut = riros.Spent;
            Debug.LogWarning("riros earnt" + rirosEarntOut);
            Debug.LogWarning("riros rirosBought" + rirosBoughtOut);
            Debug.LogWarning("riros rirosSpent" + rirosSpentOut);

            PlayerPrefs.SetInt("rirosEarnt", rirosEarntOut);
            PlayerPrefs.SetInt("rirosBought", rirosBoughtOut);
            PlayerPrefs.SetInt("rirosSpent", rirosSpentOut);
            PlayerPrefs.SetInt("rirosBalance", rirosBoughtOut + rirosEarntOut - rirosSpentOut);

         
          
        }
    }
    private class riros
    {
        public int Bought;
        public int Spent;
        public int Earnt;


    }
}
