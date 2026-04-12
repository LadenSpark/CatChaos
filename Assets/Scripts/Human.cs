using UnityEngine;
using System.Collections;

public class HumanCleaner : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float paceDistance = 4f;
    public float cleanDuration = 2f;

    [Header("Detection Settings")]
    public float viewDistance = 10f;
    public LayerMask catLayer;    // Set this to your 'Cat' layer
    public LayerMask groundLayer; // Set this to your 'Ground' layer
    public Transform catTransform;

    [Header("References")]
    public GameObject ladderVisual; // Optional: a ladder sprite to enable when climbing

    private Vector2 startPosition;
    private GameObject currentMess;
    private int paceDirection = 1;
    private bool isCleaning = false;
    private bool isClimbing = false;

    private enum State { Pacing, MovingToMess, Cleaning, SearchingForCat, Climbing }
    private State currentState = State.Pacing;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        switch (currentState)
        {
            case State.Pacing:
                PaceBackAndForth();
                FindNearestMess();
                break;

            case State.MovingToMess:
                MoveTowardsMess();
                break;

            case State.SearchingForCat:
                // Periodically check LoS while "looking around"
                if (CheckForCat())
                {
                    // Logic for when the cat is caught (e.g., Game Over or Chase)
                    Debug.Log("CAUGHT YOU!");
                }
                // Return to pacing after a brief search
                StartCoroutine(ResumePacingAfterDelay(2f));
                break;
        }
    }

    #region State Logic

    void PaceBackAndForth()
    {
        float leftLimit = startPosition.x - paceDistance;
        float rightLimit = startPosition.x + paceDistance;

        if (transform.position.x >= rightLimit) paceDirection = -1;
        if (transform.position.x <= leftLimit) paceDirection = 1;

        transform.Translate(Vector2.right * paceDirection * moveSpeed * Time.deltaTime);
        
        // Face the direction of movement
        transform.localScale = new Vector3(paceDirection, 1, 1);
    }

    void FindNearestMess()
    {
        GameObject[] messes = GameObject.FindGameObjectsWithTag("Mess");
        if (messes.Length > 0)
        {
            currentMess = messes[0]; // Logic could be expanded to find the closest
            currentState = State.MovingToMess;
        }
    }

    void MoveTowardsMess()
    {
        if (currentMess == null)
        {
            currentState = State.Pacing;
            return;
        }

        float distanceX = Mathf.Abs(transform.position.x - currentMess.transform.position.x);
        float distanceY = currentMess.transform.position.y - transform.position.y;

        // 1. Check if mess is on a higher platform
        if (distanceY > 1.5f && !isClimbing)
        {
            StartCoroutine(ClimbLadderRoutine());
            return;
        }

        // 2. Move horizontally to the mess
        if (distanceX > 0.5f)
        {
            float dir = (currentMess.transform.position.x > transform.position.x) ? 1 : -1;
            transform.Translate(Vector2.right * dir * moveSpeed * Time.deltaTime);
            transform.localScale = new Vector3(dir, 1, 1);
        }
        else if (!isCleaning && Mathf.Abs(distanceY) < 1f)
        {
            StartCoroutine(CleanMessRoutine());
        }
    }

    #endregion

    #region Coroutines

    IEnumerator CleanMessRoutine()
    {
        isCleaning = true;
        currentState = State.Cleaning;
        
        Debug.Log("Human: 'What a mess...' Cleaning...");
        yield return new WaitForSeconds(cleanDuration);
        
        if (currentMess != null) Destroy(currentMess);
        
        isCleaning = false;
        currentState = State.SearchingForCat;
    }

    IEnumerator ClimbLadderRoutine()
    {
        isClimbing = true;
        currentState = State.Climbing;

        Debug.Log("Human: 'I need the ladder for this.'");
        if (ladderVisual != null) ladderVisual.SetActive(true);

        Vector3 targetHeight = new Vector3(transform.position.x, currentMess.transform.position.y, transform.position.z);
        
        while (Vector3.Distance(transform.position, targetHeight) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetHeight, moveSpeed * 0.7f * Time.deltaTime);
            yield return null;
        }

        if (ladderVisual != null) ladderVisual.SetActive(false);
        isClimbing = false;
        currentState = State.MovingToMess;
    }

    IEnumerator ResumePacingAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (currentState == State.SearchingForCat)
        {
            currentState = State.Pacing;
        }
    }

    #endregion

    #region Public Helpers

    public bool CheckForCat()
    {
        if (catTransform == null) return false;

        Vector2 directionToCat = (catTransform.position - transform.position).normalized;
        float distanceToCat = Vector2.Distance(transform.position, catTransform.position);

        if (distanceToCat < viewDistance)
        {
            // Raycast checks Ground layer to see if walls block vision, and Cat layer for the target
            RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToCat, viewDistance, groundLayer | catLayer);

            if (hit.collider != null && ((1 << hit.collider.gameObject.layer) & catLayer) != 0)
            {
                Debug.DrawRay(transform.position, directionToCat * distanceToCat, Color.red);
                return true;
            }
        }

        Debug.DrawRay(transform.position, directionToCat * viewDistance, Color.green);
        return false;
    }

    #endregion
}