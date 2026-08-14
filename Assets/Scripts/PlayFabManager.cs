using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using TMPro;
using System.Collections.Generic;

public class PlayFabManager : MonoBehaviour
{
    public static PlayFabManager Instance { get; private set; }

    [Header("Leaderboard Settings")]
    [Tooltip("The exact name of the Statistic in your PlayFab Dashboard")]
    public string leaderboardName = "HighScore";

    [Header("Guest Scoreboard UI")]
    public GameObject guestNamePanel;
    public TMP_InputField guestNameInput;

    private bool hasDisplayName = false;
    private int pendingScore = 0;

    private void Awake()
    {
        // Make sure only one PlayFabManager ever exists, and keep it alive across scene loads
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (guestNamePanel != null) guestNamePanel.SetActive(false);

        // Modern V11+ Initialization
        PlayGamesPlatform.Activate();
        AuthenticateWithGoogle();
    }

    #region Authentication

    public void AuthenticateWithGoogle()
    {
        Debug.Log("Attempting Google Play Games Login...");
        
        PlayGamesPlatform.Instance.Authenticate((SignInStatus status) =>
        {
            if (status == SignInStatus.Success)
            {
                PlayGamesPlatform.Instance.RequestServerSideAccess(true, authCode =>
                {
                    LoginToPlayFabWithGoogle(authCode);
                });
            }
            else
            {
                Debug.LogWarning($"GPG Login Failed ({status}). Logging in as Guest...");
                LoginAsGuest();
            }
        });
    }

    private void LoginToPlayFabWithGoogle(string authCode)
    {
        var request = new LoginWithGoogleAccountRequest
        {
            ServerAuthCode = authCode,
            CreateAccount = true
        };

        PlayFabClientAPI.LoginWithGoogleAccount(request, OnGoogleLoginSuccess, OnPlayFabError);
    }

    private void OnGoogleLoginSuccess(LoginResult result)
    {
        Debug.Log("PlayFab: Logged in with Google!");
        hasDisplayName = true; // GPG users always have a name
        
        // Grab their Google Play Games username and automatically set it in PlayFab!
        string googleUsername = PlayGamesPlatform.Instance.GetUserDisplayName();
        SetPlayFabDisplayName(googleUsername, false);
    }

    public void LoginAsGuest()
    {
        Debug.Log("Attempting to create/login to Android Guest Account...");

        var request = new LoginWithAndroidDeviceIDRequest
        {
            AndroidDeviceId = SystemInfo.deviceUniqueIdentifier,
            CreateAccount = true,
            OS = SystemInfo.operatingSystem,
            AndroidDevice = SystemInfo.deviceModel,
            // We ask PlayFab to return the Player Profile so we can check if they already set a name!
            InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
            {
                GetPlayerProfile = true
            }
        };

        PlayFabClientAPI.LoginWithAndroidDeviceID(request, OnGuestLoginSuccess, OnPlayFabError);
    }

    private void OnGuestLoginSuccess(LoginResult result)
    {
        Debug.Log("PlayFab: Logged in as Guest!");
        
        // Check if this guest device already set a name in a previous play session
        if (result.InfoResultPayload != null && result.InfoResultPayload.PlayerProfile != null)
        {
            if (!string.IsNullOrEmpty(result.InfoResultPayload.PlayerProfile.DisplayName))
            {
                Debug.Log($"Welcome back, Guest: {result.InfoResultPayload.PlayerProfile.DisplayName}");
                hasDisplayName = true;
            }
        }
    }

    #endregion

    #region Leaderboard Submission & Display Name

    /// <summary>
    /// Called by your GameManager when the human catches the cat.
    /// </summary>
    public void SubmitScore(int score)
    {
        pendingScore = score;

        if (hasDisplayName)
        {
            // They already have a tag, send the score immediately!
            SendScoreToPlayFab(pendingScore);
        }
        else
        {
            // They are a new guest. Show the UI to ask for a tag.
            if (guestNamePanel != null) guestNamePanel.SetActive(true);
        }
    }

    /// <summary>
    /// Link this to your UI "Submit" button on the Guest Name Panel
    /// </summary>
    public void SubmitGuestName()
    {
        if (guestNameInput != null && !string.IsNullOrEmpty(guestNameInput.text))
        {
            hasDisplayName = true;
            SetPlayFabDisplayName(guestNameInput.text, true);
            guestNamePanel.SetActive(false); 
        }
        else
        {
            Debug.LogWarning("Please enter a valid guest tag!");
        }
    }

    private void SetPlayFabDisplayName(string displayName, bool submitScoreAfter)
    {
        var request = new UpdateUserTitleDisplayNameRequest
        {
            DisplayName = displayName
        };

        PlayFabClientAPI.UpdateUserTitleDisplayName(request, result => 
        {
            Debug.Log($"PlayFab: Scoreboard Tag updated to {result.DisplayName}");
            if (submitScoreAfter)
            {
                SendScoreToPlayFab(pendingScore);
            }
        }, OnPlayFabError);
    }

    private void SendScoreToPlayFab(int score)
    {
        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>
            {
                new StatisticUpdate
                {
                    StatisticName = leaderboardName,
                    Value = score
                }
            }
        };

        PlayFabClientAPI.UpdatePlayerStatistics(request, 
            result => Debug.Log($"Successfully submitted score of {score} to {leaderboardName}!"), 
            OnPlayFabError);
    }

    private void OnPlayFabError(PlayFabError error)
    {
        Debug.LogError("PlayFab Error: " + error.GenerateErrorReport());
    }

    #endregion
}