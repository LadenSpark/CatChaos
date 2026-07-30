using UnityEngine;
using System.Collections;
using TouchControlsKit;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class CatController : MonoBehaviour
{
    [Header("Climb Positioning")]
    [SerializeField] private Vector2 climbOffset = new Vector2(0.5f, 1.5f); 
    
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 12f;

    [Header("Airborne Tilt")]
    [Tooltip("How many degrees the cat tilts forward when falling.")]
    [SerializeField] private float maxFallTilt = 25f; 
    [Tooltip("How fast the cat tilts up and down.")]
    [SerializeField] private float tiltSpeed = 10f;
    
    [Header("Detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask whatIsGround; 

    [Header("Ledge Hang Logic")]
    [SerializeField] private Transform ledgeDetector; 
    [SerializeField] private LayerMask ledgeLayer;    
    [SerializeField] private float hangChance = 0.4f; 
    [SerializeField] private float luckMagnitude = 0.2f; 
    [SerializeField] private int tapsRequired = 5;    
    [SerializeField] private float hangMaxTime = 2.0f; 

    [Header("Combat/Interaction")]
    [Tooltip("Drag your physical PawHitbox child object here from the Hierarchy.")]
    public GameObject pawHitbox;

    [Header("Touch Control Identifiers")]
    [SerializeField] private string joystickIdentifier = "Joystick0"; 
    [SerializeField] private string jumpButtonIdentifier = "buttonJump"; 
    [SerializeField] private string interactButtonIdentifier = "buttonInteract";

    [Header("Visuals")]
    [SerializeField] private bool isFacingRight = true; 

    private Rigidbody2D rb;
    private Animator catAnim;
    private bool isGrounded;
    private bool isHanging = false;
    private bool hasAttemptedHang = false; 
    private int currentTaps = 0;
    private float hangTimer;
    private float defaultGravity;
    
    // NEW: We use this to only deal damage during the tiny swipe window
    private bool isSwiping = false;

    void Awake() 
    {
        catAnim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true; 
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        defaultGravity = rb.gravityScale; 
        catAnim.SetBool("catSwipe", false);

        // We now leave the paw hitbox ON all the time!
        if (pawHitbox != null) pawHitbox.SetActive(true);
    }

    void Update()
    {
        // Run the tilt math every frame so it can smoothly recover when landing
        HandleTilt();

        if (isHanging)
        {
            HandleHanging();
            return; 
        }

        if (catAnim.GetBool("catSwipe") == false) 
        {
            HandleMovement();
            CheckLedgeDetection();
        }

        // Swipe logic
        if (isGrounded && catAnim.GetBool("catSwipe") == false) 
        {
            if (Input.GetKeyDown(KeyCode.Q) || TCKInput.GetButtonDown(interactButtonIdentifier))
            {
                catAnim.SetBool("catSwipe", true);
                StartCoroutine(SwipeRoutine());
            }
        }
    }

    private void CatSwipeConclusion() 
    {
        catAnim.SetBool("catSwipe", false);
    }

    private void HandleMovement()
    {
        float moveInput = TCKInput.GetAxis(joystickIdentifier, EAxisType.Horizontal);

        if (Mathf.Abs(moveInput) < 0.01f)
        {
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                catAnim.SetBool("walk", true);
                moveInput = -1;
            }
            else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                catAnim.SetBool("walk", true);
                moveInput = 1;
            }
            else catAnim.SetBool("walk", false);
        }
        else catAnim.SetBool("walk", true);

        if (moveInput > 0.1f && !isFacingRight) Flip();
        else if (moveInput < -0.1f && isFacingRight) Flip();

        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround);
        if (isGrounded)
        {
            catAnim.SetBool("Catjump", false);
            hasAttemptedHang = false;
        }
        else catAnim.SetBool("Catjump", true);

        bool jumpPressed = Input.GetKeyDown(KeyCode.Space) ||
                           Input.GetKeyDown(KeyCode.W) ||
                           TCKInput.GetButtonDown(jumpButtonIdentifier);
            
        if (jumpPressed && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    private void HandleTilt()
    {
        float targetZ = 0f;

        // If we are actively falling (negative Y velocity) and not grabbing a ledge
        if (!isHanging && !isGrounded && rb.linearVelocity.y < -0.5f)
        {
            // If facing left, the scale is -1, so we invert the rotation angle to pitch the nose downward properly
            targetZ = isFacingRight ? -maxFallTilt : maxFallTilt;
        }

        // Smoothly rotate the cat towards the target angle
        Quaternion targetRotation = Quaternion.Euler(0, 0, targetZ);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * tiltSpeed);
    }

    private IEnumerator SwipeRoutine()
    {
        // Wait a tiny bit so the hit registers exactly as the paw swings forward in the animation
        yield return new WaitForSeconds(0.15f);

        // Turn on the "damage window"
        isSwiping = true;

        // Keep it active for a brief fraction of a second to register the hit
        yield return new WaitForSeconds(0.1f);

        // Turn it back off so you don't keep knocking things over just by standing near them
        isSwiping = false;
    }

    // This catches objects even if they are just resting quietly inside the hitbox when you press swipe.
    private void OnTriggerStay2D(Collider2D other)
    {
        // Only run the hit logic if the player is currently in the active swipe window
        if (isSwiping && other.CompareTag("FallingObject"))
        {
            FallenObject fallenObj = other.GetComponent<FallenObject>();
            if (fallenObj != null)
            {
                float direction = isFacingRight ? 1f : -1f;
                fallenObj.KnockOff(direction);
                Debug.Log("Swat! Cat knocked over: " + other.gameObject.name);
            }
        }
    }

    private void CheckLedgeDetection()
    {
        if (!isGrounded && !hasAttemptedHang && ledgeDetector != null)
        {
            bool hittingLedge = Physics2D.OverlapCircle(ledgeDetector.position, 0.15f, ledgeLayer);
            if (hittingLedge)
            {
                hasAttemptedHang = true;
                if (Random.value <= (hangChance + luckMagnitude)) StartHanging();
            }
        }
    }

    private void StartHanging()
    {
        isHanging = true;
        currentTaps = 0;
        hangTimer = hangMaxTime;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0; 
    }

    private void HandleHanging()
    {
        hangTimer -= Time.deltaTime;
        bool tapPressed = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || TCKInput.GetButtonDown(jumpButtonIdentifier);
        if (tapPressed) currentTaps++;
        if (currentTaps >= tapsRequired) ExecuteClimb();
        if (hangTimer <= 0) ExitHanging(false);
    }

    private void ExecuteClimb()
    {
        isHanging = false;
        rb.gravityScale = defaultGravity; 
        float direction = isFacingRight ? 1 : -1;
        Vector3 landingPos = new Vector3(ledgeDetector.position.x + (climbOffset.x * direction), ledgeDetector.position.y + climbOffset.y, transform.position.z);
        transform.position = landingPos;
        rb.linearVelocity = new Vector2(direction * moveSpeed * 0.5f, 2f);
    }

    private void ExitHanging(bool climbed)
    {
        isHanging = false;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = defaultGravity; 
        if (!climbed) rb.linearVelocity = new Vector2(isFacingRight ? -2f : 2f, 0);
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;
    }
}


// using UnityEngine;
// using System.Collections;
// using TouchControlsKit;

// [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
// public class CatController : MonoBehaviour
// {
//     [Header("Climb Positioning")]
//     [SerializeField] private Vector2 climbOffset = new Vector2(0.5f, 1.5f); 
    
//     [Header("Movement Settings")]
//     [SerializeField] private float moveSpeed = 8f;
//     [SerializeField] private float jumpForce = 12f;
    
//     [Header("Detection")]
//     [SerializeField] private Transform groundCheck;
//     [SerializeField] private float groundCheckRadius = 0.2f;
//     [SerializeField] private LayerMask whatIsGround; 

//     [Header("Ledge Hang Logic")]
//     [SerializeField] private Transform ledgeDetector; 
//     [SerializeField] private LayerMask ledgeLayer;    
//     [SerializeField] private float hangChance = 0.4f; 
//     [SerializeField] private float luckMagnitude = 0.2f; 
//     [SerializeField] private int tapsRequired = 5;    
//     [SerializeField] private float hangMaxTime = 2.0f; 

//     [Header("Combat/Interaction")]
//     [Tooltip("Drag your physical PawHitbox child object here from the Hierarchy.")]
//     public GameObject pawHitbox;

//     [Header("Touch Control Identifiers")]
//     [SerializeField] private string joystickIdentifier = "Joystick0"; 
//     [SerializeField] private string jumpButtonIdentifier = "buttonJump"; 
//     [SerializeField] private string interactButtonIdentifier = "buttonInteract";

//     [Header("Visuals")]
//     [SerializeField] private bool isFacingRight = true; 

//     private Rigidbody2D rb;
//     private Animator catAnim;
//     private bool isGrounded;
//     private bool isHanging = false;
//     private bool hasAttemptedHang = false; 
//     private int currentTaps = 0;
//     private float hangTimer;
//     private float defaultGravity;
    
//     // NEW: We use this to only deal damage during the tiny swipe window
//     private bool isSwiping = false;

//     void Awake() 
//     {
//         catAnim = GetComponent<Animator>();
//         rb = GetComponent<Rigidbody2D>();
//         rb.freezeRotation = true; 
//         rb.interpolation = RigidbodyInterpolation2D.Interpolate;
//         defaultGravity = rb.gravityScale; 
//         catAnim.SetBool("catSwipe", false);

//         // We now leave the paw hitbox ON all the time!
//         if (pawHitbox != null) pawHitbox.SetActive(true);
//     }

//     void Update()
//     {
//         if (isHanging)
//         {
//             HandleHanging();
//             return; 
//         }

//         if (catAnim.GetBool("catSwipe") == false) 
//         {
//             HandleMovement();
//             CheckLedgeDetection();
//         }

//         // Swipe logic
//         if (isGrounded && catAnim.GetBool("catSwipe") == false) 
//         {
//             if (Input.GetKeyDown(KeyCode.Q) || TCKInput.GetButtonDown(interactButtonIdentifier))
//             {
//                 catAnim.SetBool("catSwipe", true);
//                 StartCoroutine(SwipeRoutine());
//             }
//         }
//     }

//     private void CatSwipeConclusion() 
//     {
//         catAnim.SetBool("catSwipe", false);
//     }

//     private void HandleMovement()
//     {
//         float moveInput = TCKInput.GetAxis(joystickIdentifier, EAxisType.Horizontal);

//         if (Mathf.Abs(moveInput) < 0.01f)
//         {
//             if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
//             {
//                 catAnim.SetBool("walk", true);
//                 moveInput = -1;
//             }
//             else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
//             {
//                 catAnim.SetBool("walk", true);
//                 moveInput = 1;
//             }
//             else catAnim.SetBool("walk", false);
//         }
//         else catAnim.SetBool("walk", true);

//         if (moveInput > 0.1f && !isFacingRight) Flip();
//         else if (moveInput < -0.1f && isFacingRight) Flip();

//         rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

//         isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround);
//         if (isGrounded)
//         {
//             catAnim.SetBool("Catjump", false);
//             hasAttemptedHang = false;
//         }
//         else catAnim.SetBool("Catjump", true);

//         bool jumpPressed = Input.GetKeyDown(KeyCode.Space) ||
//                            Input.GetKeyDown(KeyCode.W) ||
//                            TCKInput.GetButtonDown(jumpButtonIdentifier);
            
//         if (jumpPressed && isGrounded)
//         {
//             rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
//         }
//     }

//     private IEnumerator SwipeRoutine()
//     {
//         // Wait a tiny bit so the hit registers exactly as the paw swings forward in the animation
//         yield return new WaitForSeconds(0.15f);

//         // Turn on the "damage window"
//         isSwiping = true;

//         // Keep it active for a brief fraction of a second to register the hit
//         yield return new WaitForSeconds(0.1f);

//         // Turn it back off so you don't keep knocking things over just by standing near them
//         isSwiping = false;
//     }

//     // CHANGED to OnTriggerStay2D! 
//     // This catches objects even if they are just resting quietly inside the hitbox when you press swipe.
//     private void OnTriggerStay2D(Collider2D other)
//     {
//         // Only run the hit logic if the player is currently in the active swipe window
//         if (isSwiping && other.CompareTag("FallingObject"))
//         {
//             FallenObject fallenObj = other.GetComponent<FallenObject>();
//             if (fallenObj != null)
//             {
//                 float direction = isFacingRight ? 1f : -1f;
//                 fallenObj.KnockOff(direction);
//                 Debug.Log("Swat! Cat knocked over: " + other.gameObject.name);
//             }
//         }
//     }

//     private void CheckLedgeDetection()
//     {
//         if (!isGrounded && !hasAttemptedHang && ledgeDetector != null)
//         {
//             bool hittingLedge = Physics2D.OverlapCircle(ledgeDetector.position, 0.15f, ledgeLayer);
//             if (hittingLedge)
//             {
//                 hasAttemptedHang = true;
//                 if (Random.value <= (hangChance + luckMagnitude)) StartHanging();
//             }
//         }
//     }

//     private void StartHanging()
//     {
//         isHanging = true;
//         currentTaps = 0;
//         hangTimer = hangMaxTime;
//         rb.linearVelocity = Vector2.zero;
//         rb.gravityScale = 0; 
//     }

//     private void HandleHanging()
//     {
//         hangTimer -= Time.deltaTime;
//         bool tapPressed = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || TCKInput.GetButtonDown(jumpButtonIdentifier);
//         if (tapPressed) currentTaps++;
//         if (currentTaps >= tapsRequired) ExecuteClimb();
//         if (hangTimer <= 0) ExitHanging(false);
//     }

//     private void ExecuteClimb()
//     {
//         isHanging = false;
//         rb.gravityScale = defaultGravity; 
//         float direction = isFacingRight ? 1 : -1;
//         Vector3 landingPos = new Vector3(ledgeDetector.position.x + (climbOffset.x * direction), ledgeDetector.position.y + climbOffset.y, transform.position.z);
//         transform.position = landingPos;
//         rb.linearVelocity = new Vector2(direction * moveSpeed * 0.5f, 2f);
//     }

//     private void ExitHanging(bool climbed)
//     {
//         isHanging = false;
//         rb.bodyType = RigidbodyType2D.Dynamic;
//         rb.gravityScale = defaultGravity; 
//         if (!climbed) rb.linearVelocity = new Vector2(isFacingRight ? -2f : 2f, 0);
//     }

//     private void Flip()
//     {
//         isFacingRight = !isFacingRight;
//         Vector3 theScale = transform.localScale;
//         theScale.x *= -1;
//         transform.localScale = theScale;
//     }
// }