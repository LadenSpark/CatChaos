using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(Collider2D))]
public class FallenObject : MonoBehaviour
{
    [Header("Scoring & Visuals")]
    public int dogHitPoints = 50;
    public Sprite messSprite;
    public Vector3 messScale = new Vector3(1f, 1f, 1f);

    [Header("Animations")]
    public string floorHitAnimTrigger = "HitFloor";
    public string dogHitAnimTrigger = "HitDog";
    
    [Header("Fall & Tumble Settings")]
    public float tumbleSpin = 200f; 

    [Header("Autonomous Respawn Settings")]
    [Tooltip("How long the object stays a mess on the floor before fading out.")]
    public float messDuration = 4.0f;
    [Tooltip("How long the object freezes on the dog before fading out.")]
    public float dogHitDuration = 0.5f;
    [Tooltip("How long the fade-in and fade-out animations take.")]
    public float fadeDuration = 1.5f;
    [Tooltip("How long the shelf remains empty before the item begins fading back in.")]
    public float emptyShelfDuration = 3.0f;
    [Tooltip("How fast it blinks while fading in/out.")]
    public float flashSpeed = 15f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D myCollider;
    private Animator anim;
    
    private bool hasLanded = false;
    private bool isLaunched = false;
    
    // Prevents the cat from swatting the item while it is fading in
    [HideInInspector] public bool isSpawning = false;

    // Saved starting states
    private Sprite originalSprite;
    private Vector3 originalScale;
    private string originalTag;
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    // Track the active coroutine so the Human can interrupt it!
    private Coroutine currentRespawnRoutine;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        myCollider = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();

        originalSprite = spriteRenderer.sprite;
        originalScale = transform.localScale;
        originalTag = gameObject.tag;
        originalPosition = transform.position;
        originalRotation = transform.rotation;

        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    public void KnockOff(float catFacingDirectionX)
    {
        if (isLaunched || hasLanded || isSpawning) return; 
        isLaunched = true;
        
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = Vector2.zero; 
        rb.angularVelocity = -catFacingDirectionX * tumbleSpin;

        StartCoroutine(FallThroughPlatformBriefly());
    }

    private IEnumerator FallThroughPlatformBriefly()
    {
        Vector2 boxCenter = (Vector2)myCollider.bounds.center - new Vector2(0, myCollider.bounds.extents.y + 0.1f);
        Vector2 boxSize = new Vector2(myCollider.bounds.size.x * 0.8f, 0.2f);
        
        Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f);
        List<Collider2D> ignoredShelves = new List<Collider2D>();

        foreach (Collider2D hit in hits)
        {
            if (hit != myCollider && !hit.isTrigger && !hit.CompareTag("floor") && !hit.CompareTag("dog"))
            {
                Physics2D.IgnoreCollision(myCollider, hit, true);
                ignoredShelves.Add(hit);
            }
        }
            
        yield return new WaitForSeconds(0.4f);
            
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

        if (hitObject.CompareTag("floor"))
        {
            BecomeMess();
        }
        else if (hitObject.CompareTag("dog"))
        {
            HitDog();
        }
    }

    private void BecomeMess()
    {
        hasLanded = true;
        gameObject.tag = "mess"; 

        if (anim != null && !string.IsNullOrEmpty(floorHitAnimTrigger)) anim.SetTrigger(floorHitAnimTrigger);
        if (messSprite != null) spriteRenderer.sprite = messSprite;
        
        transform.localScale = messScale;

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        transform.rotation = Quaternion.identity;

        if (GameManager.Instance != null)
        {
            // GameManager.Instance.AddRisk(transform.position);
        }

        // NEW: Tell the Human directly that we made a mess and pass our position!
        // Tell the GameManager directly that we made a mess and pass our position!
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddChaosFromMess(transform.position);
        }

        // Start autonomous sequence with standard delays
        currentRespawnRoutine = StartCoroutine(RespawnSequence(messDuration, emptyShelfDuration));
    }

    private void HitDog()
    {
        hasLanded = true;
        Debug.Log($"Bonk! Hit the dog. Awarding {dogHitPoints} points!");
        
        if (anim != null && !string.IsNullOrEmpty(dogHitAnimTrigger)) anim.SetTrigger(dogHitAnimTrigger);

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        // Start autonomous sequence with shorter dog delay
        currentRespawnRoutine = StartCoroutine(RespawnSequence(dogHitDuration, emptyShelfDuration));
    }

    // --- NEW: Called by the Human to force an immediate reset ---
    public void ForceRespawn()
    {
        // If it is already sitting cleanly on the shelf, ignore it
        if (!hasLanded && !isLaunched && !isSpawning) return;

        // Cancel the current slow timer
        if (currentRespawnRoutine != null)
        {
            StopCoroutine(currentRespawnRoutine);
        }

        // Start the sequence with ZERO delay so it cleans up instantly!
        currentRespawnRoutine = StartCoroutine(RespawnSequence(0f, 0f));
    }

    private IEnumerator RespawnSequence(float initialWait, float shelfWait)
    {
        // 1. Wait (Will be skipped if forced by Human)
        if (initialWait > 0f) yield return new WaitForSeconds(initialWait);

        myCollider.enabled = false;

        // 2. Flash and Fade Out
        Color origColor = spriteRenderer.color;
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeDuration;
            
            float lerpAlpha = Mathf.Lerp(1f, 0f, progress);
            float flashAlpha = lerpAlpha * (Mathf.PingPong(timer * flashSpeed, 1f) + 0.2f);
            
            spriteRenderer.color = new Color(origColor.r, origColor.g, origColor.b, Mathf.Clamp01(flashAlpha));
            yield return null;
        }
        
        spriteRenderer.color = new Color(origColor.r, origColor.g, origColor.b, 0f);

        // 3. Wait while shelf is empty (Will be skipped if forced by Human)
        if (shelfWait > 0f) yield return new WaitForSeconds(shelfWait);

        // 4. Lock interactions and restore physical state
        isSpawning = true;
        gameObject.tag = originalTag;
        spriteRenderer.sprite = originalSprite;
        transform.localScale = originalScale;
        transform.position = originalPosition;
        transform.rotation = originalRotation;

        if (anim != null) 
        {
            anim.Rebind();
            anim.Update(0f);
        }

        // 5. Flash and Fade In
        timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeDuration;
            
            float lerpAlpha = Mathf.Lerp(0f, 1f, progress);
            float flashAlpha = lerpAlpha * (Mathf.PingPong(timer * flashSpeed, 1f) + 0.2f);
            
            spriteRenderer.color = new Color(origColor.r, origColor.g, origColor.b, Mathf.Clamp01(flashAlpha));
            yield return null;
        }
        
        spriteRenderer.color = new Color(origColor.r, origColor.g, origColor.b, 1f);

        // 6. Unlock interactions
        myCollider.enabled = true;
        hasLanded = false;
        isLaunched = false;
        isSpawning = false;
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
