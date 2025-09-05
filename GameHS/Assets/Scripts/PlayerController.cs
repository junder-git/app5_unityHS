using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 200f;
    public float jumpForce = 10f;
    public float gravityStrength = 60f;
    
    [Header("Camera Settings")]
    public float mouseSensitivity = 60f;
    
    [Header("References")]
    public Camera playerCamera;
    public Transform playerVisual;
    
    [Header("Physics")]
    public LayerMask groundLayer = -1;
    public float groundCheckDistance = 3f;
    public float playerRadius = 1f;
    
    private Vector3 velocity;
    private bool isGrounded;
    private float yaw, pitch;
    private Vector3 planetCenter;
    private float planetRadius;
    private Rigidbody rb;
    private CapsuleCollider playerCollider;
    
    // Input state
    private Vector2 moveInput;
    private bool jumpInput;
    private Vector2 mouseInput;
    
    private void Start()
    {
        // Get planet data from GameManager
        var gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            planetCenter = gameManager.PlanetCenter;
            planetRadius = gameManager.PlanetRadius;
        }
        
        // Get or add physics components
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        playerCollider = GetComponent<CapsuleCollider>();
        if (playerCollider == null)
        {
            playerCollider = gameObject.AddComponent<CapsuleCollider>();
        }
        
        // Configure rigidbody
        rb.mass = 1f;
        rb.linearDamping = 2f;
        rb.angularDamping = 5f;
        rb.useGravity = false; // We'll handle gravity ourselves
        rb.freezeRotation = true; // Prevent physics rotation
        
        // Configure collider
        playerCollider.radius = playerRadius;
        playerCollider.height = 2f;
        playerCollider.center = Vector3.up;
        
        // Lock cursor for mouse look
        Cursor.lockState = CursorLockMode.Locked;
        
        // Position player above planet surface
        Vector3 spawnPos = planetCenter + Vector3.up * (planetRadius + 20f);
        transform.position = spawnPos;
        
        // Setup first person camera
        if (playerCamera != null)
        {
            playerCamera.transform.SetParent(transform);
            playerCamera.transform.localPosition = Vector3.up * 1.8f; // Eye level
            playerCamera.transform.localRotation = Quaternion.identity;
        }
        
        Debug.Log($"🚀 Player spawned with spherical gravity at: {spawnPos}");
    }
    
    private void Update()
    {
        HandleInput();
        HandleMouseLook();
        CheckGroundContact();
        AlignToGravity();
    }
    
    private void FixedUpdate()
    {
        ApplyMovement();
        ApplySphericalGravity();
        PreventPlanetClipping();
    }
    
    private void HandleInput()
    {
        // Movement input
        moveInput = new Vector2(
            Input.GetAxis("Horizontal"),
            Input.GetAxis("Vertical")
        );
        
        // Jump input
        jumpInput = Input.GetKeyDown(KeyCode.Space);
        
        // Mouse input
        mouseInput = new Vector2(
            Input.GetAxis("Mouse X"),
            Input.GetAxis("Mouse Y")
        );
        
        // Debug toggle
        if (Input.GetKeyDown(KeyCode.F1))
        {
            var debugUI = FindObjectOfType<DebugUI>();
            if (debugUI != null) debugUI.ToggleDisplay();
        }
        
        // Quit
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Q))
        {
            Application.Quit();
        }
    }
    
    private void HandleMouseLook()
    {
        yaw += mouseInput.x * mouseSensitivity * Time.deltaTime;
        pitch -= mouseInput.y * mouseSensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, -89f, 89f);
        
        // Apply rotation to camera only (first person)
        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }
    
    private void ApplyMovement()
    {
        if (moveInput.magnitude > 0.1f)
        {
            // Get the up direction relative to planet
            Vector3 upDirection = GetUpDirection();
            
            // Get camera forward and right directions relative to planet surface
            Vector3 cameraForward = playerCamera.transform.forward;
            Vector3 cameraRight = playerCamera.transform.right;
            
            // Project camera directions onto the planet surface (perpendicular to up)
            Vector3 moveForward = Vector3.ProjectOnPlane(cameraForward, upDirection).normalized;
            Vector3 moveRight = Vector3.ProjectOnPlane(cameraRight, upDirection).normalized;
            
            // Calculate movement direction
            Vector3 moveDirection = (moveForward * moveInput.y + moveRight * moveInput.x).normalized;
            
            // Apply movement force to rigidbody
            rb.AddForce(moveDirection * moveSpeed * Time.fixedDeltaTime, ForceMode.VelocityChange);
        }
        
        // Jump
        if (jumpInput && isGrounded)
        {
            Vector3 upDirection = GetUpDirection();
            rb.AddForce(upDirection * jumpForce, ForceMode.VelocityChange);
            isGrounded = false;
            Debug.Log("🦘 Player jumped!");
        }
    }
    
    private void ApplySphericalGravity()
    {
        // Calculate gravity direction (toward planet center)
        Vector3 gravityDirection = (planetCenter - transform.position).normalized;
        
        // Apply gravity force
        rb.AddForce(gravityDirection * gravityStrength * Time.fixedDeltaTime, ForceMode.Acceleration);
    }
    
    private void AlignToGravity()
    {
        // Get the up direction (away from planet center)
        Vector3 targetUp = GetUpDirection();
        
        // Smoothly rotate the player to align with gravity
        Quaternion targetRotation = Quaternion.FromToRotation(transform.up, targetUp) * transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
    }
    
    private void CheckGroundContact()
    {
        // Get the down direction (toward planet center)
        Vector3 downDirection = (planetCenter - transform.position).normalized;
        Vector3 rayStart = transform.position + GetUpDirection() * 0.5f;
        
        RaycastHit hit;
        if (Physics.Raycast(rayStart, downDirection, out hit, groundCheckDistance, groundLayer))
        {
            isGrounded = hit.distance < 2.5f;
            
            // Debug ray
            Debug.DrawRay(rayStart, downDirection * groundCheckDistance, isGrounded ? Color.green : Color.red);
        }
        else
        {
            isGrounded = false;
            Debug.DrawRay(rayStart, downDirection * groundCheckDistance, Color.red);
        }
    }
    
    private void PreventPlanetClipping()
    {
        // Additional safety check to prevent clipping through planet
        float distanceToCenter = Vector3.Distance(transform.position, planetCenter);
        float minDistance = planetRadius + playerRadius + 1f; // Add small buffer
        
        if (distanceToCenter < minDistance)
        {
            // Push player away from planet center
            Vector3 pushDirection = (transform.position - planetCenter).normalized;
            Vector3 correctedPosition = planetCenter + pushDirection * minDistance;
            
            transform.position = correctedPosition;
            
            // Stop velocity going into planet
            Vector3 velocityIntoPlanet = Vector3.Project(rb.linearVelocity, -pushDirection);
            if (Vector3.Dot(velocityIntoPlanet, -pushDirection) > 0)
            {
                rb.linearVelocity -= velocityIntoPlanet;
            }
            
            Debug.Log("⚠️ Prevented planet clipping - pushed player out");
        }
    }
    
    private Vector3 GetUpDirection()
    {
        return (transform.position - planetCenter).normalized;
    }
    
    // Public methods for debug display
    public Vector3 GetPosition() => transform.position;
    public Vector3 GetVelocity() => rb.linearVelocity;
    public bool IsGrounded() => isGrounded;
    public float GetDistanceFromCenter() => Vector3.Distance(transform.position, planetCenter);
}