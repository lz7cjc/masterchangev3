using UnityEngine;
using UnityEngine.UI;

public class TestToggle : MonoBehaviour
{
    public Toggle testToggle;

    void Start()
    {
        if (testToggle == null)
        {
            Debug.LogError("Test Toggle is not assigned.");
        }
        else
        {
            Debug.Log("Test Toggle is assigned correctly.");
        }
    }
}
