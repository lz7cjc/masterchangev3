using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

public class slitog : MonoBehaviour
{
    //remote
    string inserthabits = "https://masterchange.today/php_scripts/habitvaluesjson.php";
    string gethabits = "https://masterchange.today/php_scripts/gethabits.php";
    private int dbuserid;
    private string Switchscene;

    public Text errormessage;

    [SerializeField] private justSetGetRiros justSetGetRiros;

    private string json;

    private void Start()
    {
        dbuserid = PlayerPrefs.GetInt("dbuserid");
        _ = GetHabitsAsync();
    }

    private async Task GetHabitsAsync()
    {
        WWWForm form = new WWWForm();
        form.AddField("dbuserid", dbuserid);
        UnityWebRequest www = UnityWebRequest.Post(gethabits, form);
        await www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ProtocolError || www.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.LogError("Network error: " + www.error);
        }
        else
        {
            string json = www.downloadHandler.text;
            Debug.Log("Initial user habits from JSON: " + json);
            UserHabits loadedPlayerData = JsonUtility.FromJson<UserHabits>(json);

            var toggles = FindObjectsByType<Toggle>(FindObjectsSortMode.None);
            var sliders = FindObjectsByType<Slider>(FindObjectsSortMode.None);

            foreach (var slider in sliders)
            {
                int sliderID = Convert.ToInt32(slider.name);
                var habit = loadedPlayerData.data.FirstOrDefault(h => h.Habit_ID == sliderID);
                if (habit != null)
                {
                    slider.value = habit.amount;
                }
            }

            foreach (var toggle in toggles)
            {
                int toggleID = Convert.ToInt32(toggle.name);
                var habit = loadedPlayerData.data.FirstOrDefault(h => h.Habit_ID == toggleID);
                if (habit != null && habit.yesorno == 1)
                {
                    toggle.isOn = true;
                }
            }
        }
    }

    public void sethabits(string nextScene)
    {
        globalvariables.Instance.nextScene = nextScene;
        Switchscene = globalvariables.Instance.nextScene;

        var toggles = FindObjectsByType<Toggle>(FindObjectsSortMode.None);
        var sliders = FindObjectsByType<Slider>(FindObjectsSortMode.None);

        UserHabitsPut obj = new UserHabitsPut();

        foreach (var slider in sliders)
        {
            int habitID = Convert.ToInt32(slider.name);
            int habitValue = Convert.ToInt32(slider.value);
            obj.data1.Add(new habitinfoput { Habit_ID = habitID, amount = habitValue, label = "slider" });
        }

        foreach (var toggle in toggles)
        {
            int habitID = Convert.ToInt32(toggle.name);
            int habitValue = Convert.ToInt32(toggle.isOn);
            obj.data1.Add(new habitinfoput { Habit_ID = habitID, amount = habitValue, label = "toggle" });
        }

        json = JsonUtility.ToJson(obj);
        Debug.Log("json is: " + json);
        _ = PushHabitsAsync();
    }

    private async Task PushHabitsAsync()
    {
        WWWForm form = new WWWForm();
        form.AddField("user", dbuserid);
        form.AddField("cthearray", json);
        UnityWebRequest www = UnityWebRequest.Post(inserthabits, form);
        await www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ProtocolError || www.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.LogError(www.error);
            errormessage.text = $"Oops, sorry but that didn't work. Please can you email us at bugs@masterchange.today; tell us what you were trying to do and the error message that follows and if we can replicate you will receive 1000 riros: <b>{www.error}</b>";
        }
        else
        {
            string dowepay = www.downloadHandler.text;
            Debug.Log("do we pay" + dowepay);
            newInfo newInfo = JsonUtility.FromJson<newInfo>(dowepay);
            if (newInfo.reward == "1")
            {
                timeToPay();
            }
            else
            {
                SceneManager.LoadScene(Switchscene);
            }
        }
    }

    public void timeToPay()
    {
        if (justSetGetRiros != null)
        {
            justSetGetRiros.toPayOut();
        }
        else
        {
            Debug.LogError("justSetGetRiros is not assigned.");
        }
    }

    [Serializable]
    public class newInfo
    {
        public string reward;
    }

    [Serializable]
    public class UserHabits
    {
        public List<habitinfo> data;
    }

    [Serializable]
    public class habitinfo
    {
        public int Habit_ID;
        public int label;
        public int amount;
        public int yesorno;
    }

    [Serializable]
    public class habitinfoput
    {
        public int Habit_ID;
        public string label;
        public int amount;
    }

    [Serializable]
    public class UserHabitsPut
    {
        public List<habitinfoput> data1 = new List<habitinfoput>();
    }
}
