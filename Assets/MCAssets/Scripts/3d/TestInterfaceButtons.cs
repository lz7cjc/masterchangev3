using UnityEngine;

public class TestInterfaceButtons : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
   public void LogButtonPress(string colour)
    {
        if (colour == "blue")
        {
            Debug.Log("Pressed blue Button");
        }
      else if (colour == "green")
        {
            Debug.Log("Pressed green Button");
        }
    }
}
