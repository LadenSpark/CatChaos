using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Added for the List<>

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(Collider2D))]
public class FallenObject : MonoBehaviour
{
    [Header("Scoring & Visuals")]
    [Tooltip("Points awarded if this object hits the dog.")]
    public int dogHitPoints = 50;
    
    [Tooltip("The sprite this object turns into when it hits the floor.")]
    public Sprite messSprite;
    
    [Tooltip("How big the mess should be when it hits the floor (X, Y, Z).")]
    public Vector3 messScale = new Vector3(1f, 1f, 1f);

    [Header("Animations")]
    [Tooltip("Animator trigger name when hitting the floor/becoming a mess.")]
    public string floorHitAnimTrigger = "HitFloor";
    [Tooltip("Animator trigger name when hitting the dog.")]
    public string dogHitAnimTrigger = "HitDog";
    [Tooltip("How long to wait before destroying the object after freezing on the dog.")]
    public float dogDestroyDelay = 0.5f;

    [Header("Fall & Tumble Settings")]
    [Tooltip("How fast the object spins as it tips over the edge.")]
    public float tumbleSpin = 200f; 

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D myCollider;
    private Animator anim;
    
    private bool hasLanded = false;
    private bool isLaunched = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        myCollider = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();

        // Start as Kinematic so it rests peacefully on the shelf until the cat swats it
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    /// <summary>
    /// Called by CatController when swiped.
    /// </summary>
    public void KnockOff(float catFacingDirectionX)
    {
        if (isLaunched || hasLanded) return; 
        isLaunched = true;
        
        // 1. Switch to Dynamic so gravity pulls it straight down naturally
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = Vector2.zero; 

        // 2. Apply a realistic tumble spin away from the cat's swipe direction
        rb.angularVelocity = -catFacingDirectionX * tumbleSpin;

        // 3. Briefly ignore the platform it's sitting on so it doesn't get stuck
        StartCoroutine(FallThroughPlatformBriefly());
    }

    private IEnumerator FallThroughPlatformBriefly()
    {
        // FIX 1: Use bounds.center so it calculates perfectly no matter where the sprite's pivot is
        Vector2 boxCenter = (Vector2)myCollider.bounds.center - new Vector2(0, myCollider.bounds.extents.y + 0.1f);
        Vector2 boxSize = new Vector2(myCollider.bounds.size.x * 0.8f, 0.2f);
        
        // FIX 2: Use OverlapBoxAll to catch everything, preventing it from accidentally targeting itself!
        Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f);
        List<Collider2D> ignoredShelves = new List<Collider2D>();

        foreach (Collider2D hit in hits)
        {
            // If it found a solid object that isn't itself, the floor, or the dog... ignore it!
            if (hit != myCollider && !hit.isTrigger && !hit.CompareTag("floor") && !hit.CompareTag("dog"))
            {
                Physics2D.IgnoreCollision(myCollider, hit, true);
                ignoredShelves.Add(hit);
            }
        }
            
        // Wait a fraction of a second to clear the shelf geometry
        yield return new WaitForSeconds(0.4f);
            
        // Turn collisions back on in case we need them later
        foreach (Collider2D shelf in ignoredShelves)
        {
            if (shelf != null)
            {
                Physics2D.IgnoreCollision(myCollider, shelf, false);
            }
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleImpact(collision.gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HandleImpact(other.gameObject);
    }

    private void HandleImpact(GameObject hitObject)
    {
        if (hasLanded || !isLaunched) return;

        // Check if it hit the floor (lowercase)
        if (hitObject.CompareTag("floor"))
        {
            BecomeMess();
        }
        // Check if it hit the dog (lowercase)
        else if (hitObject.CompareTag("dog"))
        {
            HitDog();
        }
    }

    private void BecomeMess()
    {
        hasLanded = true;
        gameObject.tag = "mess"; 

        // Play the floor hit animation
        if (anim != null && !string.IsNullOrEmpty(floorHitAnimTrigger))
        {
            anim.SetTrigger(floorHitAnimTrigger);
        }

        if (messSprite != null)
        {
            spriteRenderer.sprite = messSprite;
        }

        // Apply the custom size from the Inspector!
        transform.localScale = messScale;

        // Lock it in place as a flat mess on the floor
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        transform.rotation = Quaternion.identity;

        // Tell GameManager to increase risk based on human awareness
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddRisk(transform.position);
        }
    }

    private void HitDog()
    {
        hasLanded = true;
        
        Debug.Log($"Bonk! Hit the dog. Awarding {dogHitPoints} points!");
        // TODO: ScoreManager.Instance.AddScore(dogHitPoints);

        // Play the dog hit animation
        if (anim != null && !string.IsNullOrEmpty(dogHitAnimTrigger))
        {
            anim.SetTrigger(dogHitAnimTrigger);
        }

        // Freeze in place right where it hit the dog
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        // Destroy after the animation delay
        Destroy(gameObject, dogDestroyDelay); 
    }
}

//Legacy
// using UnityEngine;
// using System.Collections;

// [RequireComponent(typeof(Rigidbody2D))]
// public class FallenObject : MonoBehaviour
// {
//     [Header("Launch Settings")]
//     [Tooltip("X is the forward punch, Y is a slight upward hop.")]
//     public Vector2 launchPower = new Vector2(5f, 2f); 
//     public float ignorePlatformTime = 0.5f;
    
//     [Header("References")]
//     [Tooltip("Assign the platform this object is currently sitting on.")]
//     public Collider2D platformToIgnore; 

//     private Rigidbody2D rb;
//     private Collider2D myCollider;
//     private Vector3 originalPosition;
//     private bool isLaunched = false;

//     void Start()
//     {
//         rb = GetComponent<Rigidbody2D>();
//         myCollider = GetComponent<Collider2D>();
//         originalPosition = transform.position;

//         // Start as Kinematic so it doesn't fall until the player interacts
//         rb.bodyType = RigidbodyType2D.Kinematic;
//         rb.useFullKinematicContacts = true; 
//     }

//     /// <summary>
//     /// Called by the PlayerController to trigger the fall.
//     /// </summary>
//     /// <param name="playerFacingDirection">Vector2.right or Vector2.left</param>
//     public void KnockOff(Vector2 playerFacingDirection)
//     {
//         if (isLaunched) return; 
        
//         isLaunched = true;
        
//         // 1. Enable Physics
//         rb.bodyType = RigidbodyType2D.Dynamic;

//         // 2. Apply the 'Knock' force based on player direction
//         Vector2 force = new Vector2(playerFacingDirection.x * launchPower.x, launchPower.y);
//         rb.linearVelocity = force;

//         // 3. Temporarily ignore the platform it's standing on
//         if (platformToIgnore != null)
//         {
//             StartCoroutine(FallThroughPlatform());
//         }
//     }

//     IEnumerator FallThroughPlatform()
//     {
//         Physics2D.IgnoreCollision(myCollider, platformToIgnore, true);
//         yield return new WaitForSeconds(ignorePlatformTime);
//         Physics2D.IgnoreCollision(myCollider, platformToIgnore, false);
//     }

//     private void OnTriggerEnter2D(Collider2D other)
//     {
//         // Reset if it hits the floor
//         if (other.CompareTag("Floor"))
//         {
//             Debug.Log("Object hit floor. Resetting...");
//             ResetObject();
//         }
        
//         // Interaction with the dog
//         if (other.CompareTag("Dog"))
//         {
//             Debug.Log("Hit the dog! Good job!");
//             // You can call a ScoreManager here
//             ResetObject();
//         }
//     }

//     public void ResetObject()
//     {
//         rb.gravityScale = 0;
//         isLaunched = false;
//         rb.bodyType = RigidbodyType2D.Kinematic;
//         rb.linearVelocity = Vector2.zero;
//         rb.angularVelocity = 0f;
//         transform.position = originalPosition;
//         transform.rotation = Quaternion.identity;
//     }
// }
