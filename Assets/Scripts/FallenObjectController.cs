using System;
using System.Text.RegularExpressions;
using UnityEngine;

public class FallenObjectController : MonoBehaviour
{
    private Vector3 originalPosition;
    private Rigidbody2D rb;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        originalPosition = transform.position;
        rb = GetComponent<Rigidbody2D>();
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Collision Detected with: " + other.gameObject.name);
        if (other.CompareTag("Floor")) // Check if the collided object has the tag "Floor"
        {
            Debug.Log("Collided with Floor! Returning Object to Original Position.");
            ReturnObject(); // Call the method to return the object to its original position
        }
        if (other.CompareTag("Dog")) // Check if the collided object has the tag "Dog"
        {
            Debug.Log("Dog hit!");
            HitDog(); // Call the method to add points or for hitting the dog
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        Debug.Log("Exited Collision with: " + collision.gameObject.name); // Once the object leaves the collsion, log the exit event for debugging purposes

    }
    void ReturnObject() // Returns the object to its original position and resets its velocity and rotation
    {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.rotation = 0f;
        rb.position = originalPosition;
        Debug.Log($"Object's position reset!");

    }

    void HitDog() // This method is called when the object collides with the dog
    {
        Debug.Log("Hit the dog! Good job!");
        
    }
}