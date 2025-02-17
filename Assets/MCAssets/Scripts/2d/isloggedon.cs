using UnityEngine;
using UnityEngine.UI;

public class isloggedon : MonoBehaviour
{
    public Text notregistered;
    public GameObject Masterchange;
    public GameObject earnriros;
    public GameObject Register;
    public GameObject Logon;


  
    // Start is called before the first frame update
   void Start()
    {
       
         if (PlayerPrefs.HasKey("dbuserid"))
        {
            Register.SetActive(false);
            Logon.SetActive(false);
            Masterchange.SetActive(true);
            earnriros.SetActive(true);

        }
        else
        { 
        notregistered.text = "Please register/logon to continue";
            Register.SetActive(true);
            Logon.SetActive(true);
            Masterchange.SetActive(false);
            earnriros.SetActive(false);


        }


    }

    
}
