using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameObject singlePlayerPrefab;
    [SerializeField] private Transform spawnPoint;

    [Header("Chaos / Risk Settings")]
    public float chaosLevel = 0f;
    public float maxChaos = 10f;

    [Header("Score Settings")]
    public int currentScore = 0;
    public string levelStatisticKey = "Level_1_HighScore";

    [Header("UI Elements")]
    public Slider chaosMeterUI;
    public TextMeshProUGUI chaosTextUI;
    public TextMeshProUGUI scoreTextUI;

    [Header("Menu & Pause Settings")]
    public GameObject pauseMenuPanel;
    public Button resumeButton;
    public string mainMenuSceneName = "MainMenu";
    private bool isPaused = false;

    [Header("Game State")]
    public bool isGameOver = false;

    [Header("In-Game Scoreboard Panel")]
    public GameObject scoreboardPanel;
    public ScoreboardManager scoreboardManager;

    [Header("Guest Name Modal UI")]
    public GameObject nameInputPanel;
    public TMP_InputField nameInputField;
    public Button nameSubmitButton;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (singlePlayerPrefab != null && spawnPoint != null)
        {
            Instantiate(singlePlayerPrefab, spawnPoint.transform.position, Quaternion.identity);
        }
    }

    private void Start()
    {
        isGameOver = false;
        isPaused = false;
        UpdateUI();
        Time.timeScale = 1f;

        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (nameInputPanel != null) nameInputPanel.SetActive(false);
        if (scoreboardPanel != null) scoreboardPanel.SetActive(false);
    }

    private void Update()
    {
        // Only Escape pauses/resumes during active gameplay
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else if (!isGameOver)
            {
                PauseGame();
            }
        }
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

    #region Pause & Navigation

    public void PauseGame()
    {
        if (isGameOver) return;

        isPaused = true;
        Time.timeScale = 0f;
        if (resumeButton != null) resumeButton.interactable = true;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
    }

    public void ResumeGame()
    {
        if (isGameOver) return;

        isPaused = false;
        Time.timeScale = 1f;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitApp()
    {
        Debug.Log("Exiting Application...");
        Application.Quit();
    }

    #endregion

    #region Scoreboard In-Game Panel Flow

    public void OpenScoreboardPanel()
    {
        Time.timeScale = 0f;

        if (scoreboardPanel != null)
        {
            scoreboardPanel.SetActive(true);
        }

        if (scoreboardManager != null)
        {
            scoreboardManager.LoadScoreboard();
        }
    }

    public void CloseScoreboardPanel()
    {
        if (scoreboardPanel != null)
        {
            scoreboardPanel.SetActive(false);
        }

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
        }

        if (resumeButton != null)
        {
            resumeButton.interactable = false;
        }
    }

    #endregion

    #region Game Over & Submission

    public void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        Time.timeScale = 0f;

        if (PlayFabManager.Instance == null)
        {
            Debug.LogError("PlayFabManager instance not found in scene!");
            OpenScoreboardPanel();
            return;
        }

        if (PlayFabManager.Instance.HasDisplayName())
        {
            PlayFabManager.Instance.SubmitScore(currentScore, levelStatisticKey,
                onSuccess: () => OpenScoreboardPanel(),
                onFailure: (err) => OpenScoreboardPanel());
        }
        else
        {
            if (nameInputPanel != null)
            {
                nameInputPanel.SetActive(true);
                if (nameSubmitButton != null) nameSubmitButton.interactable = true;
            }
            else
            {
                PlayFabManager.Instance.SubmitScore(currentScore, levelStatisticKey,
                    onSuccess: () => OpenScoreboardPanel(),
                    onFailure: (err) => OpenScoreboardPanel());
            }
        }
    }

    public void OnGuestNameSubmitClicked()
    {
        if (nameInputField == null) return;

        string enteredName = nameInputField.text.Trim();
        if (string.IsNullOrEmpty(enteredName) || enteredName.Length < 3)
        {
            Debug.LogWarning("Name must be at least 3 characters long.");
            return;
        }

        if (nameSubmitButton != null) nameSubmitButton.interactable = false;

        PlayFabManager.Instance.SetDisplayNameAndSubmitScore(enteredName, currentScore, levelStatisticKey,
            onSuccess: () =>
            {
                if (nameInputPanel != null) nameInputPanel.SetActive(false);
                OpenScoreboardPanel();
            },
            onFailure: (err) =>
            {
                Debug.LogError($"Submission error: {err}");
                if (nameSubmitButton != null) nameSubmitButton.interactable = true;
            });
    }

    #endregion
}