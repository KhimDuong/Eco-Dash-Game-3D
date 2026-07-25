using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Heads-up display: HP bar fill + collected energy-core count + trash count.
/// Listens to PlayerHealth and GameManager events (never polls). Labels are in
/// Vietnamese to match the game brief.
/// </summary>
public class HudController : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] PlayerHealth playerHealth;
    [Tooltip("Filled Image (Image Type = Filled) representing the HP bar.")]
    [SerializeField] Image hpFill;
    [Tooltip("Text showing the exact HP numbers, e.g. '6/6'")]
    [SerializeField] TMP_Text hpText;

    [Header("Counters")]
    [SerializeField] TMP_Text coreText;
    [SerializeField] TMP_Text trashText;

    [Header("Objective labels (per level)")]
    [Tooltip("Name of the collectible objective (Level 1: cores, Level 2: keycards).")]
    [SerializeField] string objectiveLabel = "Lõi NL";
    [Tooltip("Hint shown once all are collected.")]
    [SerializeField] string objectiveCompleteHint = "Tới cổng phía Bắc!";

    void Start()
    {
        if (playerHealth == null) playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateHealth;
            UpdateHealth(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCoresChanged += UpdateCores;
            GameManager.Instance.OnTrashChanged += UpdateTrash;
            UpdateCores(GameManager.Instance.CoresCollected, GameManager.Instance.RequiredCores);
            UpdateTrash(GameManager.Instance.TrashCollected);
        }
    }

    void OnDestroy()
    {
        if (playerHealth != null) playerHealth.OnHealthChanged -= UpdateHealth;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCoresChanged -= UpdateCores;
            GameManager.Instance.OnTrashChanged -= UpdateTrash;
        }
    }

    void UpdateHealth(int current, int max)
    {
        if (hpFill != null) hpFill.fillAmount = max > 0 ? (float)current / max : 0f;
        if (hpText != null) hpText.text = $"{current}/{max}";
    }

    void UpdateCores(int collected, int required)
    {
        if (coreText == null) return;
        // Once everything is collected, point the player at the now-open exit.
        coreText.text = collected >= required
            ? $"{objectiveLabel}: {collected}/{required} - {objectiveCompleteHint}"
            : $"{objectiveLabel}: {collected}/{required}";
    }

    void UpdateTrash(int total)
    {
        if (trashText != null) trashText.text = $"Rác: {total}";
    }
}
