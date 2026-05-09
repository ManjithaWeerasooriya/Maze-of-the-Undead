using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central game state owner. Listens for ExitTrigger events and drives win flow.
/// Survives across scene loads if <see cref="persistAcrossScenes"/> is enabled.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Playing, Won, Lost, Paused }

    [Header("Lifetime")]
    [SerializeField] private bool persistAcrossScenes = false;

    [Header("Win Flow")]
    [SerializeField] private bool pauseOnWin = true;
    [SerializeField] private bool unlockCursorOnWin = true;
    [Tooltip("Optional UI panel toggled on when the player wins.")]
    [SerializeField] private GameObject winScreen;

    [SerializeField] private VictoryUI victoryUI;

    [SerializeField] private GameObject gameOverPanel;

    public GameState State { get; private set; } = GameState.Playing;

    /// <summary>Raised whenever the game state changes (Playing -> Won, etc.).</summary>
    public event System.Action<GameState> StateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (persistAcrossScenes) DontDestroyOnLoad(gameObject);

        if (winScreen == null)
            Debug.LogWarning("[GameManager] winScreen is not assigned in the Inspector — the win UI will never display.", this);
        else
            winScreen.SetActive(false);
    }

    private void OnEnable()
    {
        ExitTrigger.ExitReached += HandleExitReached;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        Debug.Log("[GameManager] Subscribed to ExitTrigger.ExitReached.", this);
    }

    private void OnDisable()
    {
        ExitTrigger.ExitReached -= HandleExitReached;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleExitReached(ExitTrigger trigger)
    {
        Debug.Log($"[GameManager] HandleExitReached from '{(trigger != null ? trigger.name : "null")}' (current state: {State}).", this);
        if (State != GameState.Playing) return;
        WinLevel();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reset state on a fresh scene load so a cached singleton doesn't stay in 'Won'.
        Time.timeScale = 1f;
        SetState(GameState.Playing);
        if (winScreen != null) winScreen.SetActive(false);
    }

    public void WinLevel()
    {
        Debug.Log("[GameManager] WinLevel() invoked.", this);

        SetState(GameState.Won);

        // 👉 Use VictoryUI instead of raw winScreen
        if (victoryUI != null)
        {
            Debug.Log("[GameManager] Calling VictoryUI.ShowVictory()");
            victoryUI.ShowVictory();
        }
        else
        {
            Debug.LogWarning("[GameManager] VictoryUI not assigned, falling back to winScreen");

            if (winScreen != null)
            {
                winScreen.SetActive(true);

                if (!winScreen.activeInHierarchy)
                {
                    Debug.LogError(
                        $"[GameManager] '{winScreen.name}' not active in hierarchy. Check parent Canvas.",
                        winScreen);
                }
            }
        }

        // Cursor handling
        if (unlockCursorOnWin)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Pause AFTER UI + particles trigger
        if (pauseOnWin)
        {
            Invoke(nameof(PauseGame), 0.05f);
        }
    }
    public void LoseLevel()
    {
        if (State != GameState.Playing) return;

        SetState(GameState.Lost);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void PauseGame()
    {
        Time.timeScale = 0f;
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadNextLevel()
    {
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        if (next >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning("[GameManager] No next scene registered in Build Settings.");
            return;
        }
        Time.timeScale = 1f;
        SceneManager.LoadScene(next);
    }

    public void LoadScene(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    private void SetState(GameState newState)
    {
        if (State == newState) return;
        State = newState;
        StateChanged?.Invoke(newState);
    }
}
