using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Risk System")]
    public float maxRisk = 10f;
    private float currentRisk = 0f;
    
    [Tooltip("Drag your UI Slider here for the Risk Meter")]
    public Slider riskMeterUI; 

    [Header("References")]
    [Tooltip("Drag the Human GameObject here so we can check their facing direction.")]
    public Human humanScript;

        // [Header("Prefabs")]
    [SerializeField] private GameObject singlePlayerPrefab;
    // [SerializeField] private GameObject networkPlayerPrefab;

    // [Header("Spawn Settings")]
    [SerializeField] private Transform spawnPoint;
    // [SerializeField] private Transform playerTwoSpawnPoint;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        Instantiate(singlePlayerPrefab, spawnPoint.transform.position, Quaternion.identity);
    }

    void Start()
    {
        UpdateRiskUI();
    }

    /// <summary>
    /// Called by FallenObject.cs when it hits the ground.
    /// </summary>
    public void AddRisk(Vector3 messPosition)
    {
        float riskToAdd = 1f; // Default risk if facing away

        if (humanScript != null)
        {
            // Figure out which direction the mess is relative to the human (positive = right, negative = left)
            float directionToMess = messPosition.x - humanScript.transform.position.x;
            
            // Get the human's current facing direction from their localScale X (1 = right, -1 = left)
            float humanFacingDirection = humanScript.transform.localScale.x;

            // If the signs match (both positive or both negative), the human is facing the mess!
            if (Mathf.Sign(directionToMess) == Mathf.Sign(humanFacingDirection))
            {
                Debug.Log("Human saw that! Double risk!");
                riskToAdd = 2f;
            }
            else
            {
                Debug.Log("Human was facing away. Phew, only 1 risk.");
            }
        }

        // Add risk and clamp it to our max of 10
        currentRisk += riskToAdd;
        currentRisk = Mathf.Clamp(currentRisk, 0, maxRisk);
        UpdateRiskUI();

        // Trigger the human if we hit 10
        if (currentRisk >= maxRisk)
        {
            Debug.Log("Risk meter hit 10! The human is coming for you!");
            humanScript.TriggerLadderSearch();
            ResetRisk(); // Resets the meter while they search
        }
    }

    public void ResetRisk()
    {
        currentRisk = 0f;
        UpdateRiskUI();
    }

    private void UpdateRiskUI()
    {
        if (riskMeterUI != null)
        {
            riskMeterUI.value = currentRisk / maxRisk;
        }
    }
}
// Legacy
// using UnityEngine;
// using Unity.Netcode;
// using UnityEngine.SceneManagement;

// public class GameManager : MonoBehaviour
// {
//     public static GameManager _instance;



//     private void Awake()
//     {
//         // Simple Singleton pattern for easy access
//         if (_instance != null && _instance != this)
//         {
//             Destroy(this.gameObject);
//         }
//         else
//         {
//             _instance = this;
//         }

//         Instantiate(singlePlayerPrefab, spawnPoint.transform.position, Quaternion.identity);
//     }

// }