using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(AudioSource))]
public class Dog : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 3f;
    public bool movingRight = true;
    
    [Tooltip("How frequently the dog randomly turns around (Ambitious = 1 to 4 seconds)")]
    public float minTurnTime = 1f;
    public float maxTurnTime = 4f;

    [Header("Detection Settings")]
    public float detectionDistance = 0.5f;
    public Transform wallCheck;
    public Transform ledgeCheck;
    public LayerMask groundLayer;

    [Header("Hit & Scoring Settings")]
    public int scoreValue = 50;
    public float hitStunDuration = 2f;

    [Header("Audio Settings")]
    public AudioClip randomBarkClip;
    public AudioClip hitBarkClip;
    public float minBarkTime = 5f;
    public float maxBarkTime = 10f;

    private Rigidbody2D rb;
    private Animator anim;
    private AudioSource audioSource;
    
    private bool isHit = false;
    private float turnTimer;
    private float barkTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        ResetTurnTimer();
        ResetBarkTimer();
    }

    void Update()
    {
        if (isHit) return; // Do nothing while stunned!

        // 1. Erratic Turnaround Logic
        turnTimer -= Time.deltaTime;
        if (turnTimer <= 0)
        {
            Flip();
            ResetTurnTimer();
        }

        // 2. Random Bark Logic
        barkTimer -= Time.deltaTime;
        if (barkTimer <= 0)
        {
            Bark(randomBarkClip);
            ResetBarkTimer();
        }
    }

    void FixedUpdate()
    {
        if (isHit)
        {
            // Lock him down completely while the hit animation plays
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        // 3. Constant Movement (No Idling!)
        float horizontalMove = movingRight ? speed : -speed;
        rb.linearVelocity = new Vector2(horizontalMove, rb.linearVelocity.y);

        // 4. Ledge & Wall Detection
        bool isWallAhead = Physics2D.Raycast(wallCheck.position, movingRight ? Vector2.right : Vector2.left, detectionDistance, groundLayer);
        bool isGroundAhead = Physics2D.Raycast(ledgeCheck.position, Vector2.down, detectionDistance, groundLayer);

        if (isWallAhead || !isGroundAhead)
        {
            Flip();
            ResetTurnTimer(); // Reset the erratic timer so he doesn't double-flip instantly
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check for falling objects and ensure we aren't already stunned
        if (!isHit && other.CompareTag("FallingObject")) 
        {
            StartCoroutine(HitRoutine());
        }
    }

    // A public method just in case your FallenObject script calls the hit directly instead of using triggers
    public void TakeHit()
    {
        if (!isHit) StartCoroutine(HitRoutine());
    }

    private IEnumerator HitRoutine()
    {
        isHit = true;
        rb.linearVelocity = Vector2.zero; // Slam on the brakes
        
        // Trigger the animation and audio
        if (anim != null) anim.SetBool("dogHit", true);
        Bark(hitBarkClip);
        
        // Call the GameManager to add points
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(scoreValue);
        }
        else
        {
            Debug.LogWarning("Dog: GameManager is missing! Cannot add score.");
        }

        // Wait for the stun duration
        yield return new WaitForSeconds(hitStunDuration);

        // Resume normal behavior
        if (anim != null) anim.SetBool("dogHit", false);
        isHit = false;
    }

    private void Flip()
    {
        movingRight = !movingRight;
        transform.eulerAngles = movingRight ? Vector3.zero : new Vector3(0, 180, 0);
    }

    private void ResetTurnTimer()
    {
        turnTimer = Random.Range(minTurnTime, maxTurnTime);
    }

    private void ResetBarkTimer()
    {
        barkTimer = Random.Range(minBarkTime, maxBarkTime);
    }

    private void Bark(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}