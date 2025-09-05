using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 200f;
    public float jumpForce = 10f;
    public float gravityStrength = 60f;
    
    [Header("Camera Settings")]
    public float mouseSensitivity = 60f;
    public float cameraDistance = 20f;
    public float cameraHeight = 8f;
    
    [Header("References")]
    public Camera playerCamera;
    public Transform playerVisual;
    
    [Header("Physics")]
    public LayerMask groundLayer = -1;
    public float groundCheckDistance = 5f;
    public float playerRadius = 1f;
    
    private Vector3 velocity;
    private bool isGrounded;
    private float yaw, pitch;
    private Vector3 planetCenter;
    private float planetRadius;
    private bool isFirstPerson = false;
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
        rb.drag = 2f;
        rb.angularDrag = 5f;
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
        
        Debug.Log($"🚀 Player spawned with physics at: {spawnPos}");
    }
    
    private void Update()
    {
        HandleInput();
        HandleMouseLook();
        CheckGroundContact();
        UpdateCamera();
    }
    
    private void FixedUpdate()
    {
        ApplyMovement();
        ApplyGravity();
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
        
        // Camera toggle
        if (Input.GetKeyDown(KeyCode.F2))
        {
            ToggleCameraMode();
        }
        
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
    }
    
    private void ApplyMovement()
    {
        if (moveInput.magnitude > 0.1f)
        {
            // Calculate movement direction relative to camera yaw
            Vector3 forward = new Vector3(Mathf.Sin(yaw * Mathf.Deg2Rad), 0, -Mathf.Cos(yaw * Mathf.Deg2Rad));
            Vector3 right = new Vector3(Mathf.Cos(yaw * Mathf.Deg2Rad), 0, Mathf.Sin(yaw * Mathf.Deg2Rad));
            
            Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;
            
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
    
    private void ApplyGravity()
    {
        Vector3 gravityDirection = (planetCenter - transform.position).normalized;
        rb.AddForce(gravityDirection * gravityStrength * Time.fixedDeltaTime, ForceMode.Acceleration);
    }
    
    private void CheckGroundContact()
    {
        // Raycast downward to check for ground
        Vector3 rayStart = transform.position + Vector3.up * 0.5f;
        Vector3 rayDirection = (planetCenter - transform.position).normalized;
        
        RaycastHit hit;
        if (Physics.Raycast(rayStart, rayDirection, out hit, groundCheckDistance, groundLayer))
        {
            isGrounded = hit.distance < 2f;
            
            // Debug ray
            Debug.DrawRay(rayStart, rayDirection * groundCheckDistance, isGrounded ? Color.green : Color.red);
        }
        else
        {
            isGrounded = false;
            Debug.DrawRay(rayStart, rayDirection * groundCheckDistance, Color.red);
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
            Vector3 velocityIntoPlanet = Vector3.Project(rb.velocity, -pushDirection);
            if (Vector3.Dot(velocityIntoPlanet, -pushDirection) > 0)
            {
                rb.velocity -= velocityIntoPlanet;
            }
            
            Debug.Log("⚠️ Prevented planet clipping - pushed player out");
        }
    }
    
    private void UpdateCamera()
    {
        Vector3 upDirection = GetUpDirection();
        Vector3 backDirection = new Vector3(-Mathf.Sin(yaw * Mathf.Deg2Rad), 0, Mathf.Cos(yaw * Mathf.Deg2Rad));
        
        if (isFirstPerson)
        {
            // First person camera
            playerCamera.transform.position = transform.position + upDirection * 2f;
            Vector3 lookDirection = new Vector3(Mathf.Sin(yaw * Mathf.Deg2Rad), 0, -Mathf.Cos(yaw * Mathf.Deg2Rad));
            playerCamera.transform.LookAt(transform.position + lookDirection, upDirection);
        }
        else
        {
            // Third person camera
            Vector3 cameraPos = transform.position + backDirection * cameraDistance + upDirection * cameraHeight;
            playerCamera.transform.position = cameraPos;
            playerCamera.transform.LookAt(transform.position, upDirection);
        }
    }
    
    private Vector3 GetUpDirection()
    {
        return (transform.position - planetCenter).normalized;
    }
    
    private void ToggleCameraMode()
    {
        isFirstPerson = !isFirstPerson;
        
        // Show/hide player visual
        if (playerVisual != null)
        {
            playerVisual.gameObject.SetActive(!isFirstPerson);
        }
        
        Debug.Log($"📹 Camera mode: {(isFirstPerson ? "First Person" : "Third Person")}");
    }
    
    // Public methods for debug display
    public Vector3 GetPosition() => transform.position;
    public Vector3 GetVelocity() => rb.velocity;
    public bool IsGrounded() => isGrounded;
    public float GetDistanceFromCenter() => Vector3.Distance(transform.position, planetCenter);
}