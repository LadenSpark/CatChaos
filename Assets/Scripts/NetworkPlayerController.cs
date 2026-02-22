using UnityEngine;
using Unity.Netcode; // Required for Networking
using Unity.Netcode.Components; // Required for NetworkTransform

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(NetworkTransform))]
// Inherit from NetworkBehaviour instead of MonoBehaviour
public class NetworkPlayerController : NetworkBehaviour 
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 12f;
    
    [Header("Detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.1f;
    [SerializeField] private LayerMask groundLayer;

    public Collider2D myCollider;
    private Rigidbody2D rb;
    private float horizontalInput;
    private bool isGrounded;

    void Awake() 
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true; 
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Update()
    {

        // Current User runs update only
        if (!IsOwner) return;

        MovePlayer();
    }

    bool TouchingGround()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(LayerMask.GetMask("Ground")); // Optional: using Layers is faster than Tags

        return myCollider.IsTouchingLayers(LayerMask.GetMask("Ground"));
    }

    void MovePlayer()
    {
        // 1. Horizontal Movement (Legacy GetKey)
        float moveInput = 0;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) moveInput = -1;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) moveInput = 1;

        // Apply velocity
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        // 2. Jumping (Legacy GetKeyDown)
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if ((Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W)) && myCollider.IsTouchingLayers(LayerMask.GetMask("ground")))
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }


        // For new input sytem when implemented...
        // horizontalInput = Input.GetAxisRaw("Horizontal");
        // if (Input.GetButtonDown("Jump") && isGrounded)
        // {
        //     rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        // }
    }
   
}
