using UnityEngine;
using Unity.Netcode;

public class EnemyPatrol : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 3f;
    [SerializeField] private bool movingRight = true;

    [Header("Detection")]
    [SerializeField] private float detectionDistance = 0.5f;
    [SerializeField] private Transform wallCheck;
    [SerializeField] private Transform ledgeCheck;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    void FixedUpdate()
    {
        // 1. Move the enemy
        float horizontalMove = movingRight ? speed : -speed;
        rb.linearVelocity = new Vector2(horizontalMove, rb.linearVelocity.y);

        // 2. Ledge & Wall Detection
        bool isWallAhead = Physics2D.Raycast(wallCheck.position, movingRight ? Vector2.right : Vector2.left, detectionDistance, groundLayer);
        bool isGroundAhead = Physics2D.Raycast(ledgeCheck.position, Vector2.down, detectionDistance, groundLayer);

        // 3. Flip Logic
        if (isWallAhead || !isGroundAhead)
        {
            Flip();
        }
    }

    private void Flip()
    {
        movingRight = !movingRight;
        transform.eulerAngles = movingRight ? Vector3.zero : new Vector3(0, 180, 0);
    }
}