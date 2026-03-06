using UnityEngine;

public class LaunchTracjectory : MonoBehaviour
{
    public Vector2 launchVelocity = new Vector2(1f, 2f); // Default launch direction (diagonal up-right)
    private Rigidbody2D rb;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void CollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Collision Detected with: " + collision.gameObject.name);
            rb.AddForce(launchVelocity, ForceMode2D.Impulse); // Apply the launch velocity as an impulse to the object
        }
    }
}
