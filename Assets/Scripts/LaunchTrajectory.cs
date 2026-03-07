using UnityEngine;
using System.Collections;

public class LaunchTracjectory : MonoBehaviour
{
    public Vector2 launchVelocity = new Vector2(3f, 3f);
    public float ignoreTime = 0.5f;

    private Rigidbody2D rb;
    private Collider2D objectCollider;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        objectCollider = GetComponent<Collider2D>();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Collision Detected with: " + collision.gameObject.name);
            rb.linearVelocity = Vector2.zero; // Reset velocity before applying launch
            rb.linearVelocity = transform.right * launchVelocity;

            Collider2D playerCollider = collision.collider;
            Physics2D.IgnoreCollision(objectCollider, playerCollider, true);
            StartCoroutine(RestoreCollision(playerCollider));
        }
    }

    IEnumerator RestoreCollision(Collider2D playerCollider)
    {
        yield return new WaitForSeconds(ignoreTime);

        Physics2D.IgnoreCollision(objectCollider, playerCollider, false);
    }
}