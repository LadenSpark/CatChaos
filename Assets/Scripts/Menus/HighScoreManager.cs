using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using TMPro; // Uncomment if using TextMeshPro

public class HighScoreManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField nameInputField; // Use InputField if not using TMP
    public Button submitButton;
    public GameObject nameInputPanel;
    
    private int currentScore;


    public void Start()
    {
        nameInputPanel.SetActive(false); // Hide the name input panel at the start
        // 1. Silently log the player in using their device hardware ID
        var request = new LoginWithCustomIDRequest
        {
            CustomId = SystemInfo.deviceUniqueIdentifier,
            CreateAccount = true
        };
        
        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnError);
    }

    // Call this when the game ends to trigger the UI
    public void OnGameOver(int score)
    {
        currentScore = score;
        nameInputPanel.SetActive(true);
        submitButton.interactable = true;
    }

    // Attached to the Submit Button's OnClick() event
    public void SubmitHighScore()
    {
        string playerName = nameInputField.text;

        if (string.IsNullOrEmpty(playerName) || playerName.Length < 3)
        {
            Debug.LogWarning("Name too short or empty!");
            return;
        }

        submitButton.interactable = false; // Prevent double clicking
        
        // Step 1: Update the player's display name
        UpdatePlayerName(playerName);
    }

    private void UpdatePlayerName(string name)
    {
        var request = new UpdateUserTitleDisplayNameRequest
        {
            DisplayName = name
        };

        PlayFabClientAPI.UpdateUserTitleDisplayName(request, OnNameUpdateSuccess, OnPlayFabError);
    }

    private void OnNameUpdateSuccess(UpdateUserTitleDisplayNameResult result)
    {
        Debug.Log($"Display name updated to: {result.DisplayName}");
        
        // Step 2: Submit the score now that the name is attached to the account
        SubmitScoreToLeaderboard(currentScore);
    }

    private void SubmitScoreToLeaderboard(int score)
    {
        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>
            {
                new StatisticUpdate
                {
                    StatisticName = "HighScoreLeaderboard", // Must match PlayFab Dashboard
                    Value = score
                }
            }
        };

        PlayFabClientAPI.UpdatePlayerStatistics(request, OnScoreSubmitSuccess, OnPlayFabError);
    }

    private void OnScoreSubmitSuccess(UpdatePlayerStatisticsResult result)
    {
        Debug.Log("Score successfully submitted to PlayFab leaderboard!");
        nameInputPanel.SetActive(false);
        
        // Step 3: Trigger your leaderboard UI to refresh and display the top scores
        GetLeaderboard();
    }

    public void GetLeaderboard()
    {
        var request = new GetLeaderboardRequest
        {
            StatisticName = "HighScoreLeaderboard",
            StartPosition = 0,
            MaxResultsCount = 10 // Top 10 scores
        };

        PlayFabClientAPI.GetLeaderboard(request, OnGetLeaderboardSuccess, OnPlayFabError);
    }

    private void OnGetLeaderboardSuccess(GetLeaderboardResult result)
    {
        // Loop through results to populate your UI text elements
        foreach (var item in result.Leaderboard)
        {
            // item.DisplayName will contain the name they just typed
            // item.StatValue contains the score
            Debug.Log($"{item.Position + 1}. {item.DisplayName} - {item.StatValue}");
        }
    }

    private void OnPlayFabError(PlayFabError error)
    {
        Debug.LogError($"PlayFab Error: {error.GenerateErrorReport()}");
        submitButton.interactable = true; // Re-enable button so player can try again
    }

    void OnLoginSuccess(LoginResult result)
    {
        Debug.Log("Ghost account logged in successfully.");
    }

    void OnError(PlayFabError error)
    {
        Debug.LogError("PlayFab Error: " + error.ErrorMessage);
    }
}