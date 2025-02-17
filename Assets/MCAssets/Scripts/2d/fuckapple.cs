using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class fuckapple : MonoBehaviour
{
    public Text fliptext;
    // Start is called before the first frame update
    public void Start()
    {
#if UNITY_ANDROID

        fliptext.text = "Earn 10,000 Riros when you register";


#endif

#if UNITY_IOS
        fliptext.text = "Register";


#endif
    }
        // Update is called once per frame
        void Update()
    {
        
    }
}
