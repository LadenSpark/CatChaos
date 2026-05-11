using UnityEngine;
using System.Collections.Generic;

public class ShatterManager : MonoBehaviour
{
    public int approximatePieces = 20;
    public float explosionForce = 3f;
    public PhysicsMaterial2D physicsMaterial;

    // This list stores the shards so we can clean them later
    private List<GameObject> activeShards = new List<GameObject>();


    public void BreakSprite()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        float ppu = sr.sprite.pixelsPerUnit;
        
        // Capture the original scale and double it
        Vector3 doubleScale = transform.localScale * 2f;

        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(approximatePieces));
        float baseWidth = sr.sprite.rect.width / gridSize;
        float baseHeight = sr.sprite.rect.height / gridSize;

        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                Rect pieceRect = new Rect(
                    sr.sprite.rect.x + (x * baseWidth), 
                    sr.sprite.rect.y + (y * baseHeight), 
                    baseWidth, 
                    baseHeight
                );

                Sprite newSprite = Sprite.Create(sr.sprite.texture, pieceRect, new Vector2(0.5f, 0.5f), ppu);

                GameObject piece = new GameObject("Shard_DoubleSize");
                
                // Set position based on the original object's world space
                Vector3 localOffset = new Vector3(
                    (pieceRect.center.x - sr.sprite.rect.center.x) / ppu,
                    (pieceRect.center.y - sr.sprite.rect.center.y) / ppu,
                    0
                );
                
                piece.transform.position = transform.TransformPoint(localOffset);
                piece.transform.rotation = transform.rotation;
                
                // Apply the DOUBLE scale here
                piece.transform.localScale = doubleScale;

                // Visuals
                piece.AddComponent<SpriteRenderer>().sprite = newSprite;
                
                // Physics
                piece.AddComponent<PolygonCollider2D>().sharedMaterial = physicsMaterial;
                Rigidbody2D rb = piece.AddComponent<Rigidbody2D>();
                rb.linearDamping = 0.5f; // Helps them settle into a pile

                activeShards.Add(piece);
                
                // Initial Pop
                rb.AddForce(Random.insideUnitCircle * explosionForce, ForceMode2D.Impulse);
            }
        }

        // Deactivate original so only shards are visible
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) BreakSprite();
    }

    // CALL THIS TO CLEAN
    public void CleanPile()
    {
        foreach (GameObject shard in activeShards)
        {
            if (shard != null)
            {
                // You could also move them toward the human here 
                // instead of just destroying them
                Destroy(shard);
            }
        }
        activeShards.Clear();
    }
}