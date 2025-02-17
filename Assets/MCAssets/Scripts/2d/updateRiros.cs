using System.Collections;
using UnityEngine;
using UnityEngine.Networking;



public class updateRiros : MonoBehaviour
{
    //remote
    string posturl = "https://masterchange.today/php_scripts/updateRiros.php";
    //local
    //  readonly string posturl = "http://localhost/php_scripts/updateRiros.php";

  //  public Text errorMessage;
    public int rirosValue;
    public bool spendRiros;
    public bool earnRiros;
    public bool buyRiros;
    private int rirosEarnt;
    private int rirosBought;
    private int rirosSpent;
    private int preRirosEarnt;
    private int preRirosBought;
    private int preRirosSpent;
    private int userid;
    private bool userexists;
    private int newRiros;
    private int newBalance;
    public string description;


    public void Start()
    {

        firstFunction();
    }

    public void firstFunction()
    {
        if (PlayerPrefs.HasKey("dbuserid"))
        {
            userid = PlayerPrefs.GetInt("dbuserid");
            Debug.Log("first userid from player prefs" + userid);
            userexists = true;

            //from PP; state before the change
            preRirosEarnt = PlayerPrefs.GetInt("rirosEarnt");
            preRirosBought = PlayerPrefs.GetInt("rirosBought");
            preRirosSpent = PlayerPrefs.GetInt("rirosSpent");
            StartCoroutine(riros());
        }
    }


IEnumerator riros()
        {
          WWWForm form = new WWWForm();
        form.AddField("dbuserid", userid);
        form.AddField("description", description);

        if (buyRiros)
            {
                form.AddField("rirosBought", rirosBought);
            newRiros = preRirosBought + rirosValue;
            PlayerPrefs.SetInt("rirosBought", newRiros);
            newBalance = newRiros + preRirosEarnt - preRirosSpent;
            PlayerPrefs.SetInt("rirosBalance", newBalance);
            }
            else if (earnRiros)
            {
                form.AddField("rirosEarnt", rirosEarnt);
            newRiros = preRirosEarnt + rirosValue;
            PlayerPrefs.SetInt("rirosEarnt", newRiros);
            newBalance = newRiros + preRirosBought - preRirosSpent;
            PlayerPrefs.SetInt("rirosBalance", newBalance);

        }
        else if (spendRiros)
            {
                form.AddField("rirosSpent", rirosSpent);
            newRiros = preRirosSpent + rirosValue;
            PlayerPrefs.SetInt("rirosSpent", newRiros);
            newBalance = preRirosBought + preRirosEarnt - newRiros;
            PlayerPrefs.SetInt("rirosBalance", newBalance);

        }

         


        UnityWebRequest www = UnityWebRequest.Post(posturl, form); // The file location for where my .php file is.
        yield return www.SendWebRequest();
            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
              //  errorMessage.text = www.error;

            }
            else
            {
                string userString = www.downloadHandler.text;
                 Debug.Log("from php: " + userString);
                   Debug.Log("Form Upload Complete!");
             
         
            }
         }





}