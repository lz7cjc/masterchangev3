//using System.Collections;
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;
//using UnityEngine.Networking;
//using UnityEngine.SceneManagement;

//public class filmvotes2 : MonoBehaviour
//{
//    private string posturllive = "https://masterchange.today/php_scripts/setgetriros.php";
//    private string recordtip = "https://masterchange.today/php_scripts/filmvote.php";

//    public TMP_Text errorMessage;
//    public GameObject problembutton;

//    private bool mousehover = false;
//    private float counter = 0;

//    private int userid;
//    private int rirosSpent, rirosBalance, rirosPaid;
//    private string creditURL;

//    void Start()
//    {
//        creditURL = PlayerPrefs.GetString("VideoUrl");
//        userid = PlayerPrefs.HasKey("dbuserid") ? PlayerPrefs.GetInt("dbuserid") : 0;
//        rirosBalance = PlayerPrefs.GetInt("rirosBalance");
//        rirosSpent = PlayerPrefs.GetInt("rirosSpent");

//        Debug.Log($"User ID: {userid}, Credit URL: {creditURL}");
//    }

//    void Update()
//    {
//        if (mousehover)
//        {
//            counter += Time.deltaTime;
//            if (counter >= 3)
//            {
//                mousehover = false;
//                counter = 0;

//                StartCoroutine(SubmitTipsAndLoadScene());
//            }
//        }
//    }

//    IEnumerator SubmitTipsAndLoadScene()
//    {
//        // Start the PHP requests
//        StartCoroutine(SubmitFilmVote());
//        StartCoroutine(ReconcileRiros());

//        // Load the MainVR scene asynchronously
//        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("MainVR");
//        while (!asyncLoad.isDone)
//        {
//            yield return null;
//        }
//    }

//    IEnumerator SubmitFilmVote()
//    {
//        WWWForm formTips = new WWWForm();
//        formTips.AddField("voteis", rirosPaid);
//        formTips.AddField("filmid", creditURL);
//        formTips.AddField("userid", userid);

//        UnityWebRequest www = UnityWebRequest.Post(recordtip, formTips);
//        yield return www.SendWebRequest();

//        if (www.result != UnityWebRequest.Result.Success)
//        {
//            Debug.LogError($"Film vote submission failed: {www.error}");
//        }
//        else
//        {
//            Debug.Log($"Film vote submitted successfully: {www.downloadHandler.text}");
//        }
//    }

//    IEnumerator ReconcileRiros()
//    {
//        if (!PlayerPrefs.HasKey("dbuserid")) yield break;

//        WWWForm form = new WWWForm();
//        form.AddField("userid", userid);
//        form.AddField("rirosValue", rirosPaid);
//        form.AddField("description", $"Film Tip: {creditURL}");
//        form.AddField("riroType", "Spent");

//        UnityWebRequest www1 = UnityWebRequest.Post(posturllive, form);
//        yield return www1.SendWebRequest();

//        if (www1.result != UnityWebRequest.Result.Success)
//        {
//            Debug.LogError($"Riros reconciliation failed: {www1.error}");
//        }
//        else
//        {
//            Debug.Log($"Riros reconciled successfully: {www1.downloadHandler.text}");
//        }
//    }
//}
