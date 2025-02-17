using UnityEngine;

public class HUDLockRotation : MonoBehaviour
{
    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void LateUpdate()
    {
        rectTransform.rotation = Quaternion.identity;
    }
}