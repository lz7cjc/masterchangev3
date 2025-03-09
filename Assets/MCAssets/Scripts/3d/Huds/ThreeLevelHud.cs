using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class ThreeLevelHUD : MonoBehaviour
{
    [Header("HUD Components")]
    [SerializeField] private GameObject level1Container; // Main HUD toggle
    [SerializeField] private GameObject level2Container; // Icon container
    [SerializeField] private GameObject[] level3Containers; // Button containers (one per icon)

    [Header("Animation")]
    [SerializeField] private float transitionSpeed = 5f;

    // State tracking
    private bool isLevel1Open = false;
    private int activeLevel2Icon = -1;

    // Reference to camera/headset
    private Transform cameraTransform;

    // Input handling
    private bool isVRMode = false;

    // Reference to the new Input System
    private InputAction mouseClickAction;
    private Camera mainCamera;

    // Store original local positions/rotations of all containers
    private Vector3 level2LocalPosition;
    private Quaternion level2LocalRotation;
    private Vector3[] level3LocalPositions;
    private Quaternion[] level3LocalRotations;

    void Awake()
    {
        // Set up the mouse click action using the new Input System
        mouseClickAction = new InputAction("MouseClick", binding: "<Mouse>/leftButton");
        mouseClickAction.performed += ctx => OnMouseClick();
        mouseClickAction.Enable();

        mainCamera = Camera.main;

        // Store the original local transforms
        if (level2Container != null)
        {
            level2LocalPosition = level2Container.transform.localPosition;
            level2LocalRotation = level2Container.transform.localRotation;
        }

        level3LocalPositions = new Vector3[level3Containers.Length];
        level3LocalRotations = new Quaternion[level3Containers.Length];

        for (int i = 0; i < level3Containers.Length; i++)
        {
            if (level3Containers[i] != null)
            {
                level3LocalPositions[i] = level3Containers[i].transform.localPosition;
                level3LocalRotations[i] = level3Containers[i].transform.localRotation;
            }
        }
    }

    void Start()
    {
        // Get reference to the main camera
        cameraTransform = Camera.main.transform;

        // Check if VR is active
        isVRMode = XRSettings.isDeviceActive;

        // Initialize HUD state
        level1Container.SetActive(true);
        level2Container.SetActive(false);
        foreach (GameObject container in level3Containers)
        {
            if (container != null)
                container.SetActive(false);
        }
    }

    void OnDestroy()
    {
        // Clean up input actions
        if (mouseClickAction != null)
        {
            mouseClickAction.Disable();
            mouseClickAction.Dispose();
        }
    }

    void Update()
    {
        // Ensure level2 and level3 containers maintain their relative positions
        UpdateChildContainersPositions();
    }

    void UpdateChildContainersPositions()
    {
        // Make sure level2 and level3 containers maintain their relative positions to main HUD

        // Update level2 position and rotation
        if (level2Container != null)
        {
            level2Container.transform.localPosition = level2LocalPosition;
            level2Container.transform.localRotation = level2LocalRotation;
        }

        // Update all level3 positions and rotations
        for (int i = 0; i < level3Containers.Length; i++)
        {
            if (level3Containers[i] != null)
            {
                level3Containers[i].transform.localPosition = level3LocalPositions[i];
                level3Containers[i].transform.localRotation = level3LocalRotations[i];
            }
        }
    }

    void OnMouseClick()
    {
        if (isVRMode)
        {
            // VR interaction would be handled separately through XR Interaction Toolkit
            return;
        }

        // Get current mouse position
        Vector2 mousePosition = Mouse.current.position.ReadValue();

        // Cast a ray from the mouse position
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            // Check what was hit and respond accordingly
            if (hit.transform.gameObject == level1Container)
            {
                ToggleLevel1();
            }

            // Check for level2 icons - you'll need to add specific checks for your actual icon objects

            // Check for level3 buttons
            for (int i = 0; i < level3Containers.Length; i++)
            {
                if (hit.transform.gameObject == level3Containers[i] ||
                    (hit.transform.parent != null && hit.transform.parent.gameObject == level3Containers[i]))
                {
                    SelectLevel2Icon(i);
                    break;
                }
            }
        }
    }

    public void ToggleLevel1()
    {
        isLevel1Open = !isLevel1Open;

        if (isLevel1Open)
        {
            // Open Level 1 and show Level 2
            level2Container.SetActive(true);
        }
        else
        {
            // Close all levels
            level2Container.SetActive(false);
            foreach (GameObject container in level3Containers)
            {
                if (container != null)
                    container.SetActive(false);
            }
        }
    }

    public void SelectLevel2Icon(int iconIndex)
    {
        // Close previously open Level 3 if any
        if (activeLevel2Icon >= 0 && activeLevel2Icon < level3Containers.Length)
        {
            level3Containers[activeLevel2Icon].SetActive(false);
        }

        // Activate the selected Level 3
        activeLevel2Icon = iconIndex;
        if (iconIndex >= 0 && iconIndex < level3Containers.Length)
        {
            level3Containers[iconIndex].SetActive(true);
        }
    }

    public void PressLevel3Button(int buttonIndex)
    {
        // Handle specific button functionality
        Debug.Log($"Button {buttonIndex} pressed on Level 3 menu {activeLevel2Icon}");

        // Implement your button-specific functionality here
    }
}