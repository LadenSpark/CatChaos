using System;
using System.Text.RegularExpressions;
using UnityEngine;

public class FallenObjectController : MonoBehaviour
{
    private Vector3 originalposition;
    private Rigidbody2D rigidbody;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        originalposition = transform.position;
        rigidbody = GetComponent<Rigidbody2D>();
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Collision Detected with: " + other.gameObject.name);
        if (other.CompareTag("Floor"))
        {
            Debug.Log("Collided with Floor! Returning Object to Original Position.");
            ReturnObject();
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        Debug.Log("Exited Collision with: " + collision.gameObject.name);

    }
    void ReturnObject()
    {
        rigidbody.linearVelocity = Vector2.zero;
        rigidbody.angularVelocity = 0f;
        rigidbody.rotation = 0f;
        rigidbody.position = originalposition;
        Debug.Log($"Object's position reset!");

    }
}