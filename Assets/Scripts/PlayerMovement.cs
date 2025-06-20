using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
	
	public Transform orientation;
	
	public float speed = 3f;
	public float groundDrag;
	
	public float playerHeight = 2f;
	public LayerMask whatIsGround;
	public bool grounded;
	
	
	
	
	private PlayerInput input;
    private Vector2 moveInput;
	
	Rigidbody rb;
	
	
	void Awake()
	{
		input = new PlayerInput();
        input.OnFoot.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        input.OnFoot.Move.canceled += ctx => moveInput = Vector2.zero;
	}
	
	void Start()
	{
		rb = GetComponent<Rigidbody>();
		
	}
	
	void Update()
	{
		grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);
		
		
		if(grounded)
			rb.linearDamping = groundDrag;
		else
			rb.linearDamping = 0;
	}
	
	void FixedUpdate()
	{
		MovePlayer();
	}
	
	
	private void MovePlayer()
	{
		Vector3 moveDir = orientation.forward * moveInput.y + orientation.right * moveInput.x;
		
		rb.AddForce(moveDir.normalized * speed * 10f, ForceMode.Force);
		
		
	}
	
	
	void OnEnable() => input.Enable();
    void OnDisable() => input.Disable();
}