using UnityEngine;

[RequireComponent(typeof(CustomReticlePointer))]
public class CameraController : MonoBehaviour
{
    private CustomReticlePointer reticlePointer;

    private void Awake()
    {
        reticlePointer = GetComponent<CustomReticlePointer>();
        if (reticlePointer == null)
        {
            Debug.LogError("CustomReticlePointer component required!");
            enabled = false;
        }
    }

    public void SetMode(CustomReticlePointer.ViewMode newMode)
    {
        reticlePointer.SetMode(newMode);
    }
}