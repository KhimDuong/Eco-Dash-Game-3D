using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Drives Mr. Bear's shop window: shows the live trash balance and a row per
/// upgrade, and opens/closes the panel. Reads/writes <see cref="PlayerProgress"/>
/// and refreshes whenever it changes (never polls). Button click handlers are
/// added in code (no persistent-listener wiring needed).
/// </summary>
public class ShopController : MonoBehaviour
{
    [Header("Window")]
    [Tooltip("The shop panel, hidden until Mr. Bear is talked to.")]
    [SerializeField] GameObject panel;
    [SerializeField] TMP_Text trashText;
    [SerializeField] ShopUpgradeRow[] rows;
    [SerializeField] Button closeButton;
    [Tooltip("Always-visible button that leaves the shop back to the Main Menu.")]
    [SerializeField] Button backButton;

    [Header("Audio (optional)")]
    [SerializeField] AudioClip openSfx;
    [SerializeField] AudioClip buySfx;

    AudioSource audioSource;

    public bool IsOpen => panel != null && panel.activeSelf;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (panel != null) panel.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (backButton != null) backButton.onClick.AddListener(BackToMenu);
    }

    /// <summary>Leave the recycling station and return to the Main Menu.</summary>
    public void BackToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    void OnEnable() => PlayerProgress.OnChanged += Refresh;
    void OnDisable() => PlayerProgress.OnChanged -= Refresh;

    void Update()
    {
        // Esc closes the shop window if open, otherwise leaves to the Main Menu.
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            if (IsOpen) Close();
            else BackToMenu();
        }
    }

    public void Open()
    {
        if (panel == null || IsOpen) return;
        panel.SetActive(true);
        Refresh();
        if (openSfx != null && audioSource != null) audioSource.PlayOneShot(openSfx);
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    /// <summary>Called by a row after a successful purchase.</summary>
    public void PlayBuyFeedback()
    {
        if (buySfx != null && audioSource != null) audioSource.PlayOneShot(buySfx);
    }

    void Refresh()
    {
        if (trashText != null) trashText.text = $"Rác: {PlayerProgress.Trash}";
        if (rows != null)
            foreach (var r in rows)
                if (r != null) r.Refresh();
    }
}
