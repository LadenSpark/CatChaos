using JetBrains.Annotations;
using UnityEngine;

public class DogHitboxTrigger : MonoBehaviour
{

    private Animator dogAnim;

    void Awake()
    {
        dogAnim = GetComponent<Animator>(); //need this for the dog hit animation
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Dog hitbox triggered by: " + other.gameObject.name);
        if (other.CompareTag("FallingObject"))
        {
            // Onomatopoeia text graphic for a dog getting hit by a falling object
            Debug.Log("Dog hit by a falling object!");
            Instantiate(Resources.Load("BonkText"), transform.position, Quaternion.identity);


            dogAnim.SetTrigger("dogHit");   //play the animation fo the dog being hit
            //dogAnim.ResetTrigger("dogHit"); //didn't need this on cat, might not need it here

            //Add score with scoremanager, NEED TO CREATE
        }
    }
}
