using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class NetworkGameManager : MonoBehaviour
{
    public static NetworkGameManager _instance;

    [Header("Prefabs")]
    [SerializeField] private GameObject singlePlayerPrefab;
    [SerializeField] private GameObject networkPlayerPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform playerTwoSpawnPoint;

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
    }

    // --- Mode Starters ---

    public void StartSinglePlayer()
    {
        Debug.Log("Starting Single Player Mode...");
        // Disable NetworkManager to avoid interference if necessary
        //if (NetworkManager.Singleton != null) NetworkManager.Singleton.gameObject.SetActive(false);

        // Standard Unity Instantation
        Instantiate(singlePlayerPrefab, spawnPoint.position, Quaternion.identity);
    }

    public void StartNetworkHost()
    {
        Debug.Log("Starting Network Host (Server + Player)...");
        ConfigureNetworkPrefab();
        NetworkManager.Singleton.StartHost();
    }

    public void StartNetworkClient()
    {
        Debug.Log("Starting Network Client...");
        // Prefab config is usually handled by the Host, but we set it for safety
        ConfigureNetworkPrefab();
        NetworkManager.Singleton.StartClient();
    }

    private void ConfigureNetworkPrefab()
    {
        if (NetworkManager.Singleton != null)
        {
            // Update the NetworkManager's player prefab slot at runtime
            var transport = NetworkManager.Singleton.gameObject.GetComponent<NetworkConfig>();
            NetworkManager.Singleton.NetworkConfig.PlayerPrefab = networkPlayerPrefab;
        }
    }

    public void ResetGame()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}