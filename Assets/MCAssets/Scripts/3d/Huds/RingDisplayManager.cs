using UnityEngine;
using UnityEditor;

public class RingDisplayManager : MonoBehaviour
{
    [Header("Ring Settings")]
    [SerializeField] private int numberOfSprites = 6;
    [SerializeField] private float height = 0f;
    [SerializeField] private Sprite spriteTemplate;

    [Header("Position Settings")]
    [SerializeField] private float verticalOffset = -1.56f;
    [SerializeField] private float forwardOffset = -3.14f;
    [SerializeField] private float rotationOffset = 180f;
    [SerializeField] private float spriteScale = 0.00226f;

    private InvertedRingGenerator ringGenerator;
    private Transform cameraTransform;
    private Vector3 lastCameraPosition;
    private Quaternion lastCameraRotation;
    private int lastSpriteCount;

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            if (lastSpriteCount != numberOfSprites)
            {
                UpdateSpriteCount();
                lastSpriteCount = numberOfSprites;
            }
            UpdateSpritePositions();
        }
    }

    private void UpdateSpriteCount()
    {
        // Remove existing sprite objects
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }

        // Create new sprite objects
        for (int i = 0; i < numberOfSprites; i++)
        {
            GameObject spriteObj = new GameObject($"Sprite_{i}");
            spriteObj.transform.SetParent(transform);

            SpriteRenderer renderer = spriteObj.AddComponent<SpriteRenderer>();
            if (spriteTemplate != null)
            {
                renderer.sprite = spriteTemplate;
            }
        }
    }

    private void Start()
    {
        ringGenerator = GetComponent<InvertedRingGenerator>();
        if (ringGenerator == null)
        {
            Debug.LogError("Missing InvertedRingGenerator!");
            return;
        }

        cameraTransform = Camera.main?.transform;
        if (cameraTransform != null)
        {
            lastCameraPosition = cameraTransform.position;
            lastCameraRotation = cameraTransform.rotation;
            UpdateRingPosition();
        }

        UpdateSpritePositions();
    }

    private void LateUpdate()
    {
        if (cameraTransform != null &&
            (lastCameraPosition != cameraTransform.position ||
             lastCameraRotation != cameraTransform.rotation))
        {
            UpdateRingPosition();
            lastCameraPosition = cameraTransform.position;
            lastCameraRotation = cameraTransform.rotation;
        }
    }

    private void UpdateRingPosition()
    {
        Vector3 targetPosition = cameraTransform.position;
        targetPosition.y += verticalOffset;

        Vector3 forward = cameraTransform.forward;
        forward.y = 0;
        forward.Normalize();

        targetPosition += forward * forwardOffset;
        transform.position = targetPosition;

        Vector3 toCamera = cameraTransform.position - transform.position;
        toCamera.y = 0;
        transform.rotation = Quaternion.LookRotation(toCamera) * Quaternion.Euler(0, rotationOffset, 0);
    }

    private void UpdateSpritePositions()
    {
        if (ringGenerator == null) return;

        float radius = ringGenerator.InnerRadius;
        float angleStep = 360f / numberOfSprites;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform spriteTransform = transform.GetChild(i);

            float angle = i * angleStep;
            float rad = Mathf.Deg2Rad * angle;

            Vector3 newPos = new Vector3(
                radius * Mathf.Cos(rad),
                height,
                radius * Mathf.Sin(rad)
            );

            spriteTransform.localPosition = newPos;

            Vector3 dirToCenter = -newPos.normalized;
            spriteTransform.localRotation = Quaternion.LookRotation(dirToCenter, Vector3.up);
            spriteTransform.Rotate(90, 0, 0, Space.Self);

            spriteTransform.localScale = new Vector3(spriteScale, spriteScale, spriteScale);
        }
    }

    // Method to update sprite count at runtime if needed
    public void SetSpriteCount(int count)
    {
        numberOfSprites = count;
        UpdateSpriteCount();
        UpdateSpritePositions();
    }
}