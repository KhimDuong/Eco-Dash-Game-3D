using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Shows the win or lose panel when the level ends and freezes the game
/// (Time.timeScale = 0). Subscribes to GameManager's OnLevelComplete /
/// OnPlayerDeath events. The panels' buttons wire their OnClick to the
/// GameManager navigation methods (RestartLevel / LoadNextLevel / LoadMainMenu).
///
/// M7: if <see cref="completeScene"/> is set, OnLevelComplete instead routes to that
/// scene by name after a short beat (used by Level 2 to play the Ending_Story slides
/// once the boss is destroyed) rather than showing the win panel.
/// </summary>
public class EndScreenController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] GameObject winPanel;
    [SerializeField] GameObject losePanel;

    [Header("Flow (optional)")]
    [Tooltip("If set, OnLevelComplete loads this scene by NAME instead of showing the win " +
             "panel — e.g. Ending_Story after the boss. Leave blank for the normal win panel.")]
    [SerializeField] string completeScene = "";
    [Tooltip("Delay before loading completeScene, so the win cue can play.")]
    [SerializeField] float completeDelay = 1.0f;

    [Header("Audio (optional)")]
    [SerializeField] AudioClip winSfx;
    [SerializeField] AudioClip loseSfx;

    void Start()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLevelComplete += ShowWin;
            GameManager.Instance.OnPlayerDeath += ShowLose;
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLevelComplete -= ShowWin;
            GameManager.Instance.OnPlayerDeath -= ShowLose;
        }
    }

    void ShowWin()
    {
        PlayCue(winSfx);

        // M7: route to a story scene (Ending_Story) instead of the win panel when set.
        if (!string.IsNullOrEmpty(completeScene))
        {
            Time.timeScale = 1f; // don't freeze — let the Invoke timer run, then load
            Invoke(nameof(LoadCompleteScene), completeDelay);
            return;
        }

        if (winPanel != null) winPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    void LoadCompleteScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(completeScene);
    }

    void ShowLose()
    {
        PlayCue(loseSfx);
        if (losePanel != null) losePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    static void PlayCue(AudioClip clip)
    {
        if (clip == null) return;
        Vector3 at = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        AudioSource.PlayClipAtPoint(clip, at);
    }
}
