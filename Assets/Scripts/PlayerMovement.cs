using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    private Rigidbody rb;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float airControlMultiplier = 0.5f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float maxSlopeAngle = 45f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 6f;
    [SerializeField] private float groundCheckDistance = 0.2f;

    [Header("Crouch Settings")]
    [SerializeField] private float crouchHeight = 0.5f;
    [SerializeField] private float standHeight = 2f;
    [SerializeField] private float crouchSpeedMultiplier = 0.6f;
    private bool isCrouching = false;

    [Header("Sprint Settings")]
    [SerializeField] private float sprintMultiplier = 1.5f;
    private bool isSprinting = false;

    [Header("Slide Settings")]
    [SerializeField] private float slideThreshold = 5f;
    [SerializeField] private float slideStaminaCost = 15f;
    [SerializeField] private float slideFriction = 0.95f;
    private bool isSliding = false;

    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float sprintDrainRate = 10f;
    [SerializeField] private float regenRate = 15f;
    [SerializeField] private float regenDelay = 1.5f;
    private float currentStamina;
    private float staminaRegenTimer;

    [Header("Environmental Modifiers")]
    [SerializeField] private float waterMultiplier = 0.5f;
    private float speedMultiplier = 1f;
    private bool isInWater = false;

    [Header("State Flags")]
    public bool isGrounded;
    public bool isAirborne;

    [Header("Stamina Bar Hook")]
    public UnityEvent<float, float> OnStaminaChanged; // (current, max)

    private Vector2 moveInput;
    private bool jumpPressed;
    private bool crouchToggled;

    private CapsuleCollider col;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();
        currentStamina = maxStamina;
    }

    private void Update()
    {
        CheckGrounded();
        HandleInput();
        HandleStamina();
        UpdateStates();
    }

    private void FixedUpdate()
    {
        HandleMovement();
        ApplySlideFriction();
    }

    private void HandleInput()
    {
        if (jumpPressed && isGrounded && !isCrouching)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        if (crouchToggled)
        {
            ToggleCrouch();
        }

        if (ShouldSlide())
        {
            StartSlide();
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
            jumpPressed = true;
        if (context.canceled)
            jumpPressed = false;
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        if (context.performed)
            crouchToggled = true;
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        isSprinting = context.ReadValueAsButton();
    }

    private void HandleMovement()
    {
        Vector3 direction = new Vector3(moveInput.x, 0, moveInput.y);
        direction = cameraTransform.TransformDirection(direction);
        direction.y = 0;

        float targetSpeed = moveSpeed;
        if (isCrouching) targetSpeed *= crouchSpeedMultiplier;
        else if (isSprinting && currentStamina > 0f && !isCrouching) targetSpeed *= sprintMultiplier;

        Vector3 desiredVelocity = direction.normalized * targetSpeed * speedMultiplier;

        Vector3 velocityChange = desiredVelocity - rb.linearVelocity;
        velocityChange.y = 0;

        float control = isGrounded ? 1f : airControlMultiplier;
        rb.AddForce(velocityChange * acceleration * control, ForceMode.Acceleration);
    }

    private void CheckGrounded()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance + 0.1f, LayerMask.GetMask("Ground"));
        isAirborne = !isGrounded;
    }

    private void ToggleCrouch()
    {
        isCrouching = !isCrouching;
        crouchToggled = false;

        col.height = isCrouching ? crouchHeight : standHeight;

        Vector3 center = col.center;
        center.y = col.height / 2f;
        col.center = center;
    }

    private void HandleStamina()
    {
        bool draining = false;

        if (isSprinting && moveInput.magnitude > 0.1f && !isCrouching && isGrounded && !isSliding)
        {
            currentStamina -= sprintDrainRate * Time.deltaTime;
            currentStamina = Mathf.Max(currentStamina, 0);
            draining = true;
        }

        if (isSliding)
        {
            currentStamina -= slideStaminaCost * Time.deltaTime;
            currentStamina = Mathf.Max(currentStamina, 0);
            draining = true;
        }

        if (!draining)
        {
            staminaRegenTimer += Time.deltaTime;
            if (staminaRegenTimer >= regenDelay)
            {
                currentStamina += regenRate * Time.deltaTime;
                currentStamina = Mathf.Min(currentStamina, maxStamina);
            }
        }
        else
        {
            staminaRegenTimer = 0f;
        }

        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    private bool ShouldSlide()
    {
        return isSprinting && isCrouching && rb.linearVelocity.magnitude > slideThreshold && currentStamina > slideStaminaCost;
    }

    private void StartSlide()
    {
        isSliding = true;
    }

    private void ApplySlideFriction()
    {
        if (isSliding)
        {
            Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            horizontalVel *= slideFriction;
            rb.linearVelocity = new Vector3(horizontalVel.x, rb.linearVelocity.y, horizontalVel.z);

            if (horizontalVel.magnitude < 1f || !isCrouching)
            {
                isSliding = false;
            }
        }
    }

    private void UpdateStates()
    {
        // For water or other zones, set this from another script/trigger
        // Example:
        // speedMultiplier = isInWater ? waterMultiplier : 1f;
    }

    // Hook from other scripts to set speed multipliers
    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = multiplier;
    }

    public void SetInWater(bool inWater)
    {
        isInWater = inWater;
        SetSpeedMultiplier(inWater ? waterMultiplier : 1f);
    }

    public float GetStaminaNormalized()
    {
        return currentStamina / maxStamina;
    }
}
