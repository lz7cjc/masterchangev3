//using UnityEngine;
//using UnityEngine.InputSystem; // Make sure to use the new Input System

//public class MouseLook : MonoBehaviour
//{
//    public enum RotationAxes { MouseXAndY = 0, MouseX = 1, MouseY = 2 }
//    public RotationAxes axes = RotationAxes.MouseXAndY;
//    public float sensitivityX = 15F;
//    public float sensitivityY = 15F;
//    public float minimumX = -360F;
//    public float maximumX = 360F;
//    public float minimumY = -60F;
//    public float maximumY = 60F;
//    private float rotationY = 0F;

//    private InputActions playerInputActions; // Reference to your input action class
//    private InputAction lookAction; // Action for mouse or touch look input

//    private void OnEnable()
//    {
//        // Initialize the Input Actions asset
//        playerInputActions = new InputActions();

//        // Get the "Look" action from your PlayerControls action map
//        lookAction = playerInputActions.PlayerControls.Look;

//        // Enable the action to start listening to input
//        lookAction.Enable();
//    }

//    private void OnDisable()
//    {
//        // Disable the action when the object is disabled to stop listening to input
//        lookAction.Disable();
//    }

//    void Update()
//    {
//        // Read the delta from the "Look" action, which will work for both mouse and touch input
//        Vector2 lookInput = lookAction.ReadValue<Vector2>();

//        // Get mouse or touch delta values from the action's binding
//        float mouseDeltaX = lookInput.x;
//        float mouseDeltaY = lookInput.y;

//        if (axes == RotationAxes.MouseXAndY)
//        {
//            // Combine X and Y axis for mouse or touch look
//            float rotationX = transform.localEulerAngles.y + mouseDeltaX * sensitivityX;
//            rotationY += mouseDeltaY * sensitivityY;
//            rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);

//            transform.localEulerAngles = new Vector3(-rotationY, rotationX, 0);
//        }
//        else if (axes == RotationAxes.MouseX)
//        {
//            // Only rotate around Y axis (for mouse or touch)
//            transform.Rotate(0, mouseDeltaX * sensitivityX, 0);
//        }
//        else
//        {
//            // Only rotate around X axis (for mouse or touch)
//            rotationY += mouseDeltaY * sensitivityY;
//            rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);

//            transform.localEulerAngles = new Vector3(-rotationY, transform.localEulerAngles.y, 0);
//        }
//    }

//    void Start()
//    {
//        // Make the rigid body not change rotation (if there's a rigidbody attached)
//        if (GetComponent<Rigidbody>())
//            GetComponent<Rigidbody>().freezeRotation = true;
//    }
//}
