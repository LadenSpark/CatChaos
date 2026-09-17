using System;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

#if UNITY_ANDROID && !UNITY_EDITOR
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

public class PlayFabManager : MonoBehaviour
{
    public static PlayFabManager Instance { get; private set; }

    [Header("Leaderboard Defaults")]
    public string defaultLevelStatistic = "Level_1_HighScore";

    public string CachedDisplayName { get; private set; } = "";
    public string PlayFabId { get; private set; } = "";
    public bool IsLoggedIn { get; private set; } = false;
    public bool IsGpgAuthenticated { get; private set; } = false;

    private void Awake()
    {
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
        InitializeAuthentication();
    }

    #region Cross-Platform Auto-Authentication

    public void InitializeAuthentication()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        AuthenticateGooglePlayGames();
#else
        LoginWithDeviceHardware();
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private void AuthenticateGooglePlayGames()
    {
        PlayGamesPlatform.Activate();
        PlayGamesPlatform.Instance.Authenticate((status) =>
        {
            if (status == SignInStatus.Success)
            {
                PlayGamesPlatform.Instance.RequestServerSideAccess(true, (authCode) =>
                {
                    if (!string.IsNullOrEmpty(authCode))
                    {
                        ExchangeGpgAuthCodeWithPlayFab(authCode);
                    }
                    else
                    {
                        Debug.LogWarning("GPG server auth code empty. Falling back to device login.");
                        LoginWithDeviceHardware();
                    }
                });
            }
            else
            {
                Debug.LogWarning($"GPG Login Failed or Canceled ({status}). Falling back to Android Device ID...");
                LoginWithDeviceHardware();
            }
        });
    }

    private void ExchangeGpgAuthCodeWithPlayFab(string authCode)
    {
        var request = new LoginWithGooglePlayGamesServicesRequest
        {
            ServerAuthCode = authCode,
            CreateAccount = true,
            InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
            {
                GetPlayerProfile = true
            }
        };

        PlayFabClientAPI.LoginWithGooglePlayGamesServices(request, OnGpgLoginSuccess, (error) =>
        {
            Debug.LogError($"PlayFab GPG Login Failed: {error.GenerateErrorReport()}. Falling back to device login.");
            LoginWithDeviceHardware();
        });
    }

    private void OnGpgLoginSuccess(LoginResult result)
    {
        IsLoggedIn = true;
        IsGpgAuthenticated = true;
        PlayFabId = result.PlayFabId;
        ResolveCachedProfileName(result.InfoResultPayload);

        string gpgGamerTag = PlayGamesPlatform.Instance.GetUserDisplayName();
        if (!string.IsNullOrEmpty(gpgGamerTag) && CachedDisplayName != gpgGamerTag)
        {
            UpdatePlayerDisplayName(gpgGamerTag, null, null);
        }
    }
#endif

    private void LoginWithDeviceHardware()
    {
        // LoginWithAndroidDeviceID bypasses the 'Custom ID creations disabled' restriction
        var request = new LoginWithAndroidDeviceIDRequest
        {
            AndroidDeviceId = SystemInfo.deviceUniqueIdentifier,
            OS = SystemInfo.operatingSystem,
            AndroidDevice = SystemInfo.deviceModel,
            CreateAccount = true,
            InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
            {
                GetPlayerProfile = true
            }
        };

        PlayFabClientAPI.LoginWithAndroidDeviceID(request, OnDeviceLoginSuccess, OnPlayFabError);
    }

    private void OnDeviceLoginSuccess(LoginResult result)
    {
        IsLoggedIn = true;
        IsGpgAuthenticated = false;
        PlayFabId = result.PlayFabId;
        ResolveCachedProfileName(result.InfoResultPayload);
        Debug.Log($"Silent Device Login Success. PlayFabID: {PlayFabId} | DisplayName: {CachedDisplayName}");
    }

    private void ResolveCachedProfileName(GetPlayerCombinedInfoResultPayload payload)
    {
        if (payload?.PlayerProfile != null && !string.IsNullOrEmpty(payload.PlayerProfile.DisplayName))
        {
            CachedDisplayName = payload.PlayerProfile.DisplayName;
        }
    }

    public bool HasDisplayName()
    {
        return !string.IsNullOrEmpty(CachedDisplayName);
    }

    #endregion

    #region Score Submission & Profile Management

    public void UpdatePlayerDisplayName(string newName, Action onSuccess, Action<string> onFailure)
    {
        var request = new UpdateUserTitleDisplayNameRequest
        {
            DisplayName = newName
        };

        PlayFabClientAPI.UpdateUserTitleDisplayName(request, (result) =>
        {
            CachedDisplayName = result.DisplayName;
            Debug.Log($"Display name bound to: {CachedDisplayName}");
            onSuccess?.Invoke();
        },
        (error) =>
        {
            Debug.LogError($"Name update error: {error.GenerateErrorReport()}");
            onFailure?.Invoke(error.ErrorMessage);
        });
    }

    public void SubmitScore(int score, string statKey = null, Action onSuccess = null, Action<string> onFailure = null)
    {
        string targetKey = string.IsNullOrEmpty(statKey) ? defaultLevelStatistic : statKey;

        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>
            {
                new StatisticUpdate
                {
                    StatisticName = targetKey,
                    Value = score
                }
            }
        };

        PlayFabClientAPI.UpdatePlayerStatistics(request, (result) =>
        {
            Debug.Log($"Score {score} posted to {targetKey}");
            onSuccess?.Invoke();
        },
        (error) =>
        {
            Debug.LogError($"Score submission failed: {error.GenerateErrorReport()}");
            onFailure?.Invoke(error.ErrorMessage);
        });
    }

    public void SetDisplayNameAndSubmitScore(string newName, int score, string statKey = null, Action onSuccess = null, Action<string> onFailure = null)
    {
        UpdatePlayerDisplayName(newName, () =>
        {
            SubmitScore(score, statKey, onSuccess, onFailure);
        }, onFailure);
    }

    public void FetchLeaderboard(string statKey, int maxResults, Action<List<PlayerLeaderboardEntry>> onSuccess, Action<string> onFailure)
    {
        string targetKey = string.IsNullOrEmpty(statKey) ? defaultLevelStatistic : statKey;

        var request = new GetLeaderboardRequest
        {
            StatisticName = targetKey,
            StartPosition = 0,
            MaxResultsCount = maxResults
        };

        PlayFabClientAPI.GetLeaderboard(request, (result) =>
        {
            onSuccess?.Invoke(result.Leaderboard);
        },
        (error) =>
        {
            Debug.LogError($"Fetch Leaderboard error: {error.GenerateErrorReport()}");
            onFailure?.Invoke(error.ErrorMessage);
        });
    }

    private void OnPlayFabError(PlayFabError error)
    {
        Debug.LogError($"PlayFab Error: {error.GenerateErrorReport()}");
    }

    #endregion
}