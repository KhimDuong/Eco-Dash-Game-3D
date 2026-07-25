using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Esc toggles a pause overlay and freezes the game (Time.timeScale = 0). The
/// pause menu offers Resume / Restart / Settings / Main Menu. While the settings
/// sub-panel is open, Esc closes it first (the game stays paused). Pausing is
/// ignored once the level has ended — the EndScreenController owns the freeze
/// then. The pause buttons wire their OnClick to Resume() / Restart() /
/// OpenSettings() here and GameManager.LoadMainMenu().
/// </summary>
public class PauseController : MonoBehaviour
{
    [SerializeField] GameObject pausePanel;
    [Tooltip("Optional settings overlay opened by the pause menu's 'Cài đặt' button.")]
    [SerializeField] SettingsPanel settingsPanel;

    bool isPaused;
    public bool IsPaused => isPaused;

    void Start()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // While the how-to-play tutorial is up it owns Esc (it closes itself), so
        // don't also toggle the pause menu underneath it.
        if (TutorialPopup.IsOpen) return;

        if (kb.escapeKey.wasPressedThisFrame && !GameIsOver())
        {
            // Esc backs out of the settings sub-panel first, then toggles pause.
            if (settingsPanel != null && settingsPanel.IsOpen) settingsPanel.Close();
            else TogglePause();
        }
    }

    static bool GameIsOver() => GameManager.Instance != null && GameManager.Instance.IsGameOver;

    public void TogglePause() => SetPaused(!isPaused);

    public void Resume() => SetPaused(false);

    public void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.Open();
    }

    /// <summary>Restart the current level — wired to the pause "Chơi lại" button.</summary>
    public void Restart()
    {
        if (GameManager.Instance != null) GameManager.Instance.RestartLevel();
        else Time.timeScale = 1f;
    }

    void SetPaused(bool paused)
    {
        isPaused = paused;
        if (pausePanel != null) pausePanel.SetActive(paused);
        if (!paused && settingsPanel != null) settingsPanel.Close();
        Time.timeScale = paused ? 0f : 1f;
    }
}
