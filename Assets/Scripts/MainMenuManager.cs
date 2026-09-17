using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Configuration")]
    [Tooltip("Type the exact name of your main game scene here.")]
    public string gameSceneName = "GameScene";

    [Header("In-Menu Scoreboard Panel")]
    public GameObject scoreboardPanel;
    public ScoreboardManager scoreboardManager;

    private void Start()
    {
        if (scoreboardPanel != null)
        {
            scoreboardPanel.SetActive(false);
        }
    }

    public void PlayGame()
    {
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.LogError("Main Menu: Game Scene Name is not set in the Inspector!");
        }
    }

    public void OpenScoreboard()
    {
        if (scoreboardManager != null)
        {
            scoreboardManager.OpenScoreboard();
        }
        else if (scoreboardPanel != null)
        {
            scoreboardPanel.SetActive(true);
        }
    }

    public void CloseScoreboard()
    {
        if (scoreboardManager != null)
        {
            scoreboardManager.OnCloseClicked();
        }
        else if (scoreboardPanel != null)
        {
            scoreboardPanel.SetActive(false);
        }
    }

    public void QuitGame()
    {
        Debug.Log("Exiting Game...");
        Application.Quit();
    }
}