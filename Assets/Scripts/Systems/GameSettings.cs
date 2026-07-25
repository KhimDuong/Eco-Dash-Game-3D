using System;
using UnityEngine;

/// <summary>
/// Persistent audio settings (PlayerPrefs-backed), shared by the Main Menu and
/// the in-level pause menu. A static store — like <see cref="PlayerProgress"/> —
/// so any scene can read/write it without scene wiring; it loads lazily.
///
/// <para><b>Master volume</b> drives <see cref="AudioListener.volume"/>, so it
/// affects everything, including SFX played through <c>AudioSource.PlayClipAtPoint</c>.
/// <b>Music volume</b> additionally scales the looping background track via
/// <see cref="MusicVolume"/> components, letting players balance the soundtrack
/// against the SFX. Settings are applied at startup
/// (<see cref="RuntimeInitializeOnLoadMethodAttribute"/>) and whenever a value
/// changes, and persist across scene loads and app restarts.</para>
/// </summary>
public static class GameSettings
{
    const string MasterKey = "eco_vol_master";
    const string MusicKey = "eco_vol_music";
    const string MuteKey = "eco_muted";

    /// <summary>Raised whenever a setting changes (sliders / music sources listen).</summary>
    public static event Action OnChanged;

    static bool loaded;
    static float master = 1f, music = 0.8f;
    static bool muted;

    public static float MasterVolume
    {
        get { EnsureLoaded(); return master; }
        set { EnsureLoaded(); master = Mathf.Clamp01(value); Save(); Apply(); }
    }

    public static float MusicVolume
    {
        get { EnsureLoaded(); return music; }
        set { EnsureLoaded(); music = Mathf.Clamp01(value); Save(); Apply(); }
    }

    public static bool Muted
    {
        get { EnsureLoaded(); return muted; }
        set { EnsureLoaded(); muted = value; Save(); Apply(); }
    }

    /// <summary>Music multiplier actually in effect (0 while muted).</summary>
    public static float EffectiveMusic => Muted ? 0f : MusicVolume;

    // Apply saved settings once at game start, before the first scene loads, so
    // the very first track honours the player's volume without any scene wiring.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        EnsureLoaded();
        Apply();
    }

    /// <summary>Push the current settings to the audio system and notify listeners.</summary>
    public static void Apply()
    {
        AudioListener.volume = muted ? 0f : master;
        OnChanged?.Invoke();
    }

    static void EnsureLoaded()
    {
        if (loaded) return;
        master = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterKey, 1f));
        music = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, 0.8f));
        muted = PlayerPrefs.GetInt(MuteKey, 0) == 1;
        loaded = true;
    }

    static void Save()
    {
        PlayerPrefs.SetFloat(MasterKey, master);
        PlayerPrefs.SetFloat(MusicKey, music);
        PlayerPrefs.SetInt(MuteKey, muted ? 1 : 0);
        PlayerPrefs.Save();
    }
}
