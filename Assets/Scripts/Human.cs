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

    [Header("Risk Settings")]
    public float riskMeter = 0f;
    public float maxRisk = 10f;

    [Header("Ladder & Climb Settings")]
    public Ladder[] ladders;
    public float climbSpeed = 2f;
    public float idleBeforeLadderDuration = 2f;
    public float lookAtTopDuration = 2f;
    public float stopAtLadderDistance = 0.3f;

    [Header("Time-Based Climb Settings")]
    [Tooltip("Minimum time in seconds between random ladder climbs (180 = 3 mins)")]
    public float minTimeBetweenClimbs = 180f;
    [Tooltip("Maximum time in seconds between random ladder climbs (300 = 5 mins)")]
    public float maxTimeBetweenClimbs = 300f;

    [Header("Animation References")]
    public string climbAnimBool = "IsClimbing";
    public string lookAnimTrigger = "Look";
    public string lookLeftAnimTrigger = "LookLeft";
    public string lookRightAnimTrigger = "LookRight";

    private Animator animator;
    private int paceDirection = 1;
    private bool isLadderSequenceActive = false;
    private bool isIdling = false;
    
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
        if (isLadderSequenceActive || isIdling) return;

        climbTimer += Time.deltaTime;

        if (riskMeter >= maxRisk)
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

        // ==========================================
        // NEW FIX: DISABLE GRAVITY WHILE CLIMBING
        // ==========================================
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        float originalGravity = 1f;
        if (rb != null)
        {
            originalGravity = rb.gravityScale;
            rb.gravityScale = 0f; // Turn off gravity!
            rb.linearVelocity = Vector2.zero; // Stop any downward momentum
        }

        Vector3 startY = transform.position;
        float targetY = chosenLadder.topPoint != null ? chosenLadder.topPoint.position.y : startY.y + 5f;
        Vector3 topTarget = new Vector3(transform.position.x, targetY, transform.position.z);

        SetAnimBool(climbAnimBool, true);
        while (Mathf.Abs(transform.position.y - topTarget.y) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, topTarget, climbSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = topTarget;
        SetAnimBool(climbAnimBool, false);

        bool lookRight = Random.value > 0.5f;
        SetFacingDirection(lookRight ? 1 : -1);

        TriggerAnim(lookAnimTrigger);
        if (lookRight) TriggerAnim(lookRightAnimTrigger);
        else TriggerAnim(lookLeftAnimTrigger);

        float lookTimer = 0f;
        while (lookTimer < lookAtTopDuration)
        {
            if (CheckForCat())
            {
                GameOver();
                yield break; 
            }
            lookTimer += Time.deltaTime;
            yield return null;
        }

        SetAnimBool(climbAnimBool, true);
        while (Mathf.Abs(transform.position.y - startY.y) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, startY, climbSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = startY;
        SetAnimBool(climbAnimBool, false);

        // ==========================================
        // NEW FIX: RESTORE GRAVITY
        // ==========================================
        if (rb != null)
        {
            rb.gravityScale = originalGravity; // Turn gravity back on!
        }

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

        climbTimer = 0f;
        nextClimbTime = Random.Range(minTimeBetweenClimbs, maxTimeBetweenClimbs);
        
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

    private void TriggerAnim(string paramName)
    {
        if (animator != null && !string.IsNullOrEmpty(paramName)) animator.SetTrigger(paramName);
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
        Debug.DrawRay(origin, dir * wallCheckDistance, hit.collider != null ? Color.red : Color.blue);
        return hit.collider != null;
    }

    public bool CheckForLedge(float direction)
    {
        if (groundLayer == 0) return false;
        Vector2 checkPos = ledgeCheck != null ? (Vector2)ledgeCheck.position : (Vector2)transform.position + new Vector2(direction * ledgeCheckOffset, 0f);
        RaycastHit2D hit = Physics2D.Raycast(checkPos, Vector2.down, ledgeCheckDistance, groundLayer);
        Debug.DrawRay(checkPos, Vector2.down * ledgeCheckDistance, hit.collider == null ? Color.yellow : Color.green);
        return hit.collider == null; 
    }

    public bool CheckForCat()
    {
        if (catTransform == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj == null) playerObj = GameObject.FindWithTag("Cat");
            if (playerObj != null) catTransform = playerObj.transform;
            else return false;
        }

        Vector2 origin = lineOfSight != null ? (Vector2)lineOfSight.position : (Vector2)transform.position;
        float facingDir = Mathf.Sign(transform.localScale.x);

        Vector2 dirToCat = (Vector2)catTransform.position - origin;
        float distanceToCat = dirToCat.magnitude;

        if (distanceToCat < viewDistance)
        {
            if (Mathf.Sign(dirToCat.x) == facingDir || distanceToCat < 0.5f)
            {
                Vector2 directionToCat = dirToCat.normalized;
                RaycastHit2D[] hits = Physics2D.RaycastAll(origin, directionToCat, viewDistance);
                foreach (RaycastHit2D hit in hits)
                {
                    if (hit.collider != null && hit.collider.gameObject != gameObject)
                    {
                        if (hit.collider.CompareTag("Cat") || hit.collider.CompareTag("Player"))
                        {
                            Debug.DrawRay(origin, directionToCat * distanceToCat, Color.red);
                            return true;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
        }

        Debug.DrawRay(origin, new Vector2(facingDir, 0f) * viewDistance, Color.green);
        return false;
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