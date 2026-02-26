using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager _instance;

    // [Header("Prefabs")]
    [SerializeField] private GameObject singlePlayerPrefab;
    // [SerializeField] private GameObject networkPlayerPrefab;

    // [Header("Spawn Settings")]
    [SerializeField] private Transform spawnPoint;
    // [SerializeField] private Transform playerTwoSpawnPoint;

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

        Instantiate(singlePlayerPrefab, spawnPoint.transform.position, Quaternion.identity);
    }

}