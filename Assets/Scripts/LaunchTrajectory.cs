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
    public string interactButton = "Fire1"; // Assigned button (e.g., Left Click/Ctrl)

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
        if (isPlayerNearby && Input.GetButtonDown(interactButton))
        {
            LaunchObject();
        }
    }

    public void LaunchObject()
    {
        Debug.Log("Object knocked!");
        
        // 1. Make it physical so it can fall
        rb.bodyType = RigidbodyType2D.Dynamic; 

        // 2. Ignore the platform so it falls through
        Physics2D.IgnoreCollision(objectCollider, platformCollider, true);
        
        // 3. Apply the launch force
        rb.linearVelocity = transform.right * launchVelocity;

        // 4. Start the timer to reset collisions
        StartCoroutine(RestoreCollision(platformCollider));
    }

    // Trigger detection instead of Collision so the player can "stand" near it
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isPlayerNearby = true;
            savedPlayerCollider = collision.collider;
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
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