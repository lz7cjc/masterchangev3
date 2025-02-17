using UnityEngine;
using UnityEngine.UI;

public class ResetButtonColor : MonoBehaviour
{
    private Button button;

    void Start()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            // Force the button to start with the Normal Color
            button.OnDeselect(null);
        }
    }
}
