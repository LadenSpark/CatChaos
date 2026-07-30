using UnityEngine;
using System.Collections;

public class Human : MonoBehaviour
{
    [Header("Movement & Pacing")]
    public float moveSpeed = 3f;
    [Range(0f, 1f)] public float idleChance = 0.3f; 
    public float minIdleTime = 1f;
    public float maxIdleTime = 3f;

    [Header("Edge & Wall Detection")]
    [Tooltip("Empty GameObject placed at the human's feet pointing forward to detect edges.")]
    public Transform ledgeCheck;
    [Tooltip("Empty GameObject placed at the human's front to detect walls.")]
    public Transform wallCheck;
    public float checkRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Vision & Target Settings")]
    [Tooltip("Drag the Cat GameObject here.")]
    public Transform catTransform;
    [Tooltip("How far can the human see on the shelf?")]
    public float viewDistance = 10f;
    [Tooltip("Set this to your Cat layer AND obstacle layers so furniture blocks vision.")]
    public LayerMask visionLayerMask;

    [Header("Ladder Settings")]
    public GameObject[] ladderLocations;
    public float ladderBottomHeightY = -2f;
    public float ladderMidHeightY = 1.5f; 
    public float ladderTopHeightY = 5f;

    private int paceDirection = 1;
    private bool isClimbingRoutineActive = false;
    private bool isIdling = false;
    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (isClimbingRoutineActive || isIdling) return;
        PaceBackAndForth();
    }

    void PaceBackAndForth()
    {
        // 1. Check for walls or edges ahead
        bool isWallAhead = wallCheck != null && Physics2D.OverlapCircle(wallCheck.position, checkRadius, groundLayer);
        bool isGroundAhead = ledgeCheck != null && Physics2D.OverlapCircle(ledgeCheck.position, checkRadius, groundLayer);

        // If he hits a wall or is about to walk off an edge, turn around!
        if (isWallAhead || !isGroundAhead)
        {
            FlipDirection();
            
            if (Random.value < idleChance)
            {
                StartCoroutine(IdleRoutine());
                return;
            }
        }

        // Random periodic idle check while walking
        if (Random.value < 0.002f) 
        {
            StartCoroutine(IdleRoutine());
            return;
        }

        // Move forward based on direction
        transform.Translate(Vector2.right * paceDirection * moveSpeed * Time.deltaTime);
        
        // Ensure idles are turned off so it naturally defaults back to walking
        if (anim != null) 
        {
            anim.SetBool("idleOne", false);
            anim.SetBool("idleTwo", false);
        }
    }

    void FlipDirection()
    {
        paceDirection *= -1;
        SetFacingDirection(paceDirection);
    }

    // NEW: Safely flips the sprite without destroying the original size scale you set in the Inspector!
    void SetFacingDirection(float direction)
    {
        Vector3 currentScale = transform.localScale;
        currentScale.x = Mathf.Abs(currentScale.x) * direction;
        transform.localScale = currentScale;
    }

    IEnumerator IdleRoutine()
    {
        isIdling = true;
        
        if (anim != null) 
        {
            // Flip a coin to choose which idle animation to play
            if (Random.value > 0.5f)
            {
                anim.SetBool("idleOne", true);
                anim.SetBool("idleTwo", false);
            }
            else
            {
                anim.SetBool("idleOne", false);
                anim.SetBool("idleTwo", true);
            }
        }

        float waitTime = Random.Range(minIdleTime, maxIdleTime);
        yield return new WaitForSeconds(waitTime);

        // After idling, random chance to turn around
        if (Random.value > 0.5f)
        {
            FlipDirection();
        }

        // Turn off both idle triggers when he finishes resting to return to default walk
        if (anim != null) 
        {
            anim.SetBool("idleOne", false);
            anim.SetBool("idleTwo", false);
        }
        
        isIdling = false;
    }

    // Called by the GameManager when Risk hits 10
    public void TriggerLadderSearch()
    {
        if (!isClimbingRoutineActive)
        {
            StopAllCoroutines(); 
            isIdling = false;
            StartCoroutine(ClimbAndSearchRoutine());
        }
    }

    IEnumerator ClimbAndSearchRoutine()
    {
        isClimbingRoutineActive = true;
        
        // Ensure idles are off before climbing
        if (anim != null) 
        {
            anim.SetBool("idleOne", false);
            anim.SetBool("idleTwo", false);
        }

        GameObject nearestLadder = GetNearestLadder();
        if (nearestLadder == null)
        {
            Debug.LogWarning("Human: 'Where are my ladders?!'");
            isClimbingRoutineActive = false;
            yield break; 
        }

        float targetX = nearestLadder.transform.position.x;

        // 1. Walk to the nearest ladder
        while (Mathf.Abs(transform.position.x - targetX) > 0.1f)
        {
            float dir = (targetX > transform.position.x) ? 1 : -1;
            transform.Translate(Vector2.right * dir * moveSpeed * Time.deltaTime);
            SetFacingDirection(dir);
            yield return null;
        }

        nearestLadder.SetActive(true);

        // 2. Randomly choose mid or top height
        float targetY = (Random.value > 0.5f) ? ladderTopHeightY : ladderMidHeightY;

        // 3. Climb Up
        if (anim != null) anim.SetBool("isClimbingUp", true);
        while (transform.position.y < targetY)
        {
            transform.position = Vector3.MoveTowards(transform.position, new Vector3(transform.position.x, targetY, transform.position.z), moveSpeed * Time.deltaTime);
            yield return null;
        }
        if (anim != null) anim.SetBool("isClimbingUp", false);

        // 4. Randomly look left or right
        float lookDirection = (Random.value > 0.5f) ? 1f : -1f;
        SetFacingDirection(lookDirection);
        
        if (anim != null) anim.SetBool("isLooking", true); 

        // 5. Look around and actively scan for 4 seconds
        float searchTimer = 0f;
        bool catFound = false;

        while (searchTimer < 4f)
        {
            if (CheckForCat(lookDirection))
            {
                catFound = true;
                break; 
            }
            searchTimer += Time.deltaTime;
            yield return null;
        }

        if (anim != null) anim.SetBool("isLooking", false);

        // 6. Resolve the search
        if (catFound)
        {
            Debug.Log("Human: 'THERE YOU ARE, YOU LITTLE RASCAL!' (Game Over!)");
            yield break; 
        }

        // 7. Cat not found? Climb down
        if (anim != null) anim.SetBool("isClimbingDown", true);
        while (transform.position.y > ladderBottomHeightY)
        {
            transform.position = Vector3.MoveTowards(transform.position, new Vector3(transform.position.x, ladderBottomHeightY, transform.position.z), moveSpeed * Time.deltaTime);
            yield return null;
        }
        if (anim != null) anim.SetBool("isClimbingDown", false);

        // 8. Reset and cleanup
        nearestLadder.SetActive(false);
        CleanupMesses();
        
        isClimbingRoutineActive = false;
    }

    private bool CheckForCat(float lookDirection)
    {
        if (catTransform == null) return false;

        Vector2 rayDirection = new Vector2(lookDirection, 0);
        RaycastHit2D hit = Physics2D.Raycast(transform.position, rayDirection, viewDistance, visionLayerMask);

        Debug.DrawRay(transform.position, rayDirection * viewDistance, Color.red);

        if (hit.collider != null && hit.collider.CompareTag("Cat"))
        {
            return true;
        }

        return false;
    }

    private void CleanupMesses()
    {
        GameObject[] messesToClean = GameObject.FindGameObjectsWithTag("mess");
        foreach (var mess in messesToClean)
        {
            Destroy(mess);
        }
    }

    private GameObject GetNearestLadder()
    {
        GameObject nearest = null;
        float minDistance = Mathf.Infinity;
        foreach (GameObject ladder in ladderLocations)
        {
            if (ladder != null)
            {
                float dist = Vector2.Distance(transform.position, ladder.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest = ladder;
                }
            }
        }
        return nearest;
    }
}
// Legacy
// using UnityEngine;
// using System.Collections;

// public class HumanCleaner : MonoBehaviour
// {
//     [Header("Movement Settings")]
//     public float moveSpeed = 3f;
//     public float paceDistance = 4f;
//     public float cleanDuration = 2f;

//     [Header("Detection Settings")]
//     public float viewDistance = 10f;
//     public LayerMask catLayer;    // Set this to your 'Cat' layer
//     public LayerMask groundLayer; // Set this to your 'Ground' layer
//     public Transform catTransform;

//     [Header("References")]
//     public GameObject ladderVisual; // Optional: a ladder sprite to enable when climbing

//     private Vector2 startPosition;
//     private GameObject currentMess;
//     private int paceDirection = 1;
//     private bool isCleaning = false;
//     private bool isClimbing = false;

//     private enum State { Pacing, MovingToMess, Cleaning, SearchingForCat, Climbing }
//     private State currentState = State.Pacing;

//     void Start()
//     {
//         startPosition = transform.position;
//     }

//     void Update()
//     {
//         switch (currentState)
//         {
//             case State.Pacing:
//                 PaceBackAndForth();
//                 FindNearestMess();
//                 break;

//             case State.MovingToMess:
//                 MoveTowardsMess();
//                 break;

//             case State.SearchingForCat:
//                 // Periodically check LoS while "looking around"
//                 if (CheckForCat())
//                 {
//                     // Logic for when the cat is caught (e.g., Game Over or Chase)
//                     Debug.Log("CAUGHT YOU!");
//                 }
//                 // Return to pacing after a brief search
//                 StartCoroutine(ResumePacingAfterDelay(2f));
//                 break;
//         }
//     }

//     #region State Logic

//     void PaceBackAndForth()
//     {
//         float leftLimit = startPosition.x - paceDistance;
//         float rightLimit = startPosition.x + paceDistance;

//         if (transform.position.x >= rightLimit) paceDirection = -1;
//         if (transform.position.x <= leftLimit) paceDirection = 1;

//         transform.Translate(Vector2.right * paceDirection * moveSpeed * Time.deltaTime);
        
//         // Face the direction of movement
//         transform.localScale = new Vector3(paceDirection, 1, 1);
//     }

//     void FindNearestMess()
//     {
//         GameObject[] messes = GameObject.FindGameObjectsWithTag("Mess");
//         if (messes.Length > 0)
//         {
//             currentMess = messes[0]; // Logic could be expanded to find the closest
//             currentState = State.MovingToMess;
//         }
//     }

//     void MoveTowardsMess()
//     {
//         if (currentMess == null)
//         {
//             currentState = State.Pacing;
//             return;
//         }

//         float distanceX = Mathf.Abs(transform.position.x - currentMess.transform.position.x);
//         float distanceY = currentMess.transform.position.y - transform.position.y;

//         // 1. Check if mess is on a higher platform
//         if (distanceY > 1.5f && !isClimbing)
//         {
//             StartCoroutine(ClimbLadderRoutine());
//             return;
//         }

//         // 2. Move horizontally to the mess
//         if (distanceX > 0.5f)
//         {
//             float dir = (currentMess.transform.position.x > transform.position.x) ? 1 : -1;
//             transform.Translate(Vector2.right * dir * moveSpeed * Time.deltaTime);
//             transform.localScale = new Vector3(dir, 1, 1);
//         }
//         else if (!isCleaning && Mathf.Abs(distanceY) < 1f)
//         {
//             StartCoroutine(CleanMessRoutine());
//         }
//     }

//     #endregion

//     #region Coroutines

//     IEnumerator CleanMessRoutine()
//     {
//         isCleaning = true;
//         currentState = State.Cleaning;
        
//         Debug.Log("Human: 'What a mess...' Cleaning...");
//         yield return new WaitForSeconds(cleanDuration);
        
//         if (currentMess != null) Destroy(currentMess);
        
//         isCleaning = false;
//         currentState = State.SearchingForCat;
//     }

//     IEnumerator ClimbLadderRoutine()
//     {
//         isClimbing = true;
//         currentState = State.Climbing;

//         Debug.Log("Human: 'I need the ladder for this.'");
//         if (ladderVisual != null) ladderVisual.SetActive(true);

//         Vector3 targetHeight = new Vector3(transform.position.x, currentMess.transform.position.y, transform.position.z);
        
//         while (Vector3.Distance(transform.position, targetHeight) > 0.1f)
//         {
//             transform.position = Vector3.MoveTowards(transform.position, targetHeight, moveSpeed * 0.7f * Time.deltaTime);
//             yield return null;
//         }

//         if (ladderVisual != null) ladderVisual.SetActive(false);
//         isClimbing = false;
//         currentState = State.MovingToMess;
//     }

//     IEnumerator ResumePacingAfterDelay(float delay)
//     {
//         yield return new WaitForSeconds(delay);
//         if (currentState == State.SearchingForCat)
//         {
//             currentState = State.Pacing;
//         }
//     }

//     #endregion

//     #region Public Helpers

//     public bool CheckForCat()
//     {
//         if (catTransform == null) return false;

//         Vector2 directionToCat = (catTransform.position - transform.position).normalized;
//         float distanceToCat = Vector2.Distance(transform.position, catTransform.position);

//         if (distanceToCat < viewDistance)
//         {
//             // Raycast checks Ground layer to see if walls block vision, and Cat layer for the target
//             RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToCat, viewDistance, groundLayer | catLayer);

//             if (hit.collider != null && ((1 << hit.collider.gameObject.layer) & catLayer) != 0)
//             {
//                 Debug.DrawRay(transform.position, directionToCat * distanceToCat, Color.red);
//                 return true;
//             }
//         }

//         Debug.DrawRay(transform.position, directionToCat * viewDistance, Color.green);
//         return false;
//     }

//     #endregion
// }