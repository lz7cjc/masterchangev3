using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// This is a singleton class to hold variables that will be used globally in this application
/// </summary>
public class globalvariables : MonoBehaviour
{
    /// This region Singleton Preparation is where the singleton
    /// is created and managed.
    /// 
    /// Don't worry too much on understanding the concepts of how 
    /// variables Instance and m_instance works right now. 
    /// Concentrate on the functions examples outside the Singleton Prataration Region
    #region Singleton Preparation

    /// <summary>
    /// As a singleton variable a single static instance will hold its data
    /// </summary>
    private static globalvariables m_instance;

    /// <summary>
    /// This is the public reference to the static variable above
    /// Other classes will have access only to this public reference
    /// </summary>
    public static globalvariables Instance
    {
        get
        {
            // First, let's try to return the content of m_instance, if it is not null. 
            // If not null we'll return it to the class calling it.
            if (m_instance == null)
            {
                m_instance = FindObjectOfType<globalvariables>(); // Here m_instance is null. So we will try to find it somewhere

                if (m_instance == null) // if we could not find it in the line above, this condition will trigger
                {
                    // Here we did not find it anywhere, so we'll create a new one
                    GameObject go = new GameObject("Global Variables");
                    m_instance = go.AddComponent<globalvariables>();
                }
                DontDestroyOnLoad(m_instance);
            }
            return m_instance;
        }
    }

    /// <summary>
    /// During a scene awake, we check for other instances of this singleton class
    /// We don't want more than one instance active, so we search for additional 
    /// classes of this singleton and destroy them
    /// </summary>
    void Awake()
    {
        globalvariables[] findOther = FindObjectsOfType<globalvariables>();
        for (int i = 0; i < findOther.Length; i++)
        {

            if (findOther[i].gameObject != this.gameObject)
                Destroy(this.gameObject);
        }
        DontDestroyOnLoad(this);
    }
    #endregion

    /*
     ******************************************************************************************
     * The section below is the one you should focus to call and set values for your variables
     * 
     * Example 1) -> We will create a string variable that will hold a very important data that
     *               will be read and modified by many classes of our code, several times.
     *               To access this variable we'll be using a getter/setter strategy
     *               
     *               
     *               How to read the value of the test variable to the variable x
     *               From another class, use:
     *               string x = globalvariable.Instance.GetString1();
     *               
     *               
     *               How to set the value "OK" to the variable:
     *               From another class, use:
     *               globalvariable.Instance.SetString1("OK");
     ******************************************************************************************
     */

    /// <summary>
    /// This is a test string variable simulating a value that you could use in different places of your code.
    /// To get the value of this variable, see GetTestString1 function
    /// To set a new value for this variable, see SetTestString1 function
    /// </summary>
    private string m_testString1 = "TEST 123";

    /// <summary>
    /// This function only returns the value for the Test String variable
    /// </summary>
    /// <returns></returns>
    public string GetTestString1()
    {
        return m_testString1;
    }

    ///// <summary>
    ///// This is a test string variable simulating a value that you could use in different places of your code.
    ///// To get the value of this variable, see GetTestString1 function
    ///// To set a new value for this variable, see SetTestString1 function
    ///// </summary>
    //private ArrayList urlArray;

    ///// <summary>
    ///// This function only returns the value for the Test String variable
    ///// </summary>
    ///// <returns></returns>
    //public ArrayList GetURLs()
    //{
    //    return urlArray;
    //}

    /// <summary>
    /// This function only sets a new value for the Test String variable
    /// </summary>
    /// <param name="p_value">The new variable value</param>
    public void SetTestString1(string p_value)
    {
        m_testString1 = p_value;
    }

    /// <summary>
    /// set the riros value
    /// </summary>
    //
    private int g_RirosSet =232323 ;

    /// <summary>
    /// This function only returns the value for the Test String variable
    /// </summary>
    /// <returns></returns>
    public int getRiros()
    {
        return g_RirosSet;
    }

     /// <summary>
    /// Set the error count for VR toggle problem
    /// </summary>
    /// <returns></returns>   
    /// 
   private int g_VRToggleCount;


    public int getVRToggleCount()
    {
        return g_VRToggleCount;
    }

   
    /// <summary>
    /// This function only sets a new value for the Test String variable
    /// </summary>
    /// <param name="p_value">The new variable value</param>
    public void SetRiros(int p_value)
    {
        g_RirosSet = p_value;
    }


    /*
     ******************************************************************************************
     * 
     * Example 2) -> We will create a string variable that will hold a very important data that
     *               will be read and modified by many classes of our code, several times.
     *               This works exactly the same as above, just another way of writing this code
     *               
     *               
     *               How to read the value of the test variable to the variable x
     *               From another class, use:
     *               string x = globalvariable.Instance.TestString2;
     *               
     *               
     *               How to set the value "OK" to the variable
     *               From another class, use:
     *               globalvariable.Instance.TestString2 = "OK";
     ******************************************************************************************
     */

    /// <summary>
    /// This is a test string variable simulating a value that you could use in different places of your code.
    /// To get the value of this variable, see the TestString2 public accessor
    /// To set a new value for this variable, see TestString2 public accessor
    /// </summary>
    private string m_testString2 = "TEST 456";

    /// <summary>
    /// This public accessor works the same as the functions on the example 1. 
    /// Just another way of writting this code
    /// </summary>
    public string TestString2
    {
        get
        {
            return m_testString2;
        }
        set
        {
            m_testString2 = value;
        }
    }


    private int g_VRMessage;


    public int f_VRmessage
    {
        get
        {
            return g_VRMessage;
        }
        set
        {
            g_VRMessage = value;
        }
    }





    /// <summary>
    /// next scene variableor
    /// </summary>
    private string m_nextscene;

    /// <summary>
    /// This public accessor works the same as the functions on the example 1. 
    /// Just another way of writting this code
    /// </summary>
    public string nextScene
    {
        get
        {
            return m_nextscene;
        }
        set
        {
            m_nextscene = value;
        }
    }















    /*
    ******************************************************************************************
    * 
    * Example 3) -> We will create a more complex data structure that will be used by our code
    *               to hold the current user data
    *               
    *               
    *               How to read the value of the variable:
    *               From another class, use:
    *               UserExampleData x = globalvariable.Instance.GetUserData();
    *               
    *               
    *               How to set the value to the variable:
    *               From another class, use:
    *               globalvariable.Instance.SetUserData("John Doe", 50, true, GenderData.Male);
    *               
    *               
    *               We have a 3rd example function to user data created to return a single value 
    *               of the Userdata class. It works the same as the other getter functions
    *               From another class, use:
    *               int age = globalvariable.Instance.GetUserAge();
    *               
    *               
    *               We have a last example function to that will work as a helper function
    *               Sometimes, we do have to get a string representation of a data class
    *               to upload it to a server, for instance. 
    *               This example shows that kind of usage
    *               From another class, use:
    *               string x = globalvariable.Instance.GetParsedUserData();
    ******************************************************************************************
    */

    //errors from original file but don't think i need this anyway


    //private UserExampleData m_userData = new UserExampleData();

    ///// <summary>
    ///// This function only returns the value for the User data variable
    ///// </summary>
    ///// <returns></returns>
    //public UserExampleData GetUserData()
    //{
    //    return m_userData;
    //}

    ///// <summary>
    ///// This function validates the input data and sets a new value for the User data variable
    ///// </summary>
    ///// <param name="p_name">the name of the user. Max of 50 chars long</param>
    ///// <param name="p_age">the age of the user. Has to be more than 0 and less than 100</param>
    ///// <param name="p_smoker">either true if smoker or false if not smoker</param>
    ///// <param name="p_gender">either male of female</param>
    //public void SetUserData(string p_name, int p_age, bool p_smoker, GenderData p_gender)
    //{
    //    // The advantage of using a function to set values to our variables is that
    //    // we can sanitize the data to prevent values that could break our values.

    //    if (p_name.Length > 50) // This example shows that we do not want a name that has more than 50 chars
    //    {
    //        p_name = p_name.Substring(0, 50);
    //    }
    //    if (p_name.Length < 0) // This example shows that we do not want an empty string (ex.: ""). If this occurs we will save "EMPTY" instead 
    //    {
    //        p_name = "EMPTY";
    //    }

    //    if (p_age > 99) // This example shows that we do not want any age that is above 99
    //    {
    //        p_age = 99;
    //    }
    //    if (p_age < 0) // This example shows that we do not want any age that is less than 0
    //    {
    //        p_age = 0;
    //    }


    //    // After sanitizing our data, we save them to the user data variable
    //    m_userData.name = p_name;
    //    m_userData.age = p_age;
    //    m_userData.smoker = p_smoker;
    //    m_userData.gender = p_gender;
    //}


    ///// <summary>
    ///// This function returns a single property of the User data.
    ///// We'll use the age property as an example
    ///// </summary>
    ///// <returns></returns>
    //public int GetUserAge()
    //{
    //    return m_userData.age;
    //}

    ///// <summary>
    ///// Gets the content of User data class as a parsed string. 
    ///// Useful for server upload usage or to use it as a string representation
    ///// </summary>
    ///// <returns></returns>
    //public string GetParsedUserData()
    //{
    //    return JsonUtility.ToJson(m_userData);
    //}
}
