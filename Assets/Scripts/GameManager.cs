using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine.UI; 
using TMPro; 

public class GameManager : MonoBehaviour
{
    public static GameManager _instance;
    public static GameManager Instance => _instance;

    [SerializeField] private GameObject singlePlayerPrefab;
    [SerializeField] private Transform spawnPoint;

    [Header("Chaos / Risk Settings")]
    public float chaosLevel = 0f;
    public float maxChaos = 10f; 

    [Header("Score Settings")]
    public int currentScore = 0;

    [Header("UI Elements")]
    public Slider chaosMeterUI; 
    public TextMeshProUGUI chaosTextUI; 
    public TextMeshProUGUI scoreTextUI;

    [Header("Menu & Pause Settings")]
    public GameObject pauseMenuPanel; 
    public string mainMenuSceneName = "MainMenu"; // Name of your Main Menu scene exact spelling
    private bool isPaused = false;

    private void Awake()
    {
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
        UpdateUI();
        
        // Ensure time is flowing normally when the scene starts and the pause menu is hidden
        Time.timeScale = 1f;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
    }

    public void AddChaosFromMess(Vector3 messPosition)
    {
        float riskToAdd = 1f; 

        if (Human.Instance != null)
        {
            float humanFacing = Mathf.Sign(Human.Instance.transform.localScale.x);
            float directionToMess = Mathf.Sign(messPosition.x - Human.Instance.transform.position.x);

            if (humanFacing == directionToMess)
            {
                riskToAdd = 2f; 
            }
        }

        AddChaos(riskToAdd);
    }

    public void AddChaos(float amount)
    {
        chaosLevel = Mathf.Clamp(chaosLevel + amount, 0f, maxChaos);
        UpdateUI();
    }

    public void ReduceChaosByHalf()
    {
        float reduction = Mathf.Floor(chaosLevel / 2f);
        chaosLevel -= reduction;
        UpdateUI();
    }

    public void ResetChaos()
    {
        chaosLevel = 0f;
        UpdateUI();
    }

    public void AddScore(int points)
    {
        currentScore += points;
        UpdateUI();
    }

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

    #region Pause & Scene Management

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; // Freezes the game physics and time
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // Unfreezes the game
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f; // MUST reset time before loading, or the new scene will be frozen!
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f; // MUST reset time before loading
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitApp()
    {
        Debug.Log("Exiting Application...");
        Application.Quit();
    }

    public void TriggerGameOver()
    {
        Debug.Log("Game Over Triggered! Submitting Score...");
        
        // Pause the game mechanics
        Time.timeScale = 0f; 
        
        // Send the score to PlayFab
        if (PlayFabManager.Instance != null)
        {
            PlayFabManager.Instance.SubmitScore(currentScore);
        }

        // TODO: Show your Game Over UI screen here!
    }

    #endregion
}