using UnityEngine;

[RequireComponent(typeof(VRReticlePointer))]
public class CameraController : MonoBehaviour
{
    private VRReticlePointer reticlePointer;

    private void Awake()
    {
        reticlePointer = GetComponent<VRReticlePointer>();
        if (reticlePointer == null)
        {
            Debug.LogError("VRReticlePointer component required!");
            enabled = false;
        }
    }

    public void SetMode(VRReticlePointer.ViewMode newMode)
    {
        reticlePointer.SetMode(newMode);
    }
}