using JetBrains.Annotations;
using UnityEngine;



public class DogHitboxTrigger : MonoBehaviour
{
    private Animator dogAnim;

    void Awake()
    {
        dogAnim = GetComponent<Animator>(); //need this for the dog hit animation
        dogAnim.SetBool("dogHit", false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Dog hitbox triggered by: " + other.gameObject.name);
        if (other.CompareTag("FallingObject"))
        {
            // Onomatopoeia text graphic for a dog getting hit by a falling object
            Debug.Log("Dog hit by a falling object!");
            //Instantiate(Resources.Load("BonkText"), transform.position, Quaternion.identity);

            //isAnimPlaying = true;
            dogAnim.SetBool("dogHit", true);
            //Add score with scoremanager, NEED TO CREATE
        }
    }

    private void DogHitConclusion() //called by the animation when the swipe ends, necessary to lock in place for the duration of the swipe
    {
        dogAnim.SetBool("dogHit", false);
        
    }

}
