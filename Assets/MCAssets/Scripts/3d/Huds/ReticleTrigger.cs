using System.Collections;
using UnityEngine;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;

public class ReticleTrigger : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private Vector3 rotationDirection = Vector3.up; // Set -Vector3.up for left, Vector3.up for right
    [SerializeField] private float Delay = 2f; // Time before rotation starts

    [Header("Interaction Settings")]
    [SerializeField] private float maxInteractionDistance = 10f;
    [SerializeField] private LayerMask interactableLayer;

    //private bool isHovering = false;
    private bool isRotating = false;
    [SerializeField] private float Counter = 0;
    [SerializeField] private Camera mainCamera;
    private Coroutine rotationCoroutine;
    private GameObject currentTarget;
    private bool mouseHover = false;
    [SerializeField] private int delay = 3;


    [SerializeField] private hudCountdown hudCountdown;

    private void Start()
    {

        if (mainCamera != null)
        {
            Debug.Log("Start Camera found: " + mainCamera.name);
        }
        else
        {
            Debug.LogError(" Start Main Camera not assigned!");
        }
    }

    void Update()
    {
        if (mouseHover)
        {
            hudCountdown.SetCountdown(delay, Counter);
            Counter += Time.deltaTime;
            Debug.Log("toCounter =" + Counter + "time.deltatime = " + Time.deltaTime);

            //waiting
            if (Counter < Delay)
            {
                Debug.Log("reticletrigger waiting");
                Debug.Log("toCounter =" + Counter + "time.deltatime = " + Time.deltaTime);
                if (mainCamera != null)
                {
                    Debug.Log("Updatewait Camera found: " + mainCamera.name);
                }
                else
                {
                    Debug.LogError("Updatewait Main Camera not assigned!");
                }

                CheckReticlePosition(); // Make sure the reticle stays locked on the object
                if (hudCountdown == null)
                {
                    hudCountdown = FindFirstObjectByType<hudCountdown>();
                }

                if (hudCountdown != null)
                {
                    hudCountdown.SetCountdown(delay, Counter);
                }
            }

            //triggering
            else if (Counter >= Delay)

            {
                if (mainCamera != null)
                {
                    Debug.Log("Updatego Camera found: " + mainCamera.name);
                }
                else
                {
                    Debug.LogError("Updatego  Main Camera not assigned!");
                }
                hudCountdown.resetCountdown();
                Debug.Log("reticletrigger executing");
                //put some code here once triggered the action 
                if (isRotating)
                {
                    RotateCamera();
                    StartRotationAfterDelay();

                }

            }




        }
    }

    /// <summary>
    /// Called when the reticle enters the interactive object's collider.
    /// </summary>
    public void OnReticleEnter()
    {
        
            Debug.Log("reticletrigger enter");
            mouseHover = true;
            isRotating = true;
            currentTarget = gameObject; // Lock the current interactive object
           
       
    }

    /// <summary>
    /// Called when the reticle exits the interactive object's collider.
    /// </summary>
    public void OnReticleExit()
    {
        mouseHover = false;
        isRotating = false;
        Counter = 0;

        if (rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
            rotationCoroutine = null;
        }
    }

    private IEnumerator StartRotationAfterDelay()
    {
        yield return new WaitForSeconds(Delay);

        if (mouseHover) // Ensure the reticle is still on the object
        {
            isRotating = true;
        }

        rotationCoroutine = null;
    }

    private void RotateCamera()
    {

        if (mainCamera != null)
        {
            Debug.Log("Camera found: " + mainCamera.name);
        }
        else
        {
            Debug.LogError("Main Camera not assigned!");
        }
        if (mainCamera != null)
        {
            mainCamera.transform.Rotate(rotationDirection * (rotationSpeed * Time.deltaTime));
        }
    }

    /// <summary>
    /// Ensures the reticle remains over the interactive object while rotating.
    /// </summary>
    private void CheckReticlePosition()
    {
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxInteractionDistance, interactableLayer))
        {
            if (hit.collider.gameObject == currentTarget)
            {
                return; // The reticle is still on the correct object, do nothing
            }
        }

        OnReticleExit(); // Stop rotation if the reticle moves off the object
    }
}
