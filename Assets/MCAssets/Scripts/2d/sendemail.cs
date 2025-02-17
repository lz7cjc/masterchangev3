using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Net.Mail;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class sendemail : MonoBehaviour
{
    public InputField emailaddress;
    public InputField username;
    private string code;
    string posturl = "https://masterchange.today/php_scripts/passwordcodereset.php";
    private string finalString;
    public Text error;
    private string json;

    // Use this for initialization

    public void Start()
    {
        emailaddress.text = "nick@beriro.co.uk";
        username.text = "goagdvrfaink1";
    }
    public void CallLogInCoroutine()
    {
        StartCoroutine(CheckCreds());

    }


    IEnumerator CheckCreds()
    {

        WWWForm form = new WWWForm();
        Debug.Log("email to send to: " + emailaddress.text);
        form.AddField("c_email", emailaddress.text);
         form.AddField("c_username", username.text);



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
            Debug.Log("json is from form" + json);
            if (json == "0")

            {
                error.text = "Your username and/or password weren't recognised. Have another go";
            }

            else if (json == "4")
            {
                error.text = "There was a problem with generating your unique code. Please email passwordresets@masterchange.today with your username and registered email address so we can resolve";

            }
            else
            {
               // emailSend();
                SceneManager.LoadScene("passwordreset");
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