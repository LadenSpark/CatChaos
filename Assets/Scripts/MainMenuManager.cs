using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Configuration")]
    [Tooltip("Type the exact name of your main game scene here.")]
    public string gameSceneName = "GameScene";
    
    [Tooltip("Type the exact name of your highscore/scoreboard scene here.")]
    public string scoreboardSceneName = "Scoreboard";

    /// <summary>
    /// Loads the main game scene. Link this to your "Play" button.
    /// </summary>
    public void PlayGame()
    {
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            // Reset time scale just in case the game was paused before returning to the main menu
            Time.timeScale = 1f; 
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.LogError("Main Menu: Game Scene Name is not set in the Inspector!");
        }
    }

    /// <summary>
    /// Loads the highscore scene. Link this to your "Scoreboard" button.
    /// </summary>
    public void OpenScoreboard()
    {
        if (!string.IsNullOrEmpty(scoreboardSceneName))
        {
            SceneManager.LoadScene(scoreboardSceneName);
        }
        else
        {
            Debug.LogError("Main Menu: Scoreboard Scene Name is not set in the Inspector!");
        }
    }

    /// <summary>
    /// Exits the application. Optional, but good to have for a complete main menu.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Exiting Game...");
        Application.Quit();
    }
}