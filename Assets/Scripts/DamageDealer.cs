using UnityEngine;

public class DamageDealer : MonoBehaviour
{
    [SerializeField] private float damageAmount = 20f;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if whatever we hit has a Health component
        Health health = collision.gameObject.GetComponent<Health>();
        
        if (health != null)
        {
            health.TakeDamage(damageAmount);
        }
    }
}