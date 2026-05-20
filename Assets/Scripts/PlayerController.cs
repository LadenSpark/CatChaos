using UnityEngine;
using TouchControlsKit;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Climb Positioning")]
    [SerializeField] private Vector2 climbOffset = new Vector2(0.5f, 1.5f); 
    
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 12f;
    
    [Header("Detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask whatIsGround; // CLICK DROPDOWN IN INSPECTOR AND SELECT LAYERS

    [Header("Ledge Hang Logic")]
    [SerializeField] private Transform ledgeDetector; 
    [SerializeField] private LayerMask ledgeLayer;    
    [SerializeField] private float hangChance = 0.4f; 
    [SerializeField] private float luckMagnitude = 0.2f; 
    [SerializeField] private int tapsRequired = 5;    
    [SerializeField] private float hangMaxTime = 2.0f; 

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

    public float interactionRange = 2f;
    public LayerMask interactableLayer;

    void Awake() 
    {
        catAnim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true; 
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        defaultGravity = rb.gravityScale; // Captures original jump physics
        catAnim.SetBool("catSwipe", false);
    }

    void Update()
    {
        if (isHanging)
        {
            HandleHanging();
            return; 
        }

        if (catAnim.GetBool("catSwipe") == false) //don't let the cat move if it's swiping. if it has momentum, it will slide.
        HandleMovement();
        CheckLedgeDetection();

        if (isGrounded && catAnim.GetBool("catSwipe") == false) //don't swipe if we're swiping already or airborne 
        if (Input.GetKeyDown(KeyCode.Q) || TCKInput.GetButtonDown(interactButtonIdentifier))
        {
            catAnim.SetBool("catSwipe", true);
            AttemptInteraction();
        }
    }

    private void CatSwipeConclusion() //called by the animation when the swipe ends, necessary to lock in place for the duration of the swipe
    {
        catAnim.SetBool("catSwipe", false);
    }

    private void HandleMovement()
    {
            // 1. Prioritize Joystick
            float moveInput = TCKInput.GetAxis(joystickIdentifier, EAxisType.Horizontal);

            // 2. Fallback to Keyboard if Joystick is idle
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

            // 3. Flip Logic (Ass-Backward Fix)
            if (moveInput > 0.1f && !isFacingRight) Flip();
            else if (moveInput < -0.1f && isFacingRight) Flip();

            rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

            // 4. Ground Check (Multi-Layer)
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround);
            if (isGrounded)
            {
                catAnim.SetBool("Catjump", false);
                hasAttemptedHang = false;
            }
            else catAnim.SetBool("Catjump", true);

            // 5. Jump (Velocity reset for height consistency)
            bool jumpPressed = Input.GetKeyDown(KeyCode.Space) ||
                               Input.GetKeyDown(KeyCode.W) ||
                               TCKInput.GetButtonDown(jumpButtonIdentifier);
                
            if (jumpPressed && isGrounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }
    }

    private void AttemptInteraction()
    {
        float facingDirection = isFacingRight ? 1f : -1f;
        Vector2 rayDirection = new Vector2(facingDirection, 0);
        Vector2 rayStart = (Vector2)transform.position + (rayDirection * 0.5f);

        RaycastHit2D hit = Physics2D.Raycast(rayStart, rayDirection, interactionRange, interactableLayer);
        if (hit.collider != null)
        {
            // Uses SendMessage to trigger the knock-off logic on the object
            hit.collider.SendMessage("KnockOff", rayDirection, SendMessageOptions.DontRequireReceiver);
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
        rb.gravityScale = defaultGravity; // Restores jump height
        float direction = isFacingRight ? 1 : -1;
        Vector3 landingPos = new Vector3(ledgeDetector.position.x + (climbOffset.x * direction), ledgeDetector.position.y + climbOffset.y, transform.position.z);
        transform.position = landingPos;
        rb.linearVelocity = new Vector2(direction * moveSpeed * 0.5f, 2f);
    }

    private void ExitHanging(bool climbed)
    {
        isHanging = false;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = defaultGravity; // Restores jump height
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