using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    [Header("References")]
    public Transform player; // Player root

    [Header("Settings")]
    public float sensitivity = 100f;
    public float minPitch = -90f;
    public float maxPitch = 90f;
    public float inputDeadZone = 0.01f; // Prevents jitter from analog sticks

    private float xRotation;
    private PlayerInputHandler localInputHandler;

    private PlayerInputHandler GetInputHandler()
    {
        if (localInputHandler == null)
            localInputHandler = GetComponentInParent<PlayerInputHandler>();

        return localInputHandler;
    }

    void Start()
    {
        localInputHandler = GetComponentInParent<PlayerInputHandler>();
        ValidateReferences();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void ValidateReferences()
    {
        if (player == null)
        {
            Debug.LogError("PlayerLook: Player Transform reference is not assigned!", gameObject);
        }
        
        if (GetInputHandler() == null)
        {
            Debug.LogError("PlayerLook: Local PlayerInputHandler is not initialized!", gameObject);
        }
    }

    void LateUpdate()
    {
        PlayerInputHandler input = GetInputHandler();
        if (player == null || input == null)
            return;

        Vector2 lookInput = input.LookInput;

        // Apply dead zone to prevent jitter from analog input
        if (lookInput.magnitude < inputDeadZone)
            lookInput = Vector2.zero;

        float mouseX = lookInput.x * sensitivity * Time.deltaTime;
        float mouseY = lookInput.y * sensitivity * Time.deltaTime;

        // Vertical look (camera)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minPitch, maxPitch);
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Horizontal look (player)
        player.Rotate(Vector3.up * mouseX);
    }
}
