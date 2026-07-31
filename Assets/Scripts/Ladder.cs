using UnityEngine;
using System.Collections;

public class Ladder : MonoBehaviour
{
    [Header("Ladder Settings")]
    [Tooltip("Transform marking the exact top of the ladder.")]
    public Transform topPoint;
    [Tooltip("SpriteRenderer for flashing/fading effects. Auto-assigned if empty.")]
    public SpriteRenderer spriteRenderer;

    [Header("Flashing Settings")]
    public float flashSpeed = 6f;

    private bool isFlashing = false;
    private Color originalColor = Color.white;
    private Coroutine flashCoroutine;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    private void Start()
    {
        HideLadder();
    }

    /// <summary>
    /// Hides the ladder sprite renderer at runtime.
    /// </summary>
    public void HideLadder()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
    }

    /// <summary>
    /// Returns the exact climb height based on the topPoint's position.
    /// </summary>
    public float GetClimbHeight()
    {
        if (topPoint != null)
        {
            // Calculates the exact distance from the bottom to the top point
            return topPoint.position.y - transform.position.y;
        }
        
        Debug.LogWarning($"Ladder '{gameObject.name}' is missing its Top Point! The human won't know how high to climb.");
        return 0f;
    }

    /// <summary>
    /// Starts flashing the ladder sprite.
    /// </summary>
    public void StartFlashing()
    {
        gameObject.SetActive(true);
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    /// <summary>
    /// Stops flashing and restores normal ladder appearance.
    /// </summary>
    public void StopFlashing()
    {
        isFlashing = false;
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        if (spriteRenderer != null)
        {
            Color c = originalColor;
            c.a = 1f;
            spriteRenderer.color = c;
            spriteRenderer.enabled = true;
        }
    }

    private IEnumerator FlashRoutine()
    {
        isFlashing = true;
        while (isFlashing)
        {
            if (spriteRenderer != null)
            {
                float alpha = Mathf.PingPong(Time.time * flashSpeed, 0.8f) + 0.2f;
                Color c = originalColor;
                c.a = alpha;
                spriteRenderer.color = c;
            }
            yield return null;
        }
    }

    /// <summary>
    /// Flashes and fades out the ladder over a duration before disabling/hiding it.
    /// </summary>
    public void FlashAndFadeOut(float duration, System.Action onComplete = null)
    {
        StopFlashing();
        StartCoroutine(FadeOutRoutine(duration, onComplete));
    }

    private IEnumerator FadeOutRoutine(float duration, System.Action onComplete)
    {
        if (spriteRenderer != null)
        {
            float elapsed = 0f;
            Color startColor = spriteRenderer.color;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / duration);
                Color c = startColor;
                c.a = alpha;
                spriteRenderer.color = c;
                yield return null;
            }
            spriteRenderer.enabled = false;
        }
        
        onComplete?.Invoke();
    }
}