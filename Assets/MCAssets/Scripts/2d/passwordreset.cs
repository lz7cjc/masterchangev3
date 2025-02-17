using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Net.Mail;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class passwordreset : MonoBehaviour
{
    public InputField code;
    public InputField password;
    public InputField username;
     string posturl = "https://masterchange.today/php_scripts/passwordresetsubmit.php";
     public Text error;
    private string json;

    // Use this for initialization

    public void CallUpdateCoRoutine()
    {
        StartCoroutine(NewPassword());

    }


    IEnumerator NewPassword()
    {

        WWWForm form = new WWWForm();
         form.AddField("c_code", code.text);
        form.AddField("c_username", username.text);
        form.AddField("c_password", password.text);
           


    UnityWebRequest www = UnityWebRequest.Post(posturl, form); // The file location for where my .php file is.
        yield return www.SendWebRequest();
        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
        }
        else
        {
            Debug.Log("Form Upload Complete!");

            json = www.downloadHandler.text;
            Debug.Log("after password change should get dbuserid:" + json);
            Debug.Log("json is from form" + json);
            if (json == "0")

            {
                error.text = "Please try again. The code is case sensitive so please make sure you enter correctly. If you continue to experience problems, you can email us at passwordreset@masterchange.today for a manual reset.";
                error.color = Color.red;
            }

            else
            {
                // emailSend();
                PlayerPrefs.SetInt("dbuserid", Convert.ToInt32(json));
                SceneManager.LoadScene("passwordresetcomplete");
            }

        }
    }

    //void emailSend()
    //{


    //    MailMessage mail = new MailMessage();
    //    SmtpClient SmtpServer = new SmtpClient("smtp.123-reg.co.uk");
    //    mail.From = new MailAddress("passwordreset@masterchange.today");
    //    mail.To.Add("nick@beriro.co.uk");
    //    mail.Subject = "Code to reset your MasterChange account";
    //    mail.Body = "Thanks for getting in touch. You requested a reset code as you have forgotten your password. Your code is: " + json;

    //    //System.Net.Mail.Attachment attachment;
    //    //attachment = new System.Net.Mail.Attachment("c:/textfile.txt");
    //    //mail.Attachments.Add(attachment);

    //    SmtpServer.Port = 25;
    //    SmtpServer.Credentials = new System.Net.NetworkCredential("nick@masterchange.today", "%Tz|&.jH7H}L");
    //    SmtpServer.EnableSsl = false;

    //    SmtpServer.Send(mail);
    //    SceneManager.LoadScene("everything");
    //    CallLogInCoroutine();

    //}

}