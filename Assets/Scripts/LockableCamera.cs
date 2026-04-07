using UnityEngine;

public class LockableCamera : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float lookSpeed = 2f;

    private bool isLocked = false;
    private float yaw = 0f;
    private float pitch = 0f;

    public GameObject uiCanvas;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        uiCanvas.SetActive(false);
    }

    void Update()
    {
        // Toggle lock mode with Escape
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            isLocked = !isLocked;

            Cursor.lockState = isLocked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isLocked;

            if (uiCanvas != null)
                uiCanvas.SetActive(isLocked);
        }

        if (!isLocked)
        {
            HandleMovement();
            HandleRotation();
        }
    }

    void HandleMovement()
    {
        Vector3 direction = new Vector3(
            Input.GetAxis("Horizontal"), // A/D
            0,
            Input.GetAxis("Vertical")    // W/S
        );

        Vector3 move = transform.TransformDirection(direction) * moveSpeed * Time.deltaTime;
        transform.position += move;

        // Up/down movement with Q/E
        if (Input.GetKey(KeyCode.E)) transform.position += Vector3.up * moveSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.Q)) transform.position -= Vector3.up * moveSpeed * Time.deltaTime;
    }

    void HandleRotation()
    {
        yaw += Input.GetAxis("Mouse X") * lookSpeed;
        pitch -= Input.GetAxis("Mouse Y") * lookSpeed;
        pitch = Mathf.Clamp(pitch, -89f, 89f); // Prevent flipping

        transform.eulerAngles = new Vector3(pitch, yaw, 0f);
    }
}
