using System.Collections.Generic;
using UnityEngine;
using TMPro;
using PlayFab.ClientModels;

public class ScoreboardManager : MonoBehaviour
{
    [Header("Leaderboard Config")]
    public string statisticKey = "Level_1_HighScore";
    public int maxRows = 10;

    [Header("UI Display")]
    public Transform rowContainer;
    public GameObject rowPrefab;
    public TextMeshProUGUI statusText;

    public void OpenScoreboard()
    {
        gameObject.SetActive(true);
        LoadScoreboard();
    }

    public void LoadScoreboard()
    {
        if (statusText != null) statusText.text = "Loading Scores...";

        if (PlayFabManager.Instance == null)
        {
            if (statusText != null) statusText.text = "Network Offline.";
            return;
        }

        PlayFabManager.Instance.FetchLeaderboard(statisticKey, maxRows, OnLeaderboardFetched, OnLeaderboardFailed);
    }

    private void OnLeaderboardFetched(List<PlayerLeaderboardEntry> entries)
    {
        if (statusText != null) statusText.text = "";

        if (rowContainer != null)
        {
            foreach (Transform child in rowContainer)
            {
                Destroy(child.gameObject);
            }
        }

        foreach (var entry in entries)
        {
            string playerTag = string.IsNullOrEmpty(entry.DisplayName) ? "Anonymous Guest" : entry.DisplayName;
            int rank = entry.Position + 1;
            int score = entry.StatValue;

            if (rowPrefab != null && rowContainer != null)
            {
                GameObject row = Instantiate(rowPrefab, rowContainer);
                TextMeshProUGUI rowText = row.GetComponentInChildren<TextMeshProUGUI>();
                if (rowText != null)
                {
                    rowText.text = $"{rank}. {playerTag} - {score}";
                }
            }
        }
    }

    private void OnLeaderboardFailed(string error)
    {
        if (statusText != null) statusText.text = "Unable to fetch high scores.";
    }

    public void OnCloseClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CloseScoreboardPanel();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}