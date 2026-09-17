using UnityEngine;
using UnityEngine.SceneManagement;

public class ScrollingCredits : MonoBehaviour
{
    [Header("Scrolling Settings")]
    public float scrollSpeed = 50f; //[cite: 1]
    public float endYPosition = 1000f; //[cite: 1]
    public bool loop = false; //[cite: 1]

    [Header("Interaction Settings")]
    public float resumeDelay = 3f; //[cite: 1]

    [Header("Post-Credits Navigation")]
    public GameObject postCreditsPanel;
    public string gameSceneName = "GameScene";
    public string mainMenuSceneName = "MainMenu";

    private Vector3 startPosition; //[cite: 1]
    private bool isTouching = false; //[cite: 1]
    private float resumeTimer = 0f; //[cite: 1]
    private Vector2 lastTouchPosition; //[cite: 1]
    private bool hasReachedBottom = false;

    void Start()
    {
        startPosition = transform.localPosition; //[cite: 1]
        if (postCreditsPanel != null)
        {
            postCreditsPanel.SetActive(false);
        }
    }

    void Update()
    {
        if (hasReachedBottom) return;

        HandleInput();

        if (!isTouching)
        {
            if (resumeTimer > 0)
            {
                resumeTimer -= Time.deltaTime; //[cite: 1]
            }
            else
            {
                AutoScroll();
            }
        }

        CheckBounds();
    }

    void HandleInput()
    {
        if (Input.GetMouseButtonDown(0)) //[cite: 1]
        {
            isTouching = true; //[cite: 1]
            lastTouchPosition = Input.mousePosition; //[cite: 1]
            resumeTimer = resumeDelay; //[cite: 1]
        }

        if (Input.GetMouseButton(0)) //[cite: 1]
        {
            Vector2 currentTouchPosition = Input.mousePosition; //[cite: 1]
            float deltaY = currentTouchPosition.y - lastTouchPosition.y; //[cite: 1]
            transform.localPosition += Vector3.up * deltaY; //[cite: 1]
            lastTouchPosition = currentTouchPosition; //[cite: 1]
        }

        if (Input.GetMouseButtonUp(0)) //[cite: 1]
        {
            isTouching = false; //[cite: 1]
            resumeTimer = resumeDelay; //[cite: 1]
        }
    }

    void AutoScroll()
    {
        transform.localPosition += Vector3.up * scrollSpeed * Time.deltaTime; //[cite: 1]
    }

    void CheckBounds()
    {
        if (transform.localPosition.y >= endYPosition) //[cite: 1]
        {
            if (loop) //[cite: 1]
            {
                transform.localPosition = startPosition; //[cite: 1]
            }
            else
            {
                Vector3 pos = transform.localPosition; //[cite: 1]
                pos.y = endYPosition; //[cite: 1]
                transform.localPosition = pos; //[cite: 1]

                TriggerCreditsEnd();
            }
        }
    }

    void TriggerCreditsEnd()
    {
        hasReachedBottom = true;
        isTouching = false;

        if (postCreditsPanel != null)
        {
            postCreditsPanel.SetActive(true);
        }
    }

    #region Post-Credits Navigation

    public void OnPlayAgainClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnMainMenuClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    #endregion
}