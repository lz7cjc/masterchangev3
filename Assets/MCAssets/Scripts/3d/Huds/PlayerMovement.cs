using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;

    private bool isMoving = false;

    // Handle movement in Update
    private void Update()
    {
        if (isMoving)
        {
            MovePlayer();
        }
    }

    private void MovePlayer()
    {
        transform.position += transform.forward * moveSpeed * Time.deltaTime;
    }

    public void StartMoving()
    {
        isMoving = true;
    }

    public void StopMoving()
    {
        isMoving = false;
    }
}