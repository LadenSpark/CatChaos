using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Human : MonoBehaviour
{
    public static Human Instance { get; private set; }

    [Header("Movement & Pacing")]
    public float moveSpeed = 3f;
    [Range(0f, 1f)] public float idleChance = 0.3f; 
    public float minIdleTime = 1f;
    public float maxIdleTime = 3f;

    [Header("Detection Settings")]
    public float viewDistance = 10f;
    public LayerMask groundLayer; 
    public Transform catTransform;
    public Transform lineOfSight;
    public Transform wallCheck;
    public Transform ledgeCheck;
    public float wallCheckDistance = 0.5f;
    public float ledgeCheckOffset = 0.5f;
    public float ledgeCheckDistance = 1.0f;

    [Header("Ladder & Climb Settings")]
    public Ladder[] ladders;
    public float climbSpeed = 2f;
    public float idleBeforeLadderDuration = 2f;
    public float lookAtTopDuration = 2f;
    public float stopAtLadderDistance = 0.3f;

    [Header("Time-Based Climb Settings")]
    [Tooltip("Minimum time in seconds between random ladder climbs")]
    public float minTimeBetweenClimbs = 180f;
    [Tooltip("Maximum time in seconds between random ladder climbs")]
    public float maxTimeBetweenClimbs = 300f;

    [Header("Animation References")]
    public string climbUpAnimBool = "IsClimbingUp";
    public string climbDownAnimBool = "IsClimbingDown";
    public string lookLeftAnimBool = "IsLookingLeft";
    public string lookRightAnimBool = "IsLookingRight";

    private Animator animator;
    private int paceDirection = 1;
    private bool isLadderSequenceActive = false;
    private bool isIdling = false;
    private bool isClimbingRungs = false; // NEW: Tracks when he is physically on the ladder
    
    private float climbTimer = 0f;
    private float nextClimbTime = 180f;
    
    private Coroutine currentIdleRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        animator = GetComponent<Animator>();
        nextClimbTime = Random.Range(minTimeBetweenClimbs, maxTimeBetweenClimbs);
    }

    void Update()
    {
        // 1. ALWAYS check for the cat FIRST, unless actively climbing up/down the rungs!
        if (!isClimbingRungs)
        {
            if (CheckForCat()) return; // Stop doing anything else if cat is spotted
        }

        // 2. If he is doing a ladder sequence or resting, don't pace.
        if (isLadderSequenceActive || isIdling) return;

        climbTimer += Time.deltaTime;

        if (GameManager.Instance != null && GameManager.Instance.chaosLevel >= GameManager.Instance.maxChaos)
        {
            StartCoroutine(LadderSequenceRoutine(false)); 
            return;
        }
        else if (climbTimer >= nextClimbTime)
        {
            StartCoroutine(LadderSequenceRoutine(true)); 
            return;
        }

        PaceBackAndForth();
    }

    #region Movement & Pacing Logic

    void PaceBackAndForth()
    {
        bool hitWall = CheckForWall(paceDirection);
        bool hitLedge = CheckForLedge(paceDirection); 

        if (hitWall || hitLedge)
        {
            FlipDirection();
            
            if (Random.value < idleChance)
            {
                currentIdleRoutine = StartCoroutine(IdleRoutine(Random.Range(minIdleTime, maxIdleTime)));
                return;
            }
        }

        if (Random.value < 0.002f) 
        {
            currentIdleRoutine = StartCoroutine(IdleRoutine(Random.Range(minIdleTime, maxIdleTime)));
            return;
        }

        transform.position += new Vector3(paceDirection * moveSpeed * Time.deltaTime, 0f, 0f);
        
        if (animator != null) 
        {
            animator.SetBool("idleOne", false);
            animator.SetBool("idleTwo", false);
        }
    }

    void FlipDirection()
    {
        paceDirection *= -1;
        SetFacingDirection(paceDirection);
    }

    void SetFacingDirection(float direction)
    {
        Vector3 currentScale = transform.localScale;
        currentScale.x = Mathf.Abs(currentScale.x) * direction;
        transform.localScale = currentScale;
    }

    IEnumerator IdleRoutine(float waitTime)
    {
        isIdling = true;
        
        if (animator != null) 
        {
            if (Random.value > 0.5f)
            {
                animator.SetBool("idleOne", true);
                animator.SetBool("idleTwo", false);
            }
            else
            {
                animator.SetBool("idleOne", false);
                animator.SetBool("idleTwo", true);
            }
        }

        yield return new WaitForSeconds(waitTime);

        if (Random.value > 0.5f)
        {
            FlipDirection();
        }

        if (animator != null) 
        {
            animator.SetBool("idleOne", false);
            animator.SetBool("idleTwo", false);
        }
        
        isIdling = false;
    }

    #endregion

    #region Ladder Sequence Logic

    private IEnumerator LadderSequenceRoutine(bool isTimeTriggered)
    {
        isLadderSequenceActive = true;
        climbTimer = 0f;
        nextClimbTime = Random.Range(minTimeBetweenClimbs, maxTimeBetweenClimbs);
        
        if (currentIdleRoutine != null)
        {
            StopCoroutine(currentIdleRoutine);
            currentIdleRoutine = null;
        }
        isIdling = false;
        
        if (animator != null) 
        {
            animator.SetBool("idleOne", false);
            animator.SetBool("idleTwo", false);
        }

        if (ladders == null || ladders.Length == 0) ladders = FindObjectsByType<Ladder>(FindObjectsSortMode.None);

        if (ladders == null || ladders.Length == 0)
        {
            isLadderSequenceActive = false;
            yield break;
        }

        Ladder chosenLadder = GetNearestLadder();
        if (chosenLadder == null)
        {
            isLadderSequenceActive = false;
            yield break;
        }

        chosenLadder.StartFlashing();
        yield return StartCoroutine(IdleRoutine(idleBeforeLadderDuration));

        Vector3 ladderPos = chosenLadder.transform.position;
        float stuckTimer = 0f;
        float lastX = transform.position.x;

        // Walk to the ladder (Vision is still ON here!)
        while (Mathf.Abs(transform.position.x - ladderPos.x) > stopAtLadderDistance)
        {
            float dir = Mathf.Sign(ladderPos.x - transform.position.x);
            SetFacingDirection(dir); 

            transform.position += new Vector3(dir * moveSpeed * Time.deltaTime, 0f, 0f);
            
            if (Mathf.Abs(transform.position.x - lastX) < 0.001f)
            {
                stuckTimer += Time.deltaTime;
                if (stuckTimer > 0.5f)
                {
                    Debug.LogWarning("Human stuck! Snapping out of infinite loop.");
                    break;
                }
            }
            else
            {
                stuckTimer = 0f;
                lastX = transform.position.x;
            }

            yield return null;
        }

        chosenLadder.StopFlashing();
        
        transform.position = new Vector3(ladderPos.x, transform.position.y, transform.position.z);

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        float originalGravity = 1f;
        if (rb != null)
        {
            originalGravity = rb.gravityScale;
            rb.gravityScale = 0f; 
            rb.linearVelocity = Vector2.zero; 
        }

        Vector3 startY = transform.position;
        float targetY = chosenLadder.topPoint != null ? chosenLadder.topPoint.position.y : startY.y + 5f;
        Vector3 topTarget = new Vector3(transform.position.x, targetY, transform.position.z);

        // Climb Up
        isClimbingRungs = true; // NEW: Turns off vision while climbing
        SetAnimBool(climbUpAnimBool, true);
        while (Mathf.Abs(transform.position.y - topTarget.y) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, topTarget, climbSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = topTarget;
        SetAnimBool(climbUpAnimBool, false);
        isClimbingRungs = false; // NEW: Turns vision back on at the top!

        // Look Left or Right
        bool lookRight = Random.value > 0.5f;
        SetFacingDirection(lookRight ? 1 : -1);

        if (lookRight) SetAnimBool(lookRightAnimBool, true);
        else SetAnimBool(lookLeftAnimBool, true);

        float lookTimer = 0f;
        while (lookTimer < lookAtTopDuration)
        {
            // Update() handles the vision check now, so we just wait here!
            lookTimer += Time.deltaTime;
            yield return null;
        }

        // Done looking, turn off the bool
        if (lookRight) SetAnimBool(lookRightAnimBool, false);
        else SetAnimBool(lookLeftAnimBool, false);

        // Climb Down
        isClimbingRungs = true; // NEW: Turns off vision while climbing down
        SetAnimBool(climbDownAnimBool, true);
        while (Mathf.Abs(transform.position.y - startY.y) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, startY, climbSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = startY;
        SetAnimBool(climbDownAnimBool, false);
        isClimbingRungs = false; // NEW: Turns vision back on when feet hit the ground

        if (rb != null)
        {
            rb.gravityScale = originalGravity; 
        }

        SetFacingDirection(paceDirection);

        chosenLadder.FlashAndFadeOut(1f);

        FallenObject[] allFallenObjects = FindObjectsByType<FallenObject>(FindObjectsSortMode.None);
        foreach (FallenObject obj in allFallenObjects)
        {
            obj.ForceRespawn(); 
        }

        if (isTimeTriggered)
        {
            if (GameManager.Instance != null) GameManager.Instance.ReduceChaosByHalf();
        }
        else
        {
            if (GameManager.Instance != null) GameManager.Instance.ResetChaos();
        }
        
        isLadderSequenceActive = false;
    }

    private void GameOver()
    {
        Debug.Log("Human: 'THERE YOU ARE, YOU LITTLE RASCAL!' (Game Over!)");
    }

    private void SetAnimBool(string paramName, bool value)
    {
        if (animator != null && !string.IsNullOrEmpty(paramName)) animator.SetBool(paramName, value);
    }

    #endregion

    #region Public Helpers (Raycasts & Detection)

    private Ladder GetNearestLadder()
    {
        Ladder nearest = null;
        float minDistanceX = Mathf.Infinity;
        
        foreach (Ladder ladder in ladders)
        {
            if (ladder != null)
            {
                float distX = Mathf.Abs(transform.position.x - ladder.transform.position.x);
                if (distX < minDistanceX)
                {
                    minDistanceX = distX;
                    nearest = ladder;
                }
            }
        }

        return nearest;
    }

    public bool CheckForWall(float direction)
    {
        if (groundLayer == 0) return false;
        Vector2 origin = wallCheck != null ? (Vector2)wallCheck.position : (Vector2)transform.position;
        Vector2 dir = new Vector2(direction, 0f);
        RaycastHit2D hit = Physics2D.Raycast(origin, dir, wallCheckDistance, groundLayer);
        return hit.collider != null;
    }

    public bool CheckForLedge(float direction)
    {
        if (groundLayer == 0) return false;
        Vector2 checkPos = ledgeCheck != null ? (Vector2)ledgeCheck.position : (Vector2)transform.position + new Vector2(direction * ledgeCheckOffset, 0f);
        RaycastHit2D hit = Physics2D.Raycast(checkPos, Vector2.down, ledgeCheckDistance, groundLayer);
        return hit.collider == null; 
    }

    public bool CheckForCat()
    {
        if (catTransform == null)
        {
            GameObject catObj = GameObject.FindWithTag("cat");
            if (catObj != null) catTransform = catObj.transform;
            else return false;
        }

        Vector2 origin = lineOfSight != null ? (Vector2)lineOfSight.position : (Vector2)transform.position;
        float facingDir = Mathf.Sign(transform.localScale.x);

        // 1. THE FIX: Shoot the vision laser STRICTLY horizontal. No more diagonal heat-seeking!
        Vector2 visionDirection = new Vector2(facingDir, 0f);

        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, visionDirection, viewDistance);

        // 2. Sort hits by distance so the closest physical object is evaluated first
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit2D hit in hits)
        {
            // 3. Ignore the human itself and invisible triggers
            if (hit.collider != null && hit.collider.gameObject != gameObject && !hit.collider.isTrigger)
            {
                if (hit.collider.CompareTag("cat"))
                {
                    // The horizontal ray hit the cat!
                    Debug.DrawRay(origin, visionDirection * hit.distance, Color.red, 2f);
                    if (GameManager.Instance != null) GameManager.Instance.TriggerGameOver();
                    return true;
                }
                else
                {
                    // Hit a solid obstacle (like a wall, or the dropped item) BEFORE the cat.
                    // Because it's a straight horizontal line, the dropped item now safely blocks his vision!
                    break;
                }
            }
        }

        // Green debug laser to show exactly where he is looking
        Debug.DrawRay(origin, visionDirection * viewDistance, Color.green);
        return false;
    }

    #endregion

    #region Bulletproof Gizmo Debugging

    private void OnDrawGizmos()
    {
        Vector2 origin = lineOfSight != null ? (Vector2)lineOfSight.position : (Vector2)transform.position;
        float facingDir = transform.localScale.x != 0 ? Mathf.Sign(transform.localScale.x) : 1f;

        // 1. ALWAYS draw the GREEN forward vision ray
        Gizmos.color = Color.green;
        Gizmos.DrawRay(origin, new Vector2(facingDir, 0f) * viewDistance);

        // 2. If the cat is found in the scene, draw a YELLOW tracking line directly to it
        if (catTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(origin, catTransform.position); 
        }
    }

    #endregion
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