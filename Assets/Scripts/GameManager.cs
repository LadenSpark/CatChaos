using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Required for the Slider
using TMPro; // Required for TextMeshPro

public class GameManager : MonoBehaviour
{
    public static GameManager _instance;
    public static GameManager Instance => _instance;

    [SerializeField] private GameObject singlePlayerPrefab;
    // [SerializeField] private GameObject networkPlayerPrefab;

    [SerializeField] private Transform spawnPoint;
    // [SerializeField] private Transform playerTwoSpawnPoint;

    [Header("Chaos / Risk Settings")]
    public float chaosLevel = 0f;
    public float maxChaos = 10f; 

    [Header("Score Settings")]
    public int currentScore = 0;

    [Header("UI Elements")]
    [Tooltip("Slider to visually represent the Risk/Chaos level.")]
    public Slider chaosMeterUI; 
    [Tooltip("Text to show the numerical Chaos/Risk level.")]
    public TextMeshProUGUI chaosTextUI; 
    [Tooltip("Text to show the player's score.")]
    public TextMeshProUGUI scoreTextUI;

    private void Awake()
    {
        // Simple Singleton pattern for easy access
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }

        if (singlePlayerPrefab != null && spawnPoint != null)
        {
            Instantiate(singlePlayerPrefab, spawnPoint.transform.position, Quaternion.identity);
        }
    }

    private void Start()
    {
        // Update the UI immediately when the game starts
        UpdateUI();
    }

    /// <summary>
    /// Calculates directional risk based on the Human's position and facing direction.
    /// </summary>
    public void AddChaosFromMess(Vector3 messPosition)
    {
        float riskToAdd = 1f; // Base risk

        if (Human.Instance != null)
        {
            float humanFacing = Mathf.Sign(Human.Instance.transform.localScale.x);
            float directionToMess = Mathf.Sign(messPosition.x - Human.Instance.transform.position.x);

            if (humanFacing == directionToMess)
            {
                riskToAdd = 2f; // Double risk if they were looking that way!
            }
        }

        AddChaos(riskToAdd);
    }

    /// <summary>
    /// Increases chaos level, forwards risk to Human, and updates UI.
    /// </summary>
    public void AddChaos(float amount)
    {
        chaosLevel = Mathf.Clamp(chaosLevel + amount, 0f, maxChaos);
        Debug.Log($"Chaos Level Increased! Current Chaos: {chaosLevel}");

        if (Human.Instance != null)
        {
            Human.Instance.riskMeter = this.chaosLevel;
        }
        
        UpdateUI();
    }

    /// <summary>
    /// Called when the human climbs randomly due to time. Reduces chaos by half (rounded down).
    /// </summary>
    public void ReduceChaosByHalf()
    {
        float reduction = Mathf.Floor(chaosLevel / 2f);
        chaosLevel -= reduction;
        Debug.Log($"Random patrol finished! Chaos reduced by {reduction}. Current Chaos: {chaosLevel}");
        
        if (Human.Instance != null)
        {
            Human.Instance.riskMeter = this.chaosLevel;
        }
        
        UpdateUI();
    }

    /// <summary>
    /// Called when the human completes a full forced climb, resetting the meter.
    /// </summary>
    public void ResetChaos()
    {
        chaosLevel = 0f;
        Debug.Log("Chaos Level Reset to 0.");
        
        if (Human.Instance != null)
        {
            Human.Instance.riskMeter = this.chaosLevel;
        }
        
        UpdateUI();
    }

    /// <summary>
    /// Adds points to the player's score and updates the UI.
    /// </summary>
    public void AddScore(int points)
    {
        currentScore += points;
        Debug.Log($"Scored {points} points! Total Score: {currentScore}");
        UpdateUI();
    }

    /// <summary>
    /// Refreshes all assigned UI elements to match current variables.
    /// </summary>
    private void UpdateUI()
    {
        if (chaosMeterUI != null)
        {
            chaosMeterUI.maxValue = maxChaos;
            chaosMeterUI.value = chaosLevel;
        }

        if (chaosTextUI != null)
        {
            chaosTextUI.text = $"Chaos: {chaosLevel} / {maxChaos}";
        }

        if (scoreTextUI != null)
        {
            scoreTextUI.text = $"Score: {currentScore}";
        }
    }
}