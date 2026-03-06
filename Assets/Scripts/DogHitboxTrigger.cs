using JetBrains.Annotations;
using UnityEngine;

public class DogHitboxTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Dog hitbox triggered by: " + other.gameObject.name);
        if (other.CompareTag("FallingObject")) ;
        {
            // Onomatopoeia text graphic for a dog getting hit by a falling object
            Debug.Log("Dog hit by a falling object!");
            Instantiate(Resources.Load("BonkText"), transform.position, Quaternion.identity);

            //Add score with scoremanager, NEED TO CREATE
        }
    }
}
