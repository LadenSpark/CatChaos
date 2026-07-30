using UnityEngine;

public class ScrollingCredits : MonoBehaviour
{
    [Header("Scrolling Settings")]
    public float scrollSpeed = 50f;
    public float endYPosition = 1000f;
    public bool loop = false;

    [Header("Interaction Settings")]
    public float resumeDelay = 3f;

    private Vector3 startPosition;
    private bool isTouching = false;
    private float resumeTimer = 0f;
    private Vector2 lastTouchPosition;

    void Start()
    {
        startPosition = transform.localPosition;
    }

    void Update()
    {
        HandleInput();

        // Only auto-scroll if not touching and the delay timer has expired
        if (!isTouching)
        {
            if (resumeTimer > 0)
            {
                resumeTimer -= Time.deltaTime;
            }
            else
            {
                AutoScroll();
            }
        }

        CheckBounds();
    }

    void HandleInput()
    {
        // Mouse/Touch Down
        if (Input.GetMouseButtonDown(0))
        {
            isTouching = true;
            lastTouchPosition = Input.mousePosition;
            resumeTimer = resumeDelay; // Set delay for when we eventually release
        }

        // Mouse/Touch Held (Dragging)
        if (Input.GetMouseButton(0))
        {
            Vector2 currentTouchPosition = Input.mousePosition;
            float deltaY = currentTouchPosition.y - lastTouchPosition.y;
            
            // Move the credits based on drag delta
            transform.localPosition += Vector3.up * deltaY;
            lastTouchPosition = currentTouchPosition;
        }

        // Mouse/Touch Up
        if (Input.GetMouseButtonUp(0))
        {
            isTouching = false;
            resumeTimer = resumeDelay; // Start the 3-second countdown
        }
    }

    void AutoScroll()
    {
        transform.localPosition += Vector3.up * scrollSpeed * Time.deltaTime;
    }

    void CheckBounds()
    {
        if (transform.localPosition.y >= endYPosition)
        {
            if (loop)
            {
                transform.localPosition = startPosition;
            }
            else
            {
                // We don't disable the script here anymore so input still works 
                // but we clamp it so it doesn't fly off forever
                Vector3 pos = transform.localPosition;
                pos.y = endYPosition;
                transform.localPosition = pos;
            }
        }
    }
}
