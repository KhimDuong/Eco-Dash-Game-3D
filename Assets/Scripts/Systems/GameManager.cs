using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Single source of truth for a level run: energy cores collected, trash
/// gathered, and win/lose flow. UI subscribes to its events instead of polling.
/// One instance per level scene. Cores/win-state are per-run; trash is permanent
/// currency stored in <see cref="PlayerProgress"/> (roadmap M4 shop economy).
///
/// On win or death this raises an event and sets <see cref="IsGameOver"/>; the
/// end-screen UI (EndScreenController) shows the panel and freezes time. Scene
/// navigation happens only when the player presses a UI button, which calls
/// <see cref="RestartLevel"/> / <see cref="LoadNextLevel"/> / <see cref="LoadMainMenu"/>.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Objective")]
    [Tooltip("Clean Energy Cores needed to clear the level.")]
    [SerializeField] int requiredCores = 3;

    /// <summary>(collected, required)</summary>
    public event Action<int, int> OnCoresChanged;
    public event Action<int> OnTrashChanged;
    /// <summary>All cores collected — the teleport gate may now power up.</summary>
    public event Action OnAllCoresCollected;
    public event Action OnLevelComplete;
    public event Action OnPlayerDeath;

    public int CoresCollected { get; private set; }
    /// <summary>Persistent recycling currency — lives in <see cref="PlayerProgress"/>.</summary>
    public int TrashCollected => PlayerProgress.Trash;
    public int RequiredCores => requiredCores;

    /// <summary>True once every required core is collected (gate is armed).</summary>
    public bool AllCoresCollected { get; private set; }

    /// <summary>True once the level has been won or the player has died.</summary>
    public bool IsGameOver { get; private set; }

    /// <summary>True if a scene exists after this one in Build Settings.</summary>
    public bool HasNextLevel =>
        SceneManager.GetActiveScene().buildIndex + 1 < SceneManager.sceneCountInBuildSettings;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Time.timeScale = 1f; // clear any freeze left over from a pause/end screen

        // Restore objective progress for this scene (M9 save persistence) so revisiting
        // a stage keeps collected cores/keycards instead of resetting them. Set in Awake
        // (before any Start) so the HUD and teleport gate read the correct state.
        int saved = SceneProgress.GetCores(SceneManager.GetActiveScene().name);
        if (saved > 0)
        {
            CoresCollected = Mathf.Min(saved, requiredCores);
            if (CoresCollected >= requiredCores) AllCoresCollected = true;
        }
    }

    void Start()
    {
        // Push initial values so a HUD that subscribed in Start gets state.
        OnCoresChanged?.Invoke(CoresCollected, requiredCores);
        OnTrashChanged?.Invoke(TrashCollected);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void CollectCore()
    {
        if (IsGameOver || AllCoresCollected) return;
        CoresCollected++;
        SceneProgress.SetCores(SceneManager.GetActiveScene().name, CoresCollected);
        OnCoresChanged?.Invoke(CoresCollected, requiredCores);

        // Collecting the last core no longer wins outright — it arms the teleport
        // gate. The level only completes once Greenie walks into the live gate
        // (see EnterGate), which is also the hook Level 2's exit will reuse.
        if (CoresCollected >= requiredCores)
        {
            AllCoresCollected = true;
            Debug.Log("[Eco-Dash] All cores collected — teleport gate armed.");
            OnAllCoresCollected?.Invoke();
        }
    }

    public void AddTrash(int amount)
    {
        // Trash is permanent currency now — bank it in PlayerProgress (roadmap M4).
        PlayerProgress.AddTrash(amount);
        OnTrashChanged?.Invoke(PlayerProgress.Trash);
    }

    /// <summary>
    /// Called by the <see cref="TeleportGate"/> when Greenie steps into the live
    /// gate. If a next level exists in Build Settings we teleport straight there;
    /// otherwise (the current demo build) we complete the level so the victory
    /// screen shows.
    /// </summary>
    public void EnterGate()
    {
        if (IsGameOver || !AllCoresCollected) return;
        if (HasNextLevel) LoadNextLevel();
        else CompleteLevel();
    }

    /// <summary>
    /// Wins the level (victory screen). Called by <see cref="EnterGate"/> on the
    /// final level, and directly by a boss on death (Level 2's Mega-Smog).
    /// </summary>
    public void CompleteLevel()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        Debug.Log("[Eco-Dash] Level cleared! Water purified — valley reclaimed.");
        OnLevelComplete?.Invoke();
    }

    public void OnPlayerDied()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        Debug.Log("[Eco-Dash] Greenie was overwhelmed.");
        OnPlayerDeath?.Invoke();
    }

    // --- Navigation (wired to UI buttons) ----------------------------------

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadNextLevel()
    {
        Time.timeScale = 1f;
        if (HasNextLevel)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        else
            Debug.Log("[Eco-Dash] No next level in Build Settings yet — demo complete!");
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }
}
