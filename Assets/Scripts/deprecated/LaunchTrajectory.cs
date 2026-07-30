// using UnityEngine;
// using System.Collections;

// public class LaunchTracjectory : MonoBehaviour
// {
//     public Vector2 launchVelocity = new Vector2(3f, 3f);
//     public float ignoreTime = 0.5f;
//     public Collider2D platformCollider;

//     private Rigidbody2D rb;
//     private Collider2D objectCollider;

//     void Start()
//     {
//         rb = GetComponent<Rigidbody2D>();
//         objectCollider = GetComponent<Collider2D>();
//     }

//     void OnCollisionEnter2D(Collision2D collision)
//     {
//         if (collision.gameObject.CompareTag("Player"))
//         {
//             Debug.Log("Collision Detected with: " + collision.gameObject.name);
//             rb.linearVelocity = Vector2.zero; // Reset velocity before applying launch
//             rb.linearVelocity = transform.right * launchVelocity;

//             Collider2D playerCollider = collision.collider;
//             Physics2D.IgnoreCollision(objectCollider, playerCollider, true);
//             Physics2D.IgnoreCollision(objectCollider, platformCollider, true);
//             StartCoroutine(RestoreCollision(playerCollider));
//             StartCoroutine(RestoreCollision(platformCollider));
//         }
//     }

//     IEnumerator RestoreCollision(Collider2D otherCollider)
//     {
//         yield return new WaitForSeconds(ignoreTime);

//         Physics2D.IgnoreCollision(objectCollider, otherCollider, false);
//     }
// }

using UnityEngine;
using System.Collections;

public class LaunchTrajectory : MonoBehaviour
{
    public Vector2 launchVelocity = new Vector2(3f, 3f);
    public float ignoreTime = 0.5f;
    public Collider2D platformCollider;
    
    private Rigidbody2D rb;
    private Collider2D objectCollider;
    private bool isPlayerNearby = false;
    private Collider2D savedPlayerCollider;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        objectCollider = GetComponent<Collider2D>();
        
        // Ensure the object doesn't move until knocked
        rb.bodyType = RigidbodyType2D.Kinematic; 
        rb.linearVelocity = Vector2.zero;
    }

    void Update()
    {
        // Check for button press while player is touching/near the object
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.Q))
        {
            LaunchObject();
        }
    }

    public void LaunchObject()
    {
        Debug.Log("Object knocked!");
        
        // 1. Make it physical so it can fall
        rb.bodyType = RigidbodyType2D.Dynamic;

        // 1.1 enable gravity so that the object begins to fall instead of sitting in place above the shelf.
        // gravity is disabled when the object is reset in the FallenObject script.
        // we set it here to keep physics calculations together.
        rb.gravityScale = 2;

        // 2. Ignore the platform so it falls through
        Physics2D.IgnoreCollision(objectCollider, platformCollider, true);
        
        // 3. Apply the launch force
        rb.linearVelocity = transform.right * launchVelocity;

        // 4. Start the timer to reset collisions
        StartCoroutine(RestoreCollision(platformCollider));
    }

    // Trigger detection instead of Collision so the player can "stand" near it
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            isPlayerNearby = true;
            savedPlayerCollider = other.GetComponent<Collider2D>();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            isPlayerNearby = false;
        }
    }

    IEnumerator RestoreCollision(Collider2D otherCollider)
    {
        yield return new WaitForSeconds(ignoreTime);
        if (otherCollider != null)
        {
            Physics2D.IgnoreCollision(objectCollider, otherCollider, false);
        }
    }
}