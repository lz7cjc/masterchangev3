using System.Collections;
using UnityEngine;

public class ReticleTrigger : MonoBehaviour
{
    public Camera mainCamera;
    public float maxInteractionDistance = 10f;
    public LayerMask interactableLayer;
    public float rotationSpeed = 50f;
    public float Delay = 3f;
    private bool mouseHover = false;
    private bool isRotating = false;
    private float Counter = 0f;
    private GameObject currentTarget;
    private Coroutine rotationCoroutine;

    private void Update()
    {
        if (mouseHover)
        {
            Counter += Time.deltaTime;
            CheckReticlePosition();
            if (Counter >= Delay)
            {
                if (mainCamera != null)
                {
                    Debug.Log("Updatego Camera found: " + mainCamera.name);
                }
                else
                {
                    Debug.LogError("Updatego  Main Camera not assigned!");
                }
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
            mainCamera.transform.Rotate(Vector3.up * (rotationSpeed * Time.deltaTime));
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
            if (hit.collider.GetComponent<BoxCollider>() != null && hit.collider.gameObject == currentTarget)
            {
                return; // The reticle is still on the correct object, do nothing
            }
        }

        OnReticleExit(); // Stop rotation if the reticle moves off the object
    }
}
