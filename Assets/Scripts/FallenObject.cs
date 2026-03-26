using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class FallenObject : MonoBehaviour
{
    [Header("Launch Settings")]
    [Tooltip("X is the forward punch, Y is a slight upward hop.")]
    public Vector2 launchPower = new Vector2(5f, 2f); 
    public float ignorePlatformTime = 0.5f;
    
    [Header("References")]
    [Tooltip("Assign the platform this object is currently sitting on.")]
    public Collider2D platformToIgnore; 

    private Rigidbody2D rb;
    private Collider2D myCollider;
    private Vector3 originalPosition;
    private bool isLaunched = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<Collider2D>();
        originalPosition = transform.position;

        // Start as Kinematic so it doesn't fall until the player interacts
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true; 
    }

    /// <summary>
    /// Called by the PlayerController to trigger the fall.
    /// </summary>
    /// <param name="playerFacingDirection">Vector2.right or Vector2.left</param>
    public void KnockOff(Vector2 playerFacingDirection)
    {
        if (isLaunched) return; 
        
        isLaunched = true;
        
        // 1. Enable Physics
        rb.bodyType = RigidbodyType2D.Dynamic;

        // 2. Apply the 'Knock' force based on player direction
        Vector2 force = new Vector2(playerFacingDirection.x * launchPower.x, launchPower.y);
        rb.linearVelocity = force;

        // 3. Temporarily ignore the platform it's standing on
        if (platformToIgnore != null)
        {
            StartCoroutine(FallThroughPlatform());
        }
    }

    IEnumerator FallThroughPlatform()
    {
        Physics2D.IgnoreCollision(myCollider, platformToIgnore, true);
        yield return new WaitForSeconds(ignorePlatformTime);
        Physics2D.IgnoreCollision(myCollider, platformToIgnore, false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Reset if it hits the floor
        if (other.CompareTag("Floor"))
        {
            Debug.Log("Object hit floor. Resetting...");
            ResetObject();
        }
        
        // Interaction with the dog
        if (other.CompareTag("Dog"))
        {
            Debug.Log("Hit the dog! Good job!");
            // You can call a ScoreManager here
        }
    }

    public void ResetObject()
    {
        isLaunched = false;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        transform.position = originalPosition;
        transform.rotation = Quaternion.identity;
    }
}
