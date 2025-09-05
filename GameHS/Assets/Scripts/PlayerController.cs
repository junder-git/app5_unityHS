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
    
    private Vector3 velocity;
    private bool isGrounded;
    private float yaw, pitch;
    private Vector3 planetCenter;
    private float planetRadius;
    private bool isFirstPerson = false;
    
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
        
        // Lock cursor for mouse look
        Cursor.lockState = CursorLockMode.Locked;
        
        // Position player above planet surface
        Vector3 spawnPos = planetCenter + Vector3.up * (planetRadius + 20f);
        transform.position = spawnPos;
    }
    
    private void Update()
    {
        HandleInput();
        HandleMouseLook();
        ApplyMovement();
        ApplyGravity();
        CheckGroundContact();
        UpdateCamera();
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
            velocity += moveDirection * moveSpeed * Time.deltaTime;
        }
        
        // Jump
        if (jumpInput && isGrounded)
        {
            Vector3 upDirection = GetUpDirection();
            velocity += upDirection * jumpForce;
            isGrounded = false;
        }
    }
    
    private void ApplyGravity()
    {
        Vector3 gravityDirection = (planetCenter - transform.position).normalized;
        velocity += gravityDirection * gravityStrength * Time.deltaTime;
        
        // Apply velocity
        transform.position += velocity * Time.deltaTime;
        
        // Apply friction when grounded
        if (isGrounded)
        {
            velocity *= 0.9f;
        }
    }
    
    private void CheckGroundContact()
    {
        float distanceToCenter = Vector3.Distance(transform.position, planetCenter);
        float surfaceDistance = Mathf.Abs(distanceToCenter - planetRadius);
        
        isGrounded = surfaceDistance < 8f;
        
        // Prevent clipping through planet
        float minDistance = planetRadius + 3f;
        if (distanceToCenter < minDistance)
        {
            Vector3 pushDirection = (transform.position - planetCenter).normalized;
            transform.position = planetCenter + pushDirection * minDistance;
            
            // Remove velocity component going into planet
            float velocityIntoSurface = Vector3.Dot(velocity, -pushDirection);
            if (velocityIntoSurface > 0)
            {
                velocity -= pushDirection * velocityIntoSurface;
            }
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
    public Vector3 GetVelocity() => velocity;
    public bool IsGrounded() => isGrounded;
    public float GetDistanceFromCenter() => Vector3.Distance(transform.position, planetCenter);
}