using UnityEngine;
using TMPro; // Only if using TextMeshPro

public class ScrollingCredits : MonoBehaviour
{
    [Header("Scrolling Settings")]
    public float scrollSpeed = 50f; // Units per second
    public float endYPosition = 1000f; // Y position at which credits stop
    public bool loop = false; // Should credits loop?

    private Vector3 startPosition;

    void Start()
    {
        // Save starting position
        startPosition = transform.localPosition;
    }

    void Update()
    {
        // Move credits upward
        transform.localPosition += Vector3.up * scrollSpeed * Time.deltaTime;

        // Check if we've reached the end
        if (transform.localPosition.y >= endYPosition)
        {
            if (loop)
            {
                // Reset to start
                transform.localPosition = startPosition;
            }
            else
            {
                // Stop scrolling
                enabled = false;
            }
        }
    }
}
