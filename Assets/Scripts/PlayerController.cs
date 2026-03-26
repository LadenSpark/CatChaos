using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class PlayerController : MonoBehaviour
{

    [Header("Climb Positioning")]
    [SerializeField] private Vector2 climbOffset = new Vector2(0.5f, 1.5f); // Horizontal and Vertical landing offset
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 12f;
    
    [Header("Detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.1f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Ledge Hang Logic")]
    [SerializeField] private Transform ledgeDetector; // Assign your Empty here
    [SerializeField] private LayerMask ledgeLayer;    // Set to your "Ledge" layer
    [SerializeField] private float hangChance = 0.4f; // 40% base chance
    [SerializeField] private float luckMagnitude = 0.2f; // +20% bonus
    [SerializeField] private int tapsRequired = 5;    // Taps to climb up
    [SerializeField] private float hangMaxTime = 2.0f; // Max time before falling

    public Collider2D myCollider;
    private Rigidbody2D rb;
    private bool isGrounded;
    
    // Hanging State Variables
    private bool isHanging = false;
    private bool hasAttemptedHang = false; // Prevents re-grabbing the same ledge instantly
    private int currentTaps = 0;
    private float hangTimer;

    // Knock Object Variables
    public float interactionRange = 2f;
    public LayerMask interactableLayer;
    private bool isFacingRight;

    void Awake() 
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true; 
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Update()
    {
        if (isHanging)
        {
            HandleHanging();
            return; // Skip normal movement while hanging
        }

        HandleMovement();
        CheckLedgeDetection();

        if (Input.GetKeyDown(KeyCode.Q))
        {
            Debug.Log("gonna knock it!");
            AttemptInteraction();
        }
    }

    private void AttemptInteraction()
    {
        // 1. Determine direction based on localScale.x
        float facingDirection = transform.localScale.x > 0 ? 1 : -1;
        Vector2 rayDirection = Vector2.left * facingDirection;

        // 2. OFFSET: Start the ray slightly in front of the player's center 
        // This prevents the ray from hitting the Player's own collider.
        Vector2 rayStart = (Vector2)transform.position + (rayDirection * 0.5f);

        // 3. DEBUG: This line MUST point toward your object in the Scene View
        Debug.DrawRay(rayStart, rayDirection * interactionRange, Color.green, 1.0f);

        // 4. Perform the Raycast
        RaycastHit2D hit = Physics2D.Raycast(rayStart, rayDirection, interactionRange, interactableLayer);

        if (hit.collider != null)
        {
            FallenObject fallenItem = hit.collider.GetComponent<FallenObject>();
            
            if (fallenItem != null)
            {
                Debug.Log("Hit: " + hit.collider.name);
                fallenItem.KnockOff(rayDirection);
            }
        }
        else
        {
            // If you see the Green line touching the object but get this message:
            // Check your LayerMask and Collider2D types!
            Debug.Log("No interactable object in range.");
        }
    }

    private void HandleMovement()
    {
        
        float moveInput = 0;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) moveInput = -1;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) moveInput = 1;
        if (moveInput > 0 && !isFacingRight)
        {
            Flip();
        }
        else if (moveInput < 0 && isFacingRight)
        {
            Flip();
        }

        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (isGrounded) hasAttemptedHang = false; // Reset hang ability when touching ground

        if ((Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W)) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    private void CheckLedgeDetection()
    {
        // Only check if we are in the air and haven't tried this ledge yet
        if (!isGrounded && !hasAttemptedHang && ledgeDetector != null)
        {
            bool hittingLedge = Physics2D.OverlapCircle(ledgeDetector.position, 0.15f, ledgeLayer);

            if (hittingLedge)
            {
                hasAttemptedHang = true;
                
                // Random Roll + Magnitude
                float roll = Random.value; 
                if (roll <= (hangChance + luckMagnitude))
                {
                    StartHanging();
                }
            }
        }
    }

    private void StartHanging()
    {
        isHanging = true;
        currentTaps = 0;
        hangTimer = hangMaxTime;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0; // Freeze in place
    }

    private void HandleHanging()
    {
        hangTimer -= Time.deltaTime;

        // Visual feedback for tapping
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W))
        {
            currentTaps++;
            // Optional: Add a tiny "jitter" effect here to show effort
        }

        // Success: Climb Up
        if (currentTaps >= tapsRequired)
        {
            ExecuteClimb();
        }

        // Failure: Out of time or let go
        if (hangTimer <= 0)
        {
            ExitHanging(false);
        }
    }

    private void ExecuteClimb()
    {
        isHanging = false;
        rb.gravityScale = 3f; // Restore gravity

        // Calculate landing position: Current ledge position + offset
        // We use the ledgeDetector's X to decide which way to 'hop' onto the platform
        float direction = transform.localScale.x > 0 ? 1 : -1;
        Vector3 landingPos = new Vector3(
            ledgeDetector.position.x + (climbOffset.x * direction), 
            ledgeDetector.position.y + climbOffset.y, 
            transform.position.z
        );

        // Teleport/Move to the top of the ledge
        transform.position = landingPos;

        // Add a small forward burst of speed so the transition isn't static
        rb.linearVelocity = new Vector2(direction * moveSpeed * 0.5f, 2f);

        Debug.Log("Climb Successful: Landed on ledge.");
    }

    private void ExitHanging(bool climbed)
    {
        // This handles the 'Failure' case (falling off)
        isHanging = false;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 3f; 
        
        if (!climbed)
        {
            Debug.Log("Fell off the ledge!");
            // Optional: push the player slightly away from the wall so they don't re-grab immediately
            rb.linearVelocity = new Vector2(-transform.localScale.x * 2f, 0);
        }
    }

    private void Flip()
    {
        // Switch the way the player is labelled as facing
        isFacingRight = !isFacingRight;

        // Multiply the player's x local scale by -1
        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;
    }

    // private void HandleHanging()
    // {
    //     hangTimer -= Time.deltaTime;

    //     // Visual feedback for tapping
    //     if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W))
    //     {
    //         currentTaps++;
    //     }

    //     // Success: Climb Up
    //     if (currentTaps >= tapsRequired)
    //     {
    //         ExitHanging(true);
    //     }

    //     // Failure: Out of time
    //     if (hangTimer <= 0)
    //     {
    //         ExitHanging(false);
    //     }
    // }

    // private void ExitHanging(bool climbed)
    // {
    //     isHanging = false;
    //     rb.gravityScale = 3f; // Return to normal gravity (adjust to your default)
        
    //     if (climbed)
    //     {
    //         // Small boost up and forward
    //         rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * 0.8f);
    //     }
    // }
}


// using UnityEngine;

// [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
// public class PlayerController : MonoBehaviour
// {
//     [Header("Movement Settings")]
//     [SerializeField] private float moveSpeed = 8f;
//     [SerializeField] private float jumpForce = 12f;
    
//     [Header("Detection")]
//     [SerializeField] private Transform groundCheck;
//     [SerializeField] private float groundCheckRadius = 0.1f;
//     [SerializeField] private LayerMask groundLayer;

//     public Collider2D myCollider;
//     private Rigidbody2D rb;
//     private float horizontalInput;
//     private bool isGrounded;

//     void Awake() 
//     {
//         rb = GetComponent<Rigidbody2D>();
//         // Lock rotation so player doesn't tip over
//         rb.freezeRotation = true; 
//         // Use Interpolate for smooth camera follow
//         rb.interpolation = RigidbodyInterpolation2D.Interpolate;
//     }

//     void Update()
//     {
        
//         // 1. Horizontal Movement (Legacy GetKey)
//         float moveInput = 0;
//         if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) moveInput = -1;
//         if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) moveInput = 1;
        

//         // Apply velocity
//         rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

//         // 2. Jumping (Legacy GetKeyDown)
//         isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

//         if ((Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W)) && myCollider.IsTouchingLayers(LayerMask.GetMask("ground")))
//         {
//             rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
//         }
//         // For new input sytem when implemented...
//         // horizontalInput = Input.GetAxisRaw("Horizontal");
//         // if (Input.GetButtonDown("Jump") && isGrounded)
//         // {
//         //     rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
//         // }
//     }

//     bool TouchingGround()
//     {
//         ContactFilter2D filter = new ContactFilter2D();
//         filter.SetLayerMask(LayerMask.GetMask("Ground")); // Optional: using Layers is faster than Tags
        
//         return myCollider.IsTouchingLayers(LayerMask.GetMask("Ground"));
//     }

// }