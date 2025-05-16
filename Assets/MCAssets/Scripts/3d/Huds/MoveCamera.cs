using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MoveCamera : MonoBehaviour
{
    public bool mousehover = false;
    public float Counter = 0;
    private hudCountdown hudCountdown;
    public int delay = 3;
    public Rigidbody player;
    private GameObject cameraTarget;
    public bool isTitle = false;
    public TMP_Text TMP_title;
    public bool gravity = true;
    [SerializeField] private closeAllHuds closeAllHuds;
    [SerializeField] private ToggleActiveIcons toggleActiveIcons;

    // NEW: Add this dictionary to map GameObjects to their zone IDs
    [System.Serializable]
    public class ZoneMapping
    {
        public GameObject targetObject;
        public string zoneID;  // This should match the exact ID expected by StartUp.cs
    }

    [Header("Zone Mappings")]
    [SerializeField] private List<ZoneMapping> zoneMappings = new List<ZoneMapping>();

    public void Start()
    {
        Debug.Log("in start of toggle movecamera");
        hudCountdown = FindFirstObjectByType<hudCountdown>();
    }

    void Update()
    {
        if (mousehover)
        {
            toggleActiveIcons.HoverIcon();
            hudCountdown.SetCountdown(delay, Counter);
            Counter += Time.deltaTime;

            if (Counter >= delay)
            {
                Debug.Log("in select of toggle movecamera");

                mousehover = false;
                Counter = 0;
                hudCountdown.resetCountdown();
                showandhide();
            }
        }
    }

    public void MouseHoverChangeScene(GameObject _cameraTarget)
    {
        Debug.Log("in hover of toggle movecamera");

        if (isTitle)
        {
            TMP_title.color = Color.white;
        }
        mousehover = true;
        cameraTarget = _cameraTarget;
    }

    public void MouseExit()
    {
        Debug.Log("in default of toggle movecamera");

        toggleActiveIcons.DefaultIcon();
        mousehover = false;
        Counter = 0;

        hudCountdown.resetCountdown();
    }

    private void showandhide()
    {
        toggleActiveIcons.SelectIcon();
        Counter = 0;
        player.useGravity = gravity;

        closeAllHuds.CloseTheHuds();

        // Set the player as a child of the cameraTarget
        player.transform.SetParent(cameraTarget.transform);

        // Reset the player's position to (0, 0, 0) relative to the cameraTarget
        player.transform.localPosition = Vector3.zero;

        // Get the correct zone ID from our mappings
        string zoneID = GetZoneIDForTarget(cameraTarget);
        if (!string.IsNullOrEmpty(zoneID))
        {
            PlayerPrefs.SetString("lastknownzone", zoneID);
            Debug.Log($"Set lastknownzone to: {zoneID}");
        }
        else
        {
            Debug.LogWarning($"No zone ID mapping found for target: {cameraTarget.name}. Using name directly.");
            PlayerPrefs.SetString("lastknownzone", cameraTarget.name);
        }
    }

    // Helper method to find the zone ID for a target GameObject
    private string GetZoneIDForTarget(GameObject target)
    {
        foreach (var mapping in zoneMappings)
        {
            if (mapping.targetObject == target)
            {
                return mapping.zoneID;
            }
        }
        return null;  // No mapping found
    }
}