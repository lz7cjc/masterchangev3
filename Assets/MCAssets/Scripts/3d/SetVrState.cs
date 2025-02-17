using UnityEngine;

public class SetVrState : MonoBehaviour
{
    [SerializeField] private Sprite spritePickedVR;
    [SerializeField] private Sprite spritePickedNoVR;
    [SerializeField] public SpriteRenderer spriterendererVR;
    [SerializeField] private GameObject mainCamera2D;
    [SerializeField] private GameObject mainCameraVR;
    private showHideHUD hudController;

    private void Start()
    {
        hudController = FindFirstObjectByType<showHideHUD>();
        int headsetOr2D = PlayerPrefs.GetInt("toggletovr");
        UpdateVRSprites(headsetOr2D == 1);
    }

    public void SetVR(int headsetOr2D)
    {
        Debug.Log($"SetVR called with state: {headsetOr2D}");

        // Ensure both cameras are disabled first
        if (mainCamera2D != null) mainCamera2D.SetActive(false);
        if (mainCameraVR != null) mainCameraVR.SetActive(false);

        // Small delay to let the camera deactivation complete
        Invoke(nameof(ActivateCamera), 0.1f);

        UpdateVRSprites(headsetOr2D == 1);
    }

    private void ActivateCamera()
    {
        int headsetOr2D = PlayerPrefs.GetInt("toggletovr");

        if (headsetOr2D == 1)
        {
            Debug.Log("Activating VR camera");
            if (mainCameraVR != null) mainCameraVR.SetActive(true);
            if (mainCamera2D != null) mainCamera2D.SetActive(false);
        }
        else
        {
            Debug.Log("Activating 2D camera");
            if (mainCameraVR != null) mainCameraVR.SetActive(false);
            if (mainCamera2D != null) mainCamera2D.SetActive(true);
        }
    }

    private void UpdateVRSprites(bool isVRMode)
    {
        if (spriterendererVR != null)
        {
            spriterendererVR.sprite = isVRMode ? spritePickedNoVR : spritePickedVR;
        }
    }
}