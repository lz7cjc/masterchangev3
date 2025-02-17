using UnityEngine;
using UnityEngine.EventSystems;

public class GoIconTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float gazeDelay = 2f;
    private float gazeTimer = 0f;
    private bool isGazing = false;
    private PlayerMovement playerMovement;

    private void Start()
    {
        // Find the PlayerMovement script on the player object
        playerMovement = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>();

        if (playerMovement == null)
        {
            Debug.LogError("Player with PlayerMovement script not found! Make sure your player has the 'Player' tag.");
        }
    }

    private void Update()
    {
        if (isGazing)
        {
            gazeTimer += Time.deltaTime;

            if (gazeTimer >= gazeDelay && playerMovement != null)
            {
                playerMovement.StartMoving();
                isGazing = false;
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isGazing = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isGazing = false;
        gazeTimer = 0f;
    }
}