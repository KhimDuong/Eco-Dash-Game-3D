using System;
using UnityEngine;

/// <summary>
/// Shared persistence backbone for the M9 content stores (<c>Inventory</c>,
/// <c>QuestLog</c>, <c>Codex</c>). It mirrors the <see cref="PlayerProgress"/> /
/// <see cref="GameSettings"/> pattern — a static store backed by
/// <see cref="PlayerPrefs"/> — but adds JSON (de)serialization for the richer
/// state those systems carry (lists of item stacks, per-quest states, per-stage
/// cleanliness %), which a flat int/float PlayerPref cannot hold.
///
/// <para>It is also the single <see cref="ResetNewGame"/> entry point the Main
/// Menu's "Bắt Đầu" button calls, so every per-playthrough store wipes together.
/// As each M9 store lands it adds its reset call here, keeping
/// <c>MenuController</c> a one-liner. Permanent meta-progression
/// (<see cref="PlayerProgress"/> trash + shop upgrades) is deliberately preserved
/// across New Game, matching the pre-M9 behaviour.</para>
/// </summary>
public static class SaveSystem
{
    /// <summary>
    /// Raised by <see cref="ResetNewGame"/> after the stores have wiped, so any
    /// live in-memory listener (e.g. an open HUD) can refresh. The persistent
    /// stores clear their own keys inside <see cref="ResetNewGame"/>; this is just
    /// a post-reset notification.
    /// </summary>
    public static event Action OnReset;

    // --- JSON persistence helpers ------------------------------------------

    /// <summary>Serialize <paramref name="payload"/> to JSON under <paramref name="key"/>.</summary>
    public static void SaveJson<T>(string key, T payload)
    {
        PlayerPrefs.SetString(key, JsonUtility.ToJson(payload));
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Load the JSON stored under <paramref name="key"/> into a fresh
    /// <typeparamref name="T"/>. Returns a new instance when the key is absent or
    /// the JSON is unreadable, so callers never have to null-check.
    /// </summary>
    public static T LoadJson<T>(string key) where T : new()
    {
        string json = PlayerPrefs.GetString(key, null);
        if (string.IsNullOrEmpty(json)) return new T();
        try { return JsonUtility.FromJson<T>(json) ?? new T(); }
        catch { return new T(); }
    }

    public static bool HasKey(string key) => PlayerPrefs.HasKey(key);

    public static void Delete(string key)
    {
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
    }

    // --- New Game reset -----------------------------------------------------

    /// <summary>
    /// Wipe <b>all</b> progress for a true "Chơi Mới" (Start Fresh) — the Main Menu's
    /// new-game path. Clears the per-playthrough stores <em>and</em>
    /// <see cref="PlayerProgress"/> (shop upgrades + trash currency) and the
    /// <see cref="SceneProgress"/> save, so nothing carries over. (Continue, by
    /// contrast, simply loads the saved scene without resetting anything.)
    /// </summary>
    public static void ResetNewGame()
    {
        QuestProgress.ResetQuest();
        Inventory.ResetAll();
        QuestLog.ResetAll();
        Codex.ResetAll();
        SceneProgress.ResetAll();
        PlayerProgress.ResetAll();
        // Re-arm the first-time how-to-play tutorial so a fresh run shows it again.
        PlayerPrefs.DeleteKey(TutorialPopup.SeenKey);
        PlayerPrefs.Save();
        OnReset?.Invoke();
    }
}
