using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour
{
    [Header("Sensitivity")]
    [SerializeField] private float sensitivityX = 2f;
    [SerializeField] private float sensitivityY = 2f;

    private PlayerInput input;
    private Vector2 lookInput;

    private float pitch = 0f;
    private Transform cameraHolder;
    private Transform cam;

    void Awake()
    {
        input = new PlayerInput();
        input.OnFoot.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        input.OnFoot.Look.canceled += ctx => lookInput = Vector2.zero;

        SetupCameraHolder();

        // Lock and hide the cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnEnable() => input.Enable();
    void OnDisable() => input.Disable();

    void Update()
    {
        Vector2 delta = lookInput * Time.deltaTime;

        // Apply sensitivity
        float mouseX = delta.x * sensitivityX;
        float mouseY = delta.y * sensitivityY;

        // Rotate camera vertically (pitch)
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -90f, 90f);
        cameraHolder.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        // Rotate player horizontally (yaw)
        transform.Rotate(Vector3.up * mouseX);
    }

    private void SetupCameraHolder()
    {
        cam = Camera.main?.transform;

        if (cam == null)
        {
            Debug.LogError("No Main Camera found. Tag your camera as 'MainCamera'.");
            return;
        }

        // Create or find camera holder
        cameraHolder = transform.Find("CameraHolder");
        if (cameraHolder == null)
        {
            GameObject holder = new GameObject("CameraHolder");
            holder.transform.SetParent(transform);
            holder.transform.localPosition = Vector3.zero;
            holder.transform.localRotation = Quaternion.identity;
            cameraHolder = holder.transform;
        }

        // Move camera under cameraHolder
        cam.SetParent(cameraHolder);
        cam.localPosition = Vector3.zero;
        cam.localRotation = Quaternion.identity;
    }
}
